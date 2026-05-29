# Dashboard Metric Validation Report

**Generated:** 2026-03-21  
**Prometheus Instance:** planck-primary  
**Total Metrics Available:** 975

---

## Executive Summary

| Dashboard | Status | Issue | Action |
|-----------|--------|-------|--------|
| 1: Network Health & Alerts | ✅ Working | None | Use as Phase 3 Dashboard A |
| 2: Exporter & Agent Health | ⚠️ Partial | Variable config needed | Fix/archive |
| 3: DHCP & Routes | ❌ No Data | No DHCP input configured | Delete |
| 4: Planck System | ❌ No Data | Label mismatch | Fix or delete |
| 5: Router Interfaces | ❌ No Data | Label mismatch | Fix labels |
| 6: Wireless & Bridge | ❌ No Data | Bridge metrics missing | Delete |
| 8: Netspeed Correlation | ✅ Working | None | Use as basis for C/D |
| 18: Latency & Throughput | ✅ Working | Fallback queries (clean up) | Consolidate with 1 → A |

---

## Detailed Analysis

### ✅ Dashboard 1: Network Health & Alerts
**UID:** b3fcf0f2-f60e-49bd-9634-8f8001e13d64  
**Panels:** 4 | **Status:** Working

**Metrics Present:**
- ✅ `ping_result_code` — Ping success/failure status
- ✅ `ping_average_response_ms` — Mean RTT
- ✅ `ping_minimum_response_ms` — Minimum RTT
- ✅ `ping_maximum_response_ms` — Maximum RTT
- ✅ `ping_standard_deviation_ms` — Ping jitter/variance
- ✅ `ping_percent_packet_loss` — Packet loss percentage

**Assessment:** Production-ready. Consolidate with Dashboard 18 for Phase 3 Dashboard A.

---

### ✅ Dashboard 18: Latency & Throughput (Legacy)
**UID:** b9ed9f68-a1f0-4225-b2a1-b3138696a0c4  
**Panels:** 9 (3 rows) | **Status:** Working

#### Row 100: RTT (ICMP)
- ✅ `probe_icmp_duration_seconds` — Blackbox ICMP probe latency
  - Internal targets (heisenberg, hawking, bohr)
  - External targets (1.1.1.1, 8.8.8.8)

#### Row 200: Throughput (iperf3)
**Panel 201 Sent (5 fallback queries, all exist):**
1. ✅ `iperf3_end_sum_sent_bits_per_second` (PRIMARY)
2. ✅ `iperf3_end_sent_bps`
3. ✅ `iperf3_end_streams_0_sender_bits_per_second`
4. ✅ `iperf3_intervals_9_sum_bits_per_second`
5. ✅ `iperf3_interval9_bps`

**Panel 202 Received (5 fallback queries, all exist):**
1. ✅ `iperf3_end_sum_received_bits_per_second` (PRIMARY)
2. ✅ `iperf3_end_received_bps`
3. ✅ `iperf3_end_streams_0_receiver_bits_per_second`
4. ✅ `iperf3_intervals_9_sum_bits_per_second`
5. ✅ `iperf3_interval9_bps`

#### Row 300: iperf3 Diagnostics
- ✅ `iperf3_end_sum_sent_retransmits` — TCP retransmits
- ✅ `iperf3_end_streams_0_sender_mean_rtt` — RTT mean

**Assessment:** Good data coverage. **Recommendation:** Clean up fallback queries - keep only PRIMARY metric names.

---

### ✅ Dashboard 8: Netspeed Correlation
**UID:** 50646b57-ac77-43f4-8d96-6bb6515bcaa5  
**Panels:** 11 | **Status:** Working

**Key Metrics (all present):**
- ✅ `prometheus_netspeed_download_mbps` — ISP download speed
- ✅ `prometheus_netspeed_upload_mbps` — ISP upload speed
- ✅ `prometheus_netspeed_latency_ms_avg|min|max` — ISP latency
- ✅ `prometheus_netspeed_latency_ms_jitter` — Jitter
- ✅ `prometheus_netspeed_download_ttfb_ms` — Time-to-first-byte
- ✅ `prometheus_netspeed_download_transfer_ms` — Transfer duration
- ✅ `prometheus_netspeed_run_success` — Test success indicator
- ✅ `prometheus_netspeed_run_exit_code` — Test exit status

**Assessment:** Excellent reference dashboard. Use as foundation for Phase 3 Dashboards C (ISP Performance) and D (Evidence Pack).

---

### ❌ Dashboard 2: Exporter & Agent Health
**Status:** ⚠️ Partial (Variable issues)

**Metrics Present:**
- ✅ `go_goroutines`, `go_memstats_alloc_bytes`, `go_gc_duration_seconds`

**Issue:** Uses `$instance` variable without explicit definition. **Action:** Fix Grafana variable config or delete.

---

### ❌ Dashboard 3: DHCP & Routes
**Status:** No Data

**Metrics Needed:**
- ❌ `dhcp_leases` — NOT IN PROMETHEUS
- ❌ `routes_total` — NOT IN PROMETHEUS

**Root Cause:** DHCP and route table inputs not configured in Telegraf.  
**Action:** Delete this dashboard (infrastructure doesn't support it).

---

### ❌ Dashboard 4: Planck System
**Status:** No Data (Label Mismatch)

**Queries Use:** `{host="$host"}`  
**Problem:** Actual Telegraf metrics use different label structure.

**Metrics Available but Labels Don't Match:**
- `cpu_usage_*` (10 metrics)
- `disk_*` (27 metrics)
- `interfaces_if*` (18 metrics)

**Action:** Test label mappings against live data and fix queries, or delete.

---

### ❌ Dashboard 5: Router Interfaces
**Status:** No Data (Label Mismatch)

**Query:** `interfaces_ifInOctets{agent_host="$router", ifDescr=~"..."}`

**Issue:** Metrics exist but `agent_host` label structure needs verification.

**Metrics Available:**
- ✅ `interfaces_ifInOctets` (metric exists)
- ✅ `interfaces_ifOutOctets`
- ✅ `interfaces_ifInErrors`, `interfaces_ifOutErrors`, etc.

**Action:** Test against live data to determine correct label names, then fix queries.

---

### ❌ Dashboard 6: Wireless & Bridge / FDB
**Status:** Completely Broken

**Metrics Needed:**
- ❌ `bridgePorts_dot1dTpFdbPort` — NOT IN PROMETHEUS
- ❌ `bridgePorts_dot1dTpFdbStatus` — NOT IN PROMETHEUS

**Root Cause:** Bridge/FDB SNMP OIDs not configured in Telegraf.  
**Prometheus Data:** 0 metrics matching pattern.

**Action:** Delete this dashboard (no infrastructure support).

---

## Metric Category Summary

| Category | Count | Status | Notes |
|----------|-------|--------|-------|
| Blackbox ICMP (probe_*) | 7 | ✅ Working | External/internal latency |
| Telegraf Ping (ping_*) | 19 | ✅ Working | External ping baselines |
| iperf3 LAN Throughput | 276 | ✅ Working | Comprehensive test data |
| pip-speed WAN Data | 10 | ✅ Working | ISP performance metrics |
| Router Interfaces | 18 | ⚠️ Label mismatch | Fix label mappings |
| Bridge/FDB | 0 | ❌ Missing | Not configured |
| CPU/Memory/Disk | 74 | ✅ Available | System metrics present |
| Node-exporter | 258 | ✅ Working | Comprehensive host metrics |
| MikroTik SNMP | 4 | ⚠️ Limited | Only 4 metrics |

---

## Phase 3 Implementation Plan

### Dashboard A: Network Health Overview
**Consolidate:** Dashboards 1 + 18 (latency section)

**Panels:**
1. ICMP Latency by Target (internal + external)
2. Packet Loss by Target
3. Jitter/Variance Trend
4. Uptime/Success Indicators

**Metrics:** All exist ✅

### Dashboard B: DNS Resolver Operations
**From:** Unbound resolver stats + Telegraf `dns_query` health checks

**Panels:**
1. Resolver query volume and recursive reply rate
2. Cache hit/miss rates and cache hit ratio
3. Synthetic lookup latency and result codes
4. Recursion timing, requestlist pressure, and thread query distribution

**Metrics:** `unbound_*` and `dns_query_*` present ✅

### Dashboard C: ISP Performance and Comparison
**Extend:** Dashboard 8 (Netspeed Correlation)

**Add Panels:**
1. External vs Internal Latency Delta
2. Time-of-Day Patterns
3. Correlation: TTFB vs Latency/Jitter
4. Cold vs Warm Download Performance

**Metrics:** All exist ✅

### Dashboard D: Evidence Pack (ISP Escalation)
**From:** Dashboard 8 data

**Panels:**
1. SLA-Style Stats (p50/p95/p99, loss %, shortfall %)
2. Incident Window Summary
3. Internal vs External Proof Comparison
4. Export-Friendly Stats Table

**Metrics:** All exist ✅

---

## Recommendations

### Immediate Actions
1. ✅ Archive dashboards 3, 4, 5, 6 (no/broken data)
2. ✅ Clean Dashboard 18 queries (remove fallback chains)
3. ✅ Consolidate Dashboards 1 + 18 → Dashboard A
4. ✅ Extend Dashboard 8 → Dashboards C + D
5. ⚠️ Fix label mappings for Dashboard 5 interface data

### Quality Improvements
1. Add freshness panels for each metric family
2. Standardize legend formats
3. Document metric semantics in dashboard notes
4. Add "query sanity" checks (non-empty result verification)

### Long-term
1. Implement stable UIDs for production dashboards
2. Automate dashboard validation against metric schema
3. Create dashboard version control (JSON in git)

---

## Validation Methodology

- **Metric Discovery:** Queried Prometheus `/api/v1/label/__name__/values` endpoint
- **Query Testing:** Validated each dashboard metric name against actual Prometheus data
- **Label Testing:** Identified label structure mismatches for SNMP/Telegraf data
- **Timestamp:** 2026-03-21 from planck-primary Prometheus instance
