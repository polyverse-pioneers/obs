#!/usr/bin/env python3
"""Emit rolling DNS activity summaries in Prometheus text format.

The collector intentionally emits bounded top-N gauges instead of raw per-query
events so household DNS activity can be inspected without turning Prometheus
into an unbounded log store.
"""

from __future__ import annotations

import ipaddress
import os
import re
import shlex
import socket
import subprocess
import sys
from collections import Counter
from typing import Iterable


QUERY_PATTERNS = [
    re.compile(r"query\[(?P<qtype>[A-Z0-9]+)\]\s+(?P<qname>[^\s]+)", re.IGNORECASE),
    re.compile(r"query\s+(?P<qname>[^\s]+)\s+IN\s+(?P<qtype>[A-Z0-9]+)", re.IGNORECASE),
    re.compile(
        r"info:\s+(?:\d{1,3}\.){3}\d{1,3}\s+(?P<qname>[^\s]+)\s+(?P<qtype>[A-Z0-9]+)\s+IN\s*$",
        re.IGNORECASE,
    ),
]

UPSTREAM_TRIGGERS = ("reply from", "sending query:", "forwards to")
IP_PATTERN = re.compile(r"(?P<ip>(?:\d{1,3}\.){3}\d{1,3}|[0-9a-fA-F:]{2,})")


def env_int(name: str, default: int) -> int:
    raw = os.getenv(name)
    if not raw:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


def parse_window_seconds(raw: str) -> int:
    match = re.fullmatch(r"\s*(\d+)\s*([smhd]?)\s*", raw)
    if not match:
        return 900
    value = int(match.group(1))
    unit = match.group(2) or "s"
    scale = {"s": 1, "m": 60, "h": 3600, "d": 86400}[unit]
    return value * scale


def journal_since_arg(window_seconds: int) -> str:
    if window_seconds % 86400 == 0:
        return f"-{window_seconds // 86400} day"
    if window_seconds % 3600 == 0:
        return f"-{window_seconds // 3600} hour"
    if window_seconds % 60 == 0:
        return f"-{window_seconds // 60} min"
    return f"-{window_seconds} sec"


def run_journal(window_seconds: int, unit: str) -> list[str]:
    command = [
        "journalctl",
        "--unit",
        unit,
        "--since",
        journal_since_arg(window_seconds),
        "--no-pager",
        "--output",
        "cat",
    ]
    result = subprocess.run(command, capture_output=True, text=True, check=False)
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or "journalctl failed")
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def normalize_qname(qname: str) -> str:
    return qname.rstrip(".").lower()


def should_ignore_qname(qname: str, ignored_suffixes: Iterable[str]) -> bool:
    normalized = normalize_qname(qname)
    for suffix in ignored_suffixes:
        clean_suffix = suffix.strip().rstrip(".").lower()
        if not clean_suffix:
            continue
        if normalized == clean_suffix or normalized.endswith(f".{clean_suffix}"):
            return True
    return False


def extract_query(line: str) -> tuple[str, str] | None:
    for pattern in QUERY_PATTERNS:
        match = pattern.search(line)
        if not match:
            continue
        qname = normalize_qname(match.group("qname"))
        qtype = match.group("qtype").upper()
        if qname:
            return qname, qtype
    return None


def extract_upstream_ip(line: str) -> str | None:
    lower_line = line.lower()
    if not any(trigger in lower_line for trigger in UPSTREAM_TRIGGERS):
        return None
    for match in IP_PATTERN.finditer(line):
        value = match.group("ip").rstrip(".:#")
        try:
            ipaddress.ip_address(value)
            return value
        except ValueError:
            continue
    return None


def parse_upstream_labels(raw: str) -> dict[str, str]:
    mapping: dict[str, str] = {}
    for item in raw.split(","):
        if "=" not in item:
            continue
        ip, label = item.split("=", 1)
        ip = ip.strip()
        label = label.strip()
        if ip and label:
            mapping[ip] = label
    return mapping


def resolve_upstream_name(ip: str, aliases: dict[str, str], cache: dict[str, str]) -> str:
    if ip in aliases:
        return aliases[ip]
    if ip in cache:
        return cache[ip]
    try:
        previous_timeout = socket.getdefaulttimeout()
        socket.setdefaulttimeout(0.5)
        host = socket.getnameinfo((ip, 53), 0)[0].rstrip(".").lower()
    except OSError:
        host = ip
    finally:
        socket.setdefaulttimeout(previous_timeout)
    cache[ip] = host
    return host


def escape_label(value: str) -> str:
    return value.replace("\\", "\\\\").replace("\n", "\\n").replace('"', '\\"')


def emit_metric(lines: list[str], name: str, labels: dict[str, str], value: float) -> None:
    label_text = ""
    if labels:
        parts = [f'{key}="{escape_label(labels[key])}"' for key in sorted(labels)]
        label_text = "{" + ",".join(parts) + "}"
    lines.append(f"{name}{label_text} {value}")


def main() -> int:
    window_seconds = parse_window_seconds(os.getenv("DNS_ACTIVITY_WINDOW", "15m"))
    top_n = max(env_int("DNS_ACTIVITY_TOP_N", 20), 1)
    journal_unit = os.getenv("DNS_ACTIVITY_JOURNAL_UNIT", "unbound")
    ignored_suffixes = [item.strip() for item in os.getenv(
        "DNS_ACTIVITY_IGNORE_SUFFIXES",
        "home.spinriko.com,home.polyversepioneers.org,home.polyversepioneers.com,spinrikolab.home.arpa",
    ).split(",")]
    upstream_aliases = parse_upstream_labels(os.getenv("DNS_ACTIVITY_UPSTREAM_LABELS", ""))

    try:
        lines = run_journal(window_seconds, journal_unit)
    except RuntimeError as error:
        print("# HELP dns_activity_observer_scrape_success Whether the DNS activity observer scraped journal data successfully")
        print("# TYPE dns_activity_observer_scrape_success gauge")
        print("dns_activity_observer_scrape_success 0")
        print("# HELP dns_activity_observer_error_info Error status for the last scrape")
        print("# TYPE dns_activity_observer_error_info gauge")
        print(f'dns_activity_observer_error_info{{message="{escape_label(str(error))}"}} 1')
        return 0

    query_counts: Counter[tuple[str, str]] = Counter()
    upstream_counts: Counter[str] = Counter()

    for line in lines:
        query = extract_query(line)
        if query and not should_ignore_qname(query[0], ignored_suffixes):
            query_counts[query] += 1

        upstream_ip = extract_upstream_ip(line)
        if upstream_ip:
            upstream_counts[upstream_ip] += 1

    reverse_cache: dict[str, str] = {}
    total_query_events = sum(query_counts.values())
    total_upstream_events = sum(upstream_counts.values())

    out: list[str] = []
    out.append("# HELP dns_activity_observer_scrape_success Whether the DNS activity observer scraped journal data successfully")
    out.append("# TYPE dns_activity_observer_scrape_success gauge")
    out.append("dns_activity_observer_scrape_success 1")
    out.append("# HELP dns_activity_observer_window_seconds Rolling observation window length")
    out.append("# TYPE dns_activity_observer_window_seconds gauge")
    out.append(f"dns_activity_observer_window_seconds {window_seconds}")
    out.append("# HELP dns_activity_observer_journal_lines Observed Unbound journal lines in the rolling window")
    out.append("# TYPE dns_activity_observer_journal_lines gauge")
    out.append(f"dns_activity_observer_journal_lines {len(lines)}")
    out.append("# HELP dns_activity_observer_query_events Parsed query log events in the rolling window")
    out.append("# TYPE dns_activity_observer_query_events gauge")
    out.append(f"dns_activity_observer_query_events {total_query_events}")
    out.append("# HELP dns_activity_observer_upstream_events Parsed upstream destination events in the rolling window")
    out.append("# TYPE dns_activity_observer_upstream_events gauge")
    out.append(f"dns_activity_observer_upstream_events {total_upstream_events}")
    out.append("# HELP dns_activity_observer_top_query_count Top query names observed in the rolling window")
    out.append("# TYPE dns_activity_observer_top_query_count gauge")
    out.append("# HELP dns_activity_observer_top_query_share Share of parsed query events represented by each top query")
    out.append("# TYPE dns_activity_observer_top_query_share gauge")
    out.append("# HELP dns_activity_observer_top_upstream_count Top upstream DNS destinations observed in the rolling window")
    out.append("# TYPE dns_activity_observer_top_upstream_count gauge")
    out.append("# HELP dns_activity_observer_top_upstream_share Share of parsed upstream events represented by each top upstream destination")
    out.append("# TYPE dns_activity_observer_top_upstream_share gauge")

    for rank, ((qname, qtype), count) in enumerate(query_counts.most_common(top_n), start=1):
        labels = {"rank": str(rank), "qname": qname, "qtype": qtype}
        emit_metric(out, "dns_activity_observer_top_query_count", labels, float(count))
        share = float(count) / float(total_query_events) if total_query_events else 0.0
        emit_metric(out, "dns_activity_observer_top_query_share", labels, share)

    for rank, (upstream_ip, count) in enumerate(upstream_counts.most_common(top_n), start=1):
        labels = {
            "rank": str(rank),
            "upstream_ip": upstream_ip,
            "upstream_name": resolve_upstream_name(upstream_ip, upstream_aliases, reverse_cache),
        }
        emit_metric(out, "dns_activity_observer_top_upstream_count", labels, float(count))
        share = float(count) / float(total_upstream_events) if total_upstream_events else 0.0
        emit_metric(out, "dns_activity_observer_top_upstream_share", labels, share)

    sys.stdout.write("\n".join(out) + "\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())