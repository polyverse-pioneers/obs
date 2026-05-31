---
domain: obs
kind: dashboard
id: obs_dashboard_isp-performance
device: planck
service: grafana
depends_on:
  - obs_probe_iperf3
  - obs_service_prometheus
aliases:
  - dashboard c
---

# Observability Dashboard: ISP Performance

Status: Active

## Purpose

Grafana dashboard for WAN throughput, retransmits, and probe success outcomes.

## Expected Coverage

- Download throughput by endpoint
- Upload throughput by endpoint
- TCP retransmits by endpoint and direction
- Download versus upload comparison
- Probe success and exit-code health
