# Next Steps: Network Observability Recovery and ISP Evidence Plan

## Objective
Build a coherent, trustworthy observability stack that helps us:
- Understand internal network behavior and bottlenecks.
- Separate internal issues from ISP-side underperformance.
- Produce defensible evidence for ISP escalation.

## Guiding Principles
- Keep metric sources explicit and minimal.
- Prefer dashboards that answer specific questions.
- Avoid duplicate/overlapping dashboards unless purpose is different.
- Every panel should map to a concrete decision or action.
- Preserve dashboard UIDs and treat JSON as source of truth.

## Phase 1: Inventory and Backup
1. Export all Grafana dashboards currently in Grafana (JSON).
2. Backup Grafana provisioning and settings (if applicable).
3. Backup Prometheus config and rules:
- `prometheus.yml`
- Rule files used by Prometheus.
- Scrape targets and job definitions.
4. Backup Telegraf configs:
- Main config and included fragments.
- Inputs/outputs/processors related to network and iperf.
5. Capture service/runtime context:
- Hostnames and role of each monitored device.
- Which jobs run where.
- Approximate scrape intervals and retention windows.
6. Save all artifacts in a timestamped folder in this repo or a sibling archive.

## Phase 2: Source-of-Truth Mapping
For each metric family, document:
1. Producer (which process emits it).
2. Collection path (Telegraf or exporter to Prometheus).
3. Labels that matter (`instance`, `job`, etc.).
4. Expected cadence (every X seconds/minutes).
5. Failure modes (what missing data means).

Initial metric families to map:
- `probe_*` (blackbox ICMP checks)
- `iperf3_*` (throughput and retransmits)
- `speedtest_*` and/or `prometheus_netspeed_*`
- Interface/system metrics (`node_*`, `interfaces_*`, relevant host metrics)

## Phase 3: Dashboard Rationalization
Create a target dashboard set with clear purpose.

### Dashboard A: Network Health Overview
Questions answered:
- Is the network healthy right now?
- Which path/target is degraded first?
Panels:
- ICMP latency by target (internal + external).
- Packet loss by target.
- Jitter/variance trend (where available).
- Uptime/success indicators for probes.

### Dashboard B: Internal Network Performance
Questions answered:
- Are internal links/devices saturating or unstable?
- Is throughput degradation inside the LAN?
Panels:
- iperf throughput by managed device.
- Retransmits and RTT/RTT variance by managed device.
- Interface error/discard counters on key hosts.

### Dashboard C: ISP Performance and Comparison
Questions answered:
- Is ISP underperforming vs expectation?
- Is degradation correlated with specific times?
Panels:
- External probe latency/loss/jitter (Cloudflare/Google and others).
- WAN throughput trends (speedtest/netspeed).
- Delta panels: internal healthy while external degrades.
- Time-of-day and day-of-week patterns.

### Dashboard D: Evidence Pack (ISP Escalation)
Questions answered:
- Can we show repeatable, quantified ISP underperformance?
Panels:
- SLA-style stats (p50/p95/p99 latency, loss %, throughput shortfall %).
- Incident windows summary.
- Internal-vs-external comparison proof.
- Export-friendly tables for dates, durations, magnitude.

## Phase 4: Data Quality Guardrails
1. Add "freshness" panels for each key metric family.
2. Add "query sanity" panels that should always show non-empty values.
3. Standardize label usage and legend formats across dashboards.
4. Enforce one Prometheus datasource UID across dashboard JSON unless explicitly needed.
5. Add dashboard notes describing assumptions and metric semantics.

## Phase 5: ISP Evidence Workflow
1. Define baseline expectations:
- Contracted download/upload.
- Acceptable latency and packet loss thresholds.
2. Define incident criteria:
- Example: external p95 latency > threshold while internal latency remains normal.
3. Create weekly evidence report template:
- Date window.
- Summary metrics.
- Graph snapshots.
- Plain-language impact statement.
4. Save exports and screenshots in a dated evidence folder.

## Implementation Plan for Our Next Session
1. You provide backups/exports for Grafana, Prometheus, and Telegraf.
2. I inventory all metrics/jobs and produce a dependency map.
3. We agree on the final dashboard set (A-D above).
4. I refactor/create dashboard JSON files with stable UIDs and clean naming.
5. We validate each panel against live data and fix gaps.
6. We finalize an "ISP evidence" dashboard and report checklist.

## Definition of Done
- A coherent dashboard set with no redundant confusion.
- Every panel has a documented purpose.
- Critical panels show live data and have freshness checks.
- You can answer:
  - Is this internal or ISP?
  - When does it happen?
  - How severe is it?
  - What evidence should be sent to ISP?

## Notes
- Recovery priority: restore the useful iperf managed-device dashboard first.
- After restoration, we will normalize naming, folders, and UIDs to avoid future loss/overwrites.
