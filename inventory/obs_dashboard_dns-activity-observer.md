---
domain: obs
kind: dashboard
id: obs_dashboard_dns-activity-observer
device: planck
service: grafana
depends_on:
  - dns_service_unbound
  - obs_service_prometheus
aliases:
  - dashboard f
---

# Observability Dashboard: DNS Activity Observer

Status: Active

## Purpose

Grafana dashboard for bounded DNS activity summaries on Planck, focused on top
query names and top outbound upstream DNS destinations without storing raw
packet payloads in Prometheus.

## Expected Coverage

- Observer scrape health and recent log coverage
- Top query names by count and share over the configured rolling window
- Top upstream DNS destinations by count and share over the configured rolling
  window
- Hostname labels for known upstream DNS destinations where explicit mappings or
  reverse lookups are available
