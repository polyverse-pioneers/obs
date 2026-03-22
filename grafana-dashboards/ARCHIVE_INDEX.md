# Archived Dashboards Index

**Archive Date:** 2026-03-21
**Reason:** Phase 3 consolidation - all old dashboards superseded by new consolidated dashboard set (A, B, C, D)

## Archived Dashboards

| File | UID | Title | Reason for Archive | Status |
|---|---|---|---|---|
| dash_1.json | b3fcf0f2-f60e-49bd-9634-8f8001e13d64 | Network Health & Alerts | Consolidated into Dashboard A | ✅ Working (merged) |
| dash_2.json | health-all | Exporter & Agent Health | Variable configuration issues, out of Phase 3 scope | ⚠️ Partial |
| dash_3.json | dhcp-routes | DHCP & Routes | No infrastructure (DHCP not configured in Telegraf) | ❌ No Data |
| dash_4.json | planck-system | Planck System | Label structure mismatch (metrics exist but use different label names) | ⚠️ Broken |
| dash_5.json | router-interfaces | Router Interfaces | SNMP label mapping issue (18 interface metrics present but label structure differs) | ⚠️ Broken (defer) |
| dash_6.json | wire-bridge | Wireless & Bridge / FDB | No bridge/FDB metrics configured (zero bridgePorts_* in Prometheus) | ❌ No Data |
| dash_8.json | 50646b57-ac77-43f4-8d96-6bb6515bcaa5 | Netspeed Correlation | Split into Dashboard C (ISP Performance) + Dashboard D (Evidence Pack) | ✅ Working (split) |
| dash_18.json | b9ed9f68-a1f0-4225-b2a1-b3138696a0c4 | Latency & Throughput (Legacy) | Split into Dashboard A (RTT/ping) + Dashboard B (iperf3) | ✅ Working (split) |

## Migration Summary

**Consolidated Fully:**
- ✅ Dashboard 1 → Dashboard A (Network Health Overview)
- ✅ Dashboard 18 → Dashboard A (RTT sections) + Dashboard B (iperf3 sections)

**Split into Separate Dashboards:**
- ✅ Dashboard 8 → Dashboard C (ISP Performance) + Dashboard D (Evidence Pack)

**Archived (Out of Scope for Phase 3):**
- Dashboard 2: Agent health monitoring (system-level metrics, needs variable fixes)
- Dashboard 3-6: Infrastructure gaps or label mismatches (deferred for future phases)

## Future Considerations

**Dashboard 5 (Router Interfaces):** Has 18 interface_if* metrics available in Prometheus but expects different label names. If SNMP label mapping is fixed in Telegraf, this dashboard can be reconstructed with correct queries.

**Dashboards 3 & 6:** Require additional Telegraf configuration:
- Dashboard 3 needs DHCP input plugin configuration
- Dashboard 6 needs bridge/FDB SNMP OID configuration

**Dashboard 4:** Planck system metrics are available with different label structure - could be recreated with correct label mapping.

## Recovery

If any archived dashboard is needed:
```bash
cp grafana-dashboards/archive/dash_N.json grafana-dashboards/
```

Then import via Grafana UI or API.

## Phase 3 Active Dashboards

These replace the archived set:
- **dash-A-network-health.json** (UID: `phase3-dashboard-a`)
- **dash-B-internal-performance.json** (UID: `phase3-dashboard-b`)
- **dash-C-isp-performance.json** (UID: `phase3-dashboard-c`)
- **dash-D-evidence-pack.json** (UID: `phase3-dashboard-d`)
