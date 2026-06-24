# Netspeed

WAN and LAN network observability stack centered on scheduled `iperf3` probes, Telegraf exec ingestion, Prometheus storage, and Grafana dashboards.

## Current Architecture

- Probe runner: `pip-speed-wrapper.sh`
- Scheduler/collector: Telegraf `inputs.exec`
- Storage/query: Prometheus
- Visualization: Grafana dashboards under `grafana-dashboards/`

The wrapper emits Prometheus-formatted metrics directly, including:

- `netspeed_download_mbps`
- `netspeed_upload_mbps`
- `netspeed_tcp_retransmits`
- `netspeed_test_duration_seconds`
- `netspeed_run_success`
- `netspeed_run_exit_code`

Common labels include `endpoint`, `direction`, `protocol`, and `parallel_streams`.

## Key Scripts

### `deploy`

Deploys the wrapper to a remote host over SSH.

- Default host: `planck-primary`
- Destination: `/opt/pip-speed/pip-speed-wrapper.sh`
- Validates remote runtime dependencies (`iperf3`, `jq`)

Run:

```bash
./deploy
```

### `pip-speed-wrapper.sh`

Runs scheduled `iperf3` tests against configured endpoints and prints Prometheus metrics to stdout.

Required environment:

- `IPERF3_ENDPOINTS` (comma-separated `host[:port]` values)

Common tuning environment variables:

- `IPERF3_DURATION_SECONDS`
- `IPERF3_TIMEOUT_SECONDS`
- `IPERF3_PARALLEL_STREAMS`
- `IPERF3_OMIT_SECONDS`
- `IPERF3_ENABLE_UPLOAD`

### `scripts/dns_activity_observer.py`

Summarizes recent Unbound query/reply logs into bounded Prometheus metrics for
top query names and top upstream DNS destinations.

Expected runtime shape:

- Run from Telegraf `inputs.exec`
- Read recent `unbound` journal lines
- Emit only rolling top-N gauges, not raw query events
- Deploy the script to `/opt/obs/dns_activity_observer.py` on Planck, or adjust
  the tracked Telegraf command path to match the installed location

Key environment variables:

- `DNS_ACTIVITY_WINDOW`
- `DNS_ACTIVITY_TOP_N`
- `DNS_ACTIVITY_IGNORE_NAMES`
- `DNS_ACTIVITY_IGNORE_SUFFIXES`
- `DNS_ACTIVITY_UPSTREAM_LABELS`

## Backups

Planck configuration snapshots live under `backups/` and are synced with rsync.

Workflow reference:

- `backups/rsync.md`

WSL local host backups for `quantum-wsl-debian` are captured with
`./scripts/backup-quantum-wsl.sh`, including non-secret resolver and host files
such as `/etc/wsl.conf` (when present) and `/etc/resolv.conf`.

## Docs

- Canonical spec: `docs/obs_spec_iperf-telegraf-prometheus.md`
- Canonical implementation log: `docs/obs_impl_iperf-telegraf-prometheus.md`
- Inventory index: `inventory/INDEX.md`
- Dashboard index: `docs/obs_dashboard_index.md`
- Dashboard implementation summary: `docs/obs_dashboard_phase3-implementation-complete.md`
- Evidence-pack guidance: `docs/obs_report_isp-throttling-evidence-pack.md`
- Evidence-pack source artifact set currently remains at legacy filenames: `docs/isp-evidence-pack.qmd`, `docs/isp-evidence-pack.html`, `docs/isp-evidence-pack.pdf`
- Legacy aliases kept for workflow compatibility: `docs/build.md`, `docs/build-impl.md`
- Glossary: `docs/obs_glossary.md`
- RAG manifest: `rag/manifest.yaml`
- Repo workflow guidance: `.github/copilot-instructions.md`
