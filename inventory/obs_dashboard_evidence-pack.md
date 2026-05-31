---
domain: obs
kind: dashboard
id: obs_dashboard_evidence-pack
device: planck
service: grafana
depends_on:
  - obs_service_prometheus
  - obs_probe_iperf3
aliases:
  - dashboard d
---

# Observability Dashboard: Evidence Pack

Status: Retirement candidate

## Purpose

Grafana dashboard for export-friendly ISP evidence, SLA metrics, and daily
reporting views.

## Lifecycle

- ISP evidence collection goals are largely met.
- Treat this dashboard as a retirement candidate rather than part of the long-term core set.

## Expected Coverage

- Daily run success rate
- Small-profile download aggregates
- Latency and TTFB SLA metrics
- Export-ready snapshot panels
