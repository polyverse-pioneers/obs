# Grafana Dashboards Extracted from Backup

**Source:** `/backups/var/lib/grafana/grafana.db`  
**Extraction Date:** 2026-03-21  
**Total Dashboards:** 8

---

## Dashboard Summary

| ID | UID | Title | Panels | Status | Notes |
|----|-----|-------|--------|--------|-------|
| 1 | b3fcf0f2-f60e-49bd-9634-8f8001e13d64 | Network Health & Alerts | 4 | ⚠️ PARTIAL | External ping queries; depends on Telegraf ping input |
| 2 | health-all | Exporter & Agent Health | 3 | ⚠️ PARTIAL | Go runtime metrics; queries have `$instance` variable |
| 3 | dhcp-routes | DHCP & Routes | 2 | ❌ NO DATA | Telegraf SNMP queries; metric names may not exist |
| 4 | planck-system | Planck System | 4 | ❌ NO DATA | Telegraf SNMP queries; metric names appear incorrect |
| 5 | router-interfaces | Router Interfaces | 3 | ❌ NO DATA | Telegraf interface metrics; labels don't match config |
| 6 | wire-bridge | Wireless & Bridge / FDB | 2 | ❌ NO DATA | Telegraf SNMP bridge port queries; no data |
| 8 | 50646b57-ac77-43f4-8d96-6bb6515bcaa5 | Netspeed Correlation | 11 | ✅ GOOD | pip-speed prometheus output; detailed timing breakdown |
| 18 | b9ed9f68-a1f0-4225-b2a1-b3138696a0c4 | Latency & Throughput (Legacy) | 9 | ⚠️ PARTIAL | Multiple fallback queries; iperf3 metric names uncertain |

---

## Detailed Analysis

### Dashboard 1: Network Health & Alerts
**UID:** `b3fcf0f2-f60e-49bd-9634-8f8001e13d64`

**Panels:**
1. **1.1.1.1 Status** (stat)
   - Query: `ping_result_code{url="1.1.1.1"}`
   - Status: ✅ Should work if Telegraf ping input active
   
2. **8.8.8.8 Status** (stat)
   - Query: `ping_result_code{url="8.8.8.8"}`
   - Status: ✅ Should work if Telegraf ping input active

3. **Ping Latency (ms)** (timeseries)
   - Queries: `ping_average_response_ms`, `ping_minimum_response_ms`, `ping_maximum_response_ms`, `ping_standard_deviation_ms`
   - Status: ⚠️ May have no data from some targets

4. **Packet Loss (%)** (timeseries)
   - Query: `ping_percent_packet_loss`
   - Status: ⚠️ May have no data

---

### Dashboard 2: Exporter & Agent Health
**UID:** `health-all`

**Panels:**
1. **Go Goroutines** (timeseries)
   - Query: `go_goroutines{instance=~"$instance"}`
   - Status: ⚠️ Uses `$instance` variable; needs dashboard variable configuration

2. **Go Memory Allocated (bytes)** (timeseries)
   - Query: `go_memstats_alloc_bytes{instance=~"$instance"}`
   - Status: ⚠️ Uses `$instance` variable; needs dashboard variable configuration

3. **GC Duration (seconds)** (timeseries)
   - Query: `go_gc_duration_seconds{instance=~"$instance"}`
   - Status: ⚠️ Uses `$instance` variable; needs dashboard variable configuration

---

### Dashboard 3: DHCP & Routes
**UID:** `dhcp-routes`

**Status: ❌ NO DATA** — Metric names do not exist in current Telegraf config

**Panels:**
1. **DHCP Lease Activity** (table)
   - Query: `dhcp_leases{agent_host="$router"}`
   - Issue: No DHCP input configured in Telegraf

2. **Route Table** (table)
   - Query: `routes_total{agent_host="$router"}`
   - Issue: No route table input configured in Telegraf

---

### Dashboard 4: Planck System
**UID:** `planck-system`

**Status: ❌ NO DATA** — Metric names do not match actual Telegraf/node-exporter output

**Panels:**
1. **CPU Usage (%)** (timeseries)
   - Query: `100 - avg by (host) (cpu_usage_idle{host="$host"})`
   - Issue: Metric likely named `cpu_*` from Telegraf but label structure differs

2. **Disk Used (%)** (timeseries)
   - Query: `avg by (host) (disk_used_percent{host="$host"})`
   - Issue: Metric name mismatch; Telegraf outputs may differ

3. **Disk IO Util (%)** (timeseries)
   - Query: `avg by (host) (diskio_io_util{host="$host"} * 100)`
   - Issue: Label structure issue

4. **Network Traffic (bps)** (timeseries)
   - Queries: Interface octet rates
   - Issue: Label mismatch with actual Telegraf SNMP output

---

### Dashboard 5: Router Interfaces
**UID:** `router-interfaces`

**Status: ❌ NO DATA** — Metric label structure does not match Telegraf SNMP config

**Panels:**
1. **Inbound Traffic (bps)** (timeseries)
   - Query: `sum by (ifDescr) (rate(interfaces_ifInOctets{agent_host="$router", ifDescr=~"...`
   - Issue: Expected labels: `instance`, `host`, `ifDescr`; actual SNMP output likely different

2. **Outbound Traffic (bps)** (timeseries)
   - Query: Similar label mismatch

3. **Errors & Discards** (table)
   - Queries: Four separate metrics with label filters
   - Issue: Label structure doesn't match SNMP table output

---

### Dashboard 6: Wireless & Bridge / FDB
**UID:** `wire-bridge`

**Status: ❌ NO DATA** — SNMP bridge metrics not available

**Panels:**
1. **Bridge FDB Port Mapping** (table)
   - Query: `bridgePorts_dot1dTpFdbPort{agent_host="$router"}`
   - Issue: Metric requires MikroTik bridge port OID config

2. **Bridge FDB Status** (table)
   - Query: `bridgePorts_dot1dTpFdbStatus{agent_host="$router"}`
   - Issue: Metric requires MikroTik bridge port OID config

---

### Dashboard 8: Netspeed Correlation ✅
**UID:** `50646b57-ac77-43f4-8d96-6bb6515bcaa5`

**Status: ✅ GOOD** — All queries align with pip-speed Prometheus output

**Panels:**
1. **Throughput (Direction Filter)** (timeseries)
   - Query template: `{__name__=~"prometheus_netspeed_(${direction})_mbps", ...}`
   - Variables: `direction`, `run_mode`, `size_profile`

2. **Latency (Avg / Min / Max)** (timeseries)
   - Queries: `prometheus_netspeed_latency_ms_avg|min|max`

3. **Jitter** (timeseries)
   - Query: `prometheus_netspeed_latency_ms_jitter`

4. **Download Timing (TTFB vs Transfer)** (timeseries)
   - Queries: `prometheus_netspeed_download_ttfb_ms`, `prometheus_netspeed_download_transfer_ms`

5. **Cold Download Time Composition** (piechart)

6. **Warm Download Time Composition** (piechart)

7. **TTFB vs Latency/Jitter Correlation** (timeseries)

8. **Warm vs Cold TTFB** (timeseries)

9. **Warm vs Cold Download Mbps** (timeseries)

10. **Warmup Benefit Delta (%)** (timeseries)

11. **Run Health By Size And Run Mode** (timeseries)
    - Queries: `prometheus_netspeed_run_success`, `prometheus_netspeed_run_exit_code`

---

### Dashboard 18: Latency & Throughput (Legacy)
**UID:** `b9ed9f68-a1f0-4225-b2a1-b3138696a0c4`

**Status: ⚠️ PARTIAL** — Multiple fallback queries suggest uncertain metric names

**Panels:**

**Row 100: RTT (ICMP)**

1. **Internal RTT** (timeseries)
   - Query: `probe_icmp_duration_seconds{instance!~".*([0-9]{1,3}\\.){3}[0-9]{1,3}.*"} * 1000`
   - Status: ✅ Blackbox probe metric; should work if ICMP module configured

2. **External RTT** (timeseries)
   - Query: `probe_icmp_duration_seconds{instance=~".*([0-9]{1,3}\\.){3}[0-9]{1,3}.*"} * 1000`
   - Status: ✅ Blackbox probe metric; targets external IPs

**Row 200: Throughput (iperf3)**

3. **Throughput (Sent)** (timeseries)
   - Queries: 5 fallback options
     - `iperf3_end_sum_sent_bits_per_second`
     - `iperf3_end_sent_bps`
     - `iperf3_end_streams_0_sender_bits_per_second`
     - `iperf3_intervals_9_sum_bits_per_second`
     - `iperf3_interval9_bps`
   - Status: ⚠️ Uncertain which metric name is correct

4. **Throughput (Received)** (timeseries)
   - Queries: 5 fallback options (similar pattern)
   - Status: ⚠️ Uncertain which metric name is correct

**Row 300: iperf3 Diagnostics**

5. **Retransmits** (timeseries)
   - Queries: 5 fallback options
   - Status: ⚠️ Uncertain which metric name is correct

6. **Sender RTT (Mean)** (timeseries)
   - Queries: 5 fallback options
   - Status: ⚠️ Uncertain which metric name is correct

---

## Issues Summary

### Metric Name Mismatches
- **Dashboards 3, 4, 5, 6:** Telegraf SNMP and system metrics use mismatched names/labels
- **Dashboard 18:** Multiple fallback queries suggest uncertainty in iperf3 JSON field parsing

### Missing Inputs
- **Dashboard 3:** DHCP and route table inputs not configured
- **Dashboard 6:** Bridge/FDB SNMP OIDs not in config

### Variable Configuration
- **Dashboard 2:** Uses `$instance` variable without clear definition

### Data Quality
- **Dashboards 1, 18:** May have partial data depending on active targets

---

## Path Forward (per Phase 3 Plan)

1. **Validate current metrics**: Query Prometheus against live data to determine actual metric names and labels
2. **Consolidate redundancy**: Merge dashboards 1 & 18 latency sections into unified "Network Health Overview" (Phase 3 Dashboard A)
3. **Fix query names**: Update queries to match actual Telegraf/Blackbox output
4. **Remove broken dashboards**: Archive 3, 4, 5, 6 as legacy; Phase 3 Dashboard B is now DNS Resolver Operations
5. **Extend Netspeed**: Keep Dashboard 8 as reference; create Phase 3 Dashboard C (ISP Performance) and D (Evidence Pack)

