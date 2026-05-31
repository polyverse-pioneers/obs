---
domain: obs
kind: dashboard
id: obs_dashboard_network-health
device: planck
service: grafana
depends_on:
  - obs_service_prometheus
aliases:
  - dashboard a
---

# Observability Dashboard: Network Health

Status: Active

## Purpose

Grafana dashboard for external ping status, loss, and RTT monitoring.

## Expected Coverage

- External ping status indicators
- Ping latency metrics and jitter
- Packet loss
- Internal and external RTT views
