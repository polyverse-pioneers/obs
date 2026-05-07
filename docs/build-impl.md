# Netspeed Implementation Log

Status: active
Spec source: `docs/build.md`

## Completed Work

- Migrated probe execution from legacy HTTP speedtest approach to scheduled `iperf3` probing.
- Rewrote `pip-speed-wrapper.sh` to emit Prometheus metrics directly.
- Added endpoint-based metric labels and run-health metrics for observability.
- Updated deployment flow to ship wrapper-only runtime artifacts.
- Added dependency preflight checks (`iperf3`, `jq`) in deploy workflow.
- Updated Telegraf configuration backups to use iperf3 endpoint list and runtime knobs.
- Updated Dashboard C to focus on throughput, retransmits, and success outcomes.
- Added backup workflow guidance and safe-by-default rsync process.

## Operational Defaults

- Probe interval: 15 minutes (Telegraf)
- Typical duration: 30 seconds
- Parallel streams: 4
- Omit warm-up window: 2 seconds

## Runtime Expectations

- Wrapper remains backward-compatible with Telegraf `inputs.exec` and Prometheus text ingestion.
- Failures are represented as metrics (`netspeed_run_success`, `netspeed_run_exit_code`) rather than silent drops.

## Next Optional Improvements

- Add endpoint grouping labels (for example: west/east/pacific) for cleaner Grafana filtering.
- Add synthetic alert rules for sustained throughput degradation and retransmit spikes.
- Add one-command maintenance helper for rsync + git commit/push workflow.
