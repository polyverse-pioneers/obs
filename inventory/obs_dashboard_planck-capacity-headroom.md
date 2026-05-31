---
domain: obs
kind: dashboard
id: obs_dashboard_planck-capacity-headroom
device: planck
service: grafana
depends_on:
  - dns_service_unbound
  - obs_service_prometheus
aliases:
  - dashboard e
---

# Observability Dashboard: Planck Capacity And Service Headroom

Status: Active

## Purpose

Grafana dashboard for deciding whether Planck can safely host an additional RAG
workload without degrading DNS or WireGuard-sensitive traffic.

## Expected Coverage

- Admission-gate summary based on CPU busy, load per core, free memory, iowait,
  and DNS latency guardrails
- CPU, iowait, memory, swap, and thermal trends for the Pi host
- Synthetic DNS latency and success ratio
- Resolver query volume and recursion timing
- `wg0` traffic and overall host network throughput
