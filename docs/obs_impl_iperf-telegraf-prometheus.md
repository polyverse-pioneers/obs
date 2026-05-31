# Netspeed Implementation Log

Status: active
Spec source: `docs/obs_spec_iperf-telegraf-prometheus.md`

## Completed Work

- Migrated probe execution from legacy HTTP speedtest approach to scheduled `iperf3` probing.
- Rewrote `pip-speed-wrapper.sh` to emit Prometheus metrics directly.
- Added endpoint-based metric labels and run-health metrics for observability.
- Updated deployment flow to ship wrapper-only runtime artifacts.
- Added dependency preflight checks (`iperf3`, `jq`) in deploy workflow.
- Updated Telegraf configuration backups to use iperf3 endpoint list and runtime knobs.
- Updated Dashboard C to focus on throughput, retransmits, and success outcomes.
- Replaced Dashboard B with a DNS resolver operations dashboard backed by `unbound_*` and `dns_query_*` metrics.
- Added Dashboard E to judge whether Planck still has enough CPU, memory, thermal, disk-wait, DNS, and WireGuard-adjacent headroom to host RAG safely.
- Added Dashboard F and a bounded DNS activity exporter to summarize top query names and upstream DNS destinations from recent Unbound logs.
- Added backup workflow guidance and safe-by-default rsync process.
- Added tracked Telegraf inputs for Unbound resolver stats and local DNS query health checks.
- Added `/etc/unbound/` to the Planck backup inventory.
- Standardized Grafana on the internal-only hostname `grafana.spinrikolab.home.arpa` behind nginx on Planck.
- Updated Planck's Unbound private zones so Grafana resolves only through `spinrikolab.home.arpa` and no longer through the public-domain home zones.
- Removed the public-domain Grafana reverse-proxy path so browser access no longer depends on HSTS-sensitive hostnames.

## Operational Defaults

- Probe interval: 15 minutes (Telegraf)
- Typical duration: 30 seconds
- Parallel streams: 4
- Omit warm-up window: 2 seconds

## Runtime Expectations

- Wrapper remains backward-compatible with Telegraf `inputs.exec` and Prometheus text ingestion.
- Failures are represented as metrics (`netspeed_run_success`, `netspeed_run_exit_code`) rather than silent drops.
- DNS monitoring uses `inputs.unbound` for activity counters and `inputs.dns_query` for lightweight end-to-end resolver checks.
- DNS activity observation uses `inputs.exec` to emit bounded top-N query and upstream destination metrics from recent Unbound query/reply logs.
- The tracked Telegraf command path expects the exporter script at `/opt/obs/dns_activity_observer.py` on Planck after deployment.
- Capacity headroom monitoring assumes Prometheus continues scraping the local `node` job and uses `node_*` metrics plus resolver health metrics as the admission signals.
- The preferred permission model is to add the `telegraf` user to the `unbound` group so `unbound-control` works without sudo.
- The DNS activity exporter assumes the `telegraf` service can read the Unbound systemd journal, preferably via the `systemd-journal` supplementary group.
- Grafana now listens on `127.0.0.1:3000` behind nginx, with `domain` and `root_url` set to `http://grafana.spinrikolab.home.arpa/`.
- The Unbound private-zone file now carries the Grafana `A` record only in `spinrikolab.home.arpa`; the public-domain home zones retain only their non-Grafana records.
- The internal-only web namespace target is `*.spinrikolab.home.arpa`; prior `home.arpa` and public-domain Grafana hostnames have been removed from the active Planck config.

## Validation

- Run `telegraf --test --config /etc/telegraf/telegraf.conf --config-directory /etc/telegraf/telegraf.d --input-filter unbound:dns_query` on Planck after syncing the config.
- Confirm Prometheus receives `unbound_*` and `dns_query_*` series before building Grafana panels or alerts.
- Confirm Prometheus receives `dns_activity_observer_*` series after enabling Unbound query/reply logging and the Telegraf exec collector.
- Confirm Prometheus still receives `node_*` series from the local `node` job before importing the capacity dashboard.
- After importing Dashboard E, verify that the `wg0` traffic panel resolves the interface label on Planck and expect it to stay near zero when the tunnel is idle.
- If Unbound stops answering while sockets remain bound on `:53`, a full `systemctl restart unbound` may be required; in this incident the stop path timed out and systemd recovered by SIGKILLing the wedged worker before clean startup.
- Verified live cutover on Planck with `dig +short grafana.spinrikolab.home.arpa A`, empty answers for `grafana.home.polyversepioneers.org` and `grafana.home.polyversepioneers.com`, and `curl -I -H 'Host: grafana.spinrikolab.home.arpa' http://127.0.0.1/login` returning `200 OK` from nginx.

## Next Optional Improvements

- Add endpoint grouping labels (for example: west/east/pacific) for cleaner Grafana filtering.
- Add synthetic alert rules for sustained throughput degradation and retransmit spikes.
- Add one-command maintenance helper for rsync + git commit/push workflow.
