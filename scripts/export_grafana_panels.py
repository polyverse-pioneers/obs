#!/usr/bin/env python3
"""Export panel PNGs from local phase3 Grafana dashboard JSON files.

Usage:
  python3 scripts/export_grafana_panels.py \
    --grafana-url http://planck-primary:3000 \
    --from now-14d \
    --to now \
    --token "$GRAFANA_TOKEN"
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen


def slugify(value: str) -> str:
    value = value.strip().lower()
    value = re.sub(r"[^a-z0-9]+", "-", value)
    return value.strip("-") or "panel"


def load_dashboard(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def iter_panels(dashboard: dict) -> list[dict]:
    # Top-level panels are enough for this repo's dashboards.
    return [p for p in dashboard.get("panels", []) if isinstance(p, dict) and "id" in p]


def download_png(
    grafana_url: str,
    uid: str,
    panel_id: int,
    from_value: str,
    to_value: str,
    width: int,
    height: int,
    tz: str,
    token: str | None,
) -> bytes:
    params = {
        "panelId": panel_id,
        "from": from_value,
        "to": to_value,
        "width": width,
        "height": height,
        "tz": tz,
    }
    base = grafana_url.rstrip("/")
    url = f"{base}/render/d-solo/{uid}/_?{urlencode(params)}"

    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"

    request = Request(url, headers=headers)
    with urlopen(request, timeout=60) as response:
        return response.read()


def main() -> int:
    parser = argparse.ArgumentParser(description="Export Grafana panels to PNG files")
    parser.add_argument("--grafana-url", required=True, help="Grafana base URL, e.g. http://planck-primary:3000")
    parser.add_argument("--token", default=None, help="Grafana API token (optional if anonymous access is enabled)")
    parser.add_argument("--from", dest="from_value", default="now-14d", help="Time range start")
    parser.add_argument("--to", dest="to_value", default="now", help="Time range end")
    parser.add_argument("--tz", default="UTC", help="Timezone for render requests")
    parser.add_argument("--width", type=int, default=1800, help="Image width in pixels")
    parser.add_argument("--height", type=int, default=520, help="Image height in pixels")
    parser.add_argument(
        "--out-dir",
        default="artifacts/grafana",
        help="Output directory for exported images",
    )
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parents[1]
    dashboards_dir = repo_root / "grafana-dashboards"
    out_root = repo_root / args.out_dir
    out_root.mkdir(parents=True, exist_ok=True)

    dashboard_files = [
        dashboards_dir / "dash-A-network-health.json",
        dashboards_dir / "dash-B-internal-performance.json",
        dashboards_dir / "dash-C-isp-performance.json",
        dashboards_dir / "dash-D-evidence-pack.json",
    ]

    failures = 0

    for dashboard_file in dashboard_files:
        if not dashboard_file.exists():
            print(f"WARN missing dashboard file: {dashboard_file}")
            failures += 1
            continue

        dashboard = load_dashboard(dashboard_file)
        uid = dashboard.get("uid")
        title = dashboard.get("title", dashboard_file.stem)
        if not uid:
            print(f"WARN missing uid in {dashboard_file}")
            failures += 1
            continue

        panel_dir = out_root / uid
        panel_dir.mkdir(parents=True, exist_ok=True)

        panels = iter_panels(dashboard)
        print(f"Exporting {len(panels)} panels from '{title}' ({uid})")

        for index, panel in enumerate(panels, start=1):
            panel_id = int(panel["id"])
            panel_title = panel.get("title", f"panel-{panel_id}")
            filename = f"{index:02d}-{panel_id:03d}-{slugify(panel_title)}.png"
            output_path = panel_dir / filename
            try:
                image = download_png(
                    grafana_url=args.grafana_url,
                    uid=uid,
                    panel_id=panel_id,
                    from_value=args.from_value,
                    to_value=args.to_value,
                    width=args.width,
                    height=args.height,
                    tz=args.tz,
                    token=args.token,
                )
                output_path.write_bytes(image)
                print(f"  OK {output_path}")
            except (HTTPError, URLError, TimeoutError, OSError) as exc:
                print(f"  FAIL panel {panel_id} ({panel_title}): {exc}")
                failures += 1

    if failures:
        print(f"Completed with {failures} warning/error(s).")
        return 2

    print("Completed successfully.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
