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

Dashboard C (`phase3-dashboard-c`) should visualize:

- Download throughput by endpoint
- Upload throughput by endpoint
- TCP retransmits by endpoint (download and upload)
- Combined download vs upload comparison
- Run success by endpoint/direction

## 8. Backup Requirements

- Planck config snapshots are captured via `backups/planck.list` and rsync.
- `backups/var/` remains ignored to avoid runtime database churn.
- Backup sync workflow is documented in `backups/rsync.md`.
