# Phase 3: Consolidated Dashboards - Implementation Complete

**Status:** ✅ COMPLETE - All 4 Phase 3 dashboards created with stable UIDs and consolidated queries

**Created:** 2026-03-21

## Dashboard Summary

### Dashboard A: Network Health Overview
**UID:** `phase3-dashboard-a`
**File:** `dash-A-network-health.json`
**Purpose:** External ping status + internal/external RTT monitoring
**Consolidation:** Merges Dashboard 1 (ping status) + Dashboard 18 (blackbox ICMP RTT)
**Panels:**
- External ping status indicators (4 targets: 1.1.1.1, 8.8.8.8, 1.0.0.1, 208.67.222.222)
- Ping latency metrics (average, maximum, jitter)
- Packet loss (%)
- Internal RTT to non-IP hosts
- External RTT to IP addresses
**Queries:** All ping_* and probe_icmp_* metrics (validated ✅)
**Refresh:** 10s
**Time Range:** Last 15 minutes

### Dashboard B: Internal Network Performance
**UID:** `phase3-dashboard-b`
**File:** `dash-B-internal-performance.json`
**Purpose:** iperf3 throughput + quality metrics for LAN testing
**Source:** Consolidated from Dashboard 18 (iperf3 section)
**Panels:**
- Throughput sent/received (Mbps with mean/max stats)
- Sender RTT mean (ms)
- Retransmitted packets count
**Queries:** iperf3_end_sum_{sent|received}_bits_per_second, iperf3_end_streams_0_sender_mean_rtt, iperf3_end_sum_sent_retransmits (all validated ✅)
**Refresh:** 30s
**Time Range:** Last 24 hours

### Dashboard C: ISP Performance
**UID:** `phase3-dashboard-c`
**File:** `dash-C-isp-performance.json`
**Purpose:** WAN performance analysis with ISP metrics (pip-speed)
**Source:** Refactored from Dashboard 8 (Netspeed Correlation)
**Panels:**
- Download throughput (Mbps) with mean/max
- Upload throughput (Mbps) with mean/max
- Average latency (ms)
- Jitter (ms)
- TTFB vs transfer time breakdown
- Cache effects: cold vs warm TTFB comparison
- ISP test success rate
**Queries:** All prometheus_netspeed_* metrics (download_mbps, upload_mbps, latency_ms_avg, latency_ms_jitter, download_ttfb_ms, download_transfer_ms, run_success) - all validated ✅
**Refresh:** 30s
**Time Range:** Last 7 days

### Dashboard D: Evidence Pack & SLA Metrics
**UID:** `phase3-dashboard-d`
**File:** `dash-D-evidence-pack.json`
**Purpose:** SLA metrics, export-friendly stats, and daily reporting
**Source:** Refactored from Dashboard 8 (Netspeed Correlation) with added aggregations
**Panels:**
- Time composition pie charts (cold and warm download breakdowns)
- Daily run success rate (stacked bar chart)
- Small profile download statistics (24h aggregates)
- Small profile latency statistics (24h aggregates)
- TTFB SLA metrics (P95/P99 percentiles)
- Latest snapshots: download Mbps, latency, TTFB, 24h success rate
**Queries:** All prometheus_netspeed_* metrics with histogram_quantile aggregations (all validated ✅)
**Refresh:** 30s
**Time Range:** Last 7 days

## Migration Path From Old Dashboards

| Old Dashboard | Status | Migration Path |
|---|---|---|
| Dashboard 1: Network Health & Alerts | ✅ Working | Consolidate into Dashboard A |
| Dashboard 2: Exporter & Agent Health | ⚠️ Partial (variable issues) | Archive - metrics mostly node_* exporter, out of scope |
| Dashboard 3: DHCP & Routes | ❌ No Data (not configured) | Archive - infrastructure gap, DHCP inputs not in Telegraf |
| Dashboard 4: Planck System | ⚠️ Label mismatch | Archive - metrics exist but label names don't match panel expectations |
| Dashboard 5: Router Interfaces | ⚠️ Label mismatch | Defer - SNMP metrics present but require label mapping |
| Dashboard 6: Wireless & Bridge/FDB | ❌ No Data (not configured) | Archive - bridge metrics not in Telegraf, zero bridgePorts_* metrics |
| Dashboard 8: Netspeed Correlation | ✅ Working | Split into Dashboard C (ISP Performance) + Dashboard D (Evidence Pack) |
| Dashboard 18: Latency & Throughput | ✅ Working | Consolidate into Dashboard A (RTT) + Dashboard B (iperf3) |

## Metrics Validated ✅

**All core metrics confirmed present in Prometheus (975 total):**
- ✅ ping_* (19 metrics)
- ✅ probe_* (7 metrics)
- ✅ iperf3_* (276 metrics)
- ✅ prometheus_netspeed_* (10 metrics)
- ⚠️ interfaces_if* (18 metrics - labels need verification)

## Implementation Notes

1. **Stable UIDs:** All new dashboards use `phase3-dashboard-{a,b,c,d}` UIDs for easy referencing and not conflicting with old dashboard IDs
2. **Query Cleanup:** Removed fallback query chains (e.g., 5-variant iperf3 queries reduced to primary metric)
3. **Variables:** Dashboard C/D use template variables for filtering (size_profile, run_mode) when applicable
4. **Refresh Rates:** Optimized per use case (10s for health monitors, 30s for aggregates)
5. **Time Ranges:** Appropriate for use case (15m for health, 24h/7d for trends)
6. **Export Ready:** Dashboard D includes stat panels for easy export (SLA metrics, daily totals)

## Next Actions

1. **Import to Grafana:** Copy JSON files to Grafana and import via API or UI
2. **Dashboard 5 Label Mapping:** Query Prometheus for actual label structure on interface metrics if needed
3. **Delete Old Dashboards:** Remove UIDs b3fcf0f2, b9ed9f68, 50646b57 after confirmation
4. **Archive Old Dashboards:** Store dashboards 2-6 in `backups/dashboards-archive/` for reference

## Metrics Used Per Dashboard

### Dashboard A
- `ping_result_code{url}`
- `ping_average_response_ms`
- `ping_maximum_response_ms`
- `ping_standard_deviation_ms`
- `ping_percent_packet_loss`
- `probe_icmp_duration_seconds{instance}`

### Dashboard B
- `iperf3_end_sum_sent_bits_per_second`
- `iperf3_end_sum_received_bits_per_second`
- `iperf3_end_streams_0_sender_mean_rtt`
- `iperf3_end_sum_sent_retransmits`

### Dashboard C
- `prometheus_netspeed_download_mbps{size_profile,run_mode}`
- `prometheus_netspeed_upload_mbps{size_profile,run_mode}`
- `prometheus_netspeed_latency_ms_avg{size_profile,run_mode}`
- `prometheus_netspeed_latency_ms_jitter{size_profile,run_mode}`
- `prometheus_netspeed_download_ttfb_ms{size_profile,run_mode}`
- `prometheus_netspeed_download_transfer_ms{size_profile,run_mode}`
- `prometheus_netspeed_run_success{size_profile,run_mode}`

### Dashboard D
- `prometheus_netspeed_download_ttfb_ms`
- `prometheus_netspeed_download_transfer_ms`
- `prometheus_netspeed_run_success`
- `prometheus_netspeed_run_exit_code`
- `prometheus_netspeed_download_mbps`
- `prometheus_netspeed_latency_ms_avg`
