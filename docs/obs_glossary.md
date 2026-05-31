# Observability Glossary

Status: Draft template

## Canonical Prefix

- `obs`

## Core Entities

- `obs_service_telegraf`: Telegraf collection and execution pipeline on Planck.
- `obs_service_prometheus`: Prometheus storage and query layer for collected metrics.
- `obs_service_grafana`: Grafana dashboards and internal UI entry point.
- `obs_probe_iperf3`: Scheduled `iperf3` throughput and retransmit probe workflow.
- `obs_dashboard_network-health`: Network-health dashboard for external ping and RTT coverage.
- `obs_dashboard_dns-resolver-operations`: Resolver health dashboard fed by Unbound and synthetic DNS metrics.
- `obs_dashboard_isp-performance`: WAN throughput and retransmit dashboard.
- `obs_dashboard_evidence-pack`: Export-oriented dashboard for ISP evidence and SLA views; now a retirement candidate.

## Preferred Terms

- Use `obs` for observability-wide concepts.
- Use `probe` for active measurement jobs.
- Use `dashboard` for Grafana deliverables.
- Use `evidence-pack` for generated ISP-facing artifacts.

## Canonical Report Docs

- `obs_report_isp-throttling-evidence-pack`: Human-oriented guidance for building and presenting the ISP evidence story.

## Alias Rules

- Treat `netspeed` as a legacy repo label, not the long-term domain prefix.
- Treat `monitoring` as an informal alias; prefer `observability` or `obs` in canonical docs.
