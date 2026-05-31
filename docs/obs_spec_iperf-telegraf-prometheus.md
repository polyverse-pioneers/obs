# Netspeed Build Specification

## 1. Objective

Provide deterministic, scheduled ISP and path performance measurements using `iperf3`, exported as Prometheus metrics through Telegraf.

## 2. Runtime Design

1. Telegraf runs `pip-speed-wrapper.sh` on a fixed interval.
2. Wrapper executes `iperf3` probes for each endpoint.
3. Wrapper emits Prometheus text format for ingestion.
4. Prometheus stores metrics and Grafana renders dashboards.

## 3. Required Runtime Dependencies

- `iperf3`
- `jq`
- Telegraf with `inputs.exec`
- Prometheus scrape of Telegraf output endpoint

## 4. Required Configuration

Environment variables passed to wrapper from Telegraf:

- `IPERF3_ENDPOINTS` (required)
- `IPERF3_DURATION_SECONDS` (default 30)
- `IPERF3_TIMEOUT_SECONDS`
- `IPERF3_PARALLEL_STREAMS` (default 4 in current deployment)
- `IPERF3_OMIT_SECONDS` (default 2)
- `IPERF3_ENABLE_UPLOAD` (0 or 1)

## 5. Output Contract

Wrapper must emit these Prometheus metrics:

- `netspeed_download_mbps`
- `netspeed_upload_mbps`
- `netspeed_test_duration_seconds`
- `netspeed_tcp_retransmits`
- `netspeed_run_success`
- `netspeed_run_exit_code`

Expected labels:

- `endpoint`
- `direction`
- `protocol`
- `parallel_streams`

## 6. Reliability Requirements

- Wrapper should always emit run-health metrics even when probe failures occur.
- Wrapper should not crash Telegraf collection cycle on single-endpoint failures.
- Failed endpoint runs must surface via `netspeed_run_success=0` and non-zero exit code metric.

## 7. Dashboard Requirements

Dashboard B (`phase3-dashboard-b`) should visualize DNS resolver health using the
Unbound and `dns_query` metrics collected from Planck.

Required dashboard coverage:

- Synthetic local lookup latency by probe/domain
- Synthetic lookup result codes or failures
- Resolver query volume and recursive reply rate
- Cache hit ratio and hit/miss rates
- Recursion timing and requestlist pressure

Dashboard F (`phase3-dashboard-f`) should visualize bounded DNS activity
summaries for Planck without turning Prometheus into a raw query log store.

Required dashboard coverage:

- Observer scrape health and recent journal coverage
- Top query names by count and share over the configured rolling window
- Top outbound upstream DNS destinations by count and share over the configured
  rolling window
- Upstream destination hostname labels where explicit aliases or reverse lookups
  are available

Dashboard C (`phase3-dashboard-c`) should visualize:

- Download throughput by endpoint
- Upload throughput by endpoint
- TCP retransmits by endpoint (download and upload)
- Combined download vs upload comparison
- Run success by endpoint/direction

Dashboard E (`phase3-dashboard-e`) should visualize whether Planck still has
enough service headroom to take on RAG workloads without hurting DNS or
WireGuard.

Required dashboard coverage:

- Admission-gate summary based on CPU busy, load per core, free memory, iowait,
  and DNS latency guardrails
- CPU busy and iowait trends
- Load-per-core trends
- Memory and swap headroom
- Thermal trend for the Pi host
- Synthetic DNS latency, DNS success ratio, resolver query volume, and recursion
  timing
- `wg0` traffic and overall host network throughput as WireGuard-adjacent health
  signals

## 8. Backup Requirements

- Planck config snapshots are captured via `backups/planck.list` and rsync.
- `backups/var/` remains ignored to avoid runtime database churn.
- Backup sync workflow is documented in `backups/rsync.md`.

## 9. DNS Monitoring Requirements

Planck also runs a local Unbound resolver that must be observable through the existing
Telegraf -> Prometheus -> Grafana path.

Required collection:

- Telegraf `inputs.unbound` for resolver activity, cache behavior, and recursion timing.
- Telegraf `inputs.dns_query` against `127.0.0.1:53` for a lightweight synthetic resolver health check.
- Telegraf `inputs.exec` running a bounded DNS activity summarizer over recent
  Unbound query/reply logs.

Minimum metrics expected in Prometheus:

- `unbound_total_num_queries`
- `unbound_total_num_cachehits`
- `unbound_total_num_cachemiss`
- `unbound_total_num_recursivereplies`
- `unbound_total_recursion_time_avg`
- `dns_query_query_time_ms`
- `dns_query_result_code`
- `dns_query_rcode_value`
- `dns_activity_observer_scrape_success`
- `dns_activity_observer_journal_lines`
- `dns_activity_observer_query_events`
- `dns_activity_observer_upstream_events`
- `dns_activity_observer_top_query_count`
- `dns_activity_observer_top_query_share`
- `dns_activity_observer_top_upstream_count`
- `dns_activity_observer_top_upstream_share`
- `node_cpu_seconds_total`
- `node_load1`
- `node_load5`
- `node_memory_MemAvailable_bytes`
- `node_memory_SwapFree_bytes`
- `node_network_receive_bytes_total`
- `node_network_transmit_bytes_total`

Recommended host metrics for the admission gate:

- `node_thermal_zone_temp`
- `node_disk_io_time_seconds_total`

Expected labels/tags for DNS metrics:

- `service=unbound`
- `resolver=planck-local`
- `thread` on thread-scoped Unbound metrics
- `domain`, `record_type`, `server`, `result`, and `rcode` on synthetic DNS query metrics
- `rank`, `qname`, and `qtype` on DNS activity query-summary metrics
- `rank`, `upstream_ip`, and `upstream_name` on DNS activity upstream-summary metrics

Operational requirement:

- The Telegraf runtime user must be able to execute `unbound-control` successfully, preferably by membership in the `unbound` group.
- The Telegraf runtime user must also be able to read recent Unbound logs,
  preferably by membership in the `systemd-journal` group when journald is the
  log source.

## 10. Internal UI Naming Requirements

- Internal-only services that should be reachable by household devices without custom CA rollout should use a reserved internal namespace outside any public-domain HSTS tree.
- Internal web UIs should live under `*.spinrikolab.home.arpa` rather than under any public-domain suffix.
- Grafana should be reachable at `http://grafana.spinrikolab.home.arpa/` through Planck's local DNS and nginx reverse proxy.
- Browser-facing internal services should not depend on public-domain aliases once the internal namespace cutover is complete.
