# Observability Dashboard Index

Status: Active

This index ties the dashboard JSON files, inventory docs, and implementation
notes together.

## Canonical Dashboard Set

| Dashboard | Inventory Doc | JSON File | UID | Purpose |
| --- | --- | --- | --- | --- |
| A | [../inventory/obs_dashboard_network-health.md](../inventory/obs_dashboard_network-health.md) | `grafana-dashboards/dash-A-network-health.json` | `phase3-dashboard-a` | Network health overview |
| B | [../inventory/obs_dashboard_dns-resolver-operations.md](../inventory/obs_dashboard_dns-resolver-operations.md) | `grafana-dashboards/dash-B-dns-resolver-operations.json` | `phase3-dashboard-b` | DNS resolver operations |
| C | [../inventory/obs_dashboard_isp-performance.md](../inventory/obs_dashboard_isp-performance.md) | `grafana-dashboards/dash-C-isp-performance.json` | `phase3-dashboard-c` | ISP performance |
| D | [../inventory/obs_dashboard_evidence-pack.md](../inventory/obs_dashboard_evidence-pack.md) | `grafana-dashboards/dash-D-evidence-pack.json` | `phase3-dashboard-d` | Evidence pack and SLA metrics; retirement candidate |

## Related Docs

- Implementation summary: [obs_dashboard_phase3-implementation-complete.md](obs_dashboard_phase3-implementation-complete.md)
- Next steps: [obs_runbook_next-steps.md](obs_runbook_next-steps.md)
- Archived dashboard inventory: `../grafana-dashboards/ARCHIVE_INDEX.md`
