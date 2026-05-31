---
domain: obs
kind: dashboard
id: obs_dashboard_dns-resolver-operations
device: planck
service: grafana
depends_on:
  - dns_service_unbound
  - obs_service_prometheus
aliases:
  - dashboard b
---

# Observability Dashboard: DNS Resolver Operations

Status: Active

## Purpose

Grafana dashboard for local resolver health using Unbound metrics and synthetic
DNS probes.

## Expected Coverage

- Synthetic local lookup latency
- Synthetic lookup failures and result codes
- Resolver query volume and recursive reply rate
- Cache hit ratio and hit/miss rates
- Recursion timing and requestlist pressure
