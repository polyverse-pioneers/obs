# ISP Throttling Evidence Pack (14-day)

Purpose: build a PDF that demonstrates sustained throughput limitation (possible throttling) rather than random congestion spikes.

## What convinces an ISP escalation team

Use a control-vs-treatment story:

1. Internal network is healthy and fast (LAN control).
2. External line underperforms consistently across many days (WAN treatment).
3. Underperformance appears as a repeated ceiling or sustained shortfall, not isolated spikes.
4. Degradation is not explained by packet loss or local error bursts at the same times.

## Current data horizon (verified on planck-primary)

- Prometheus oldest TSDB sample: 2026-03-20T00:00:00Z
- Prometheus newest TSDB sample: 2026-04-04T07:10:36Z
- Effective retained window: about 15.3 days
- This confirms your 14-day report is possible from existing data.

## 14-day baseline values (queried live)

WAN (netspeed, small/cold):
- Samples in 14d: 1791
- Download avg: 19.83 Mbps
- Download p50: 20.18 Mbps
- Download p95: 21.81 Mbps
- Upload p50: 38.24 Mbps
- Latency p95: 583.81 ms
- Jitter p95: 329.26 ms
- Run success rate: 100%

LAN control (iperf3):
- LAN receive p50: 7291.09 Mbps
- LAN receive p95: 16598.80 Mbps
- LAN RTT p95: 411 ms
- LAN retransmits p95: 1

External quality (ping 1.1.1.1):
- Avg packet loss over 14d: 0.029%
- RTT p95 over 14d: 30.58 ms

Interpretation starter:
- LAN throughput is very high while WAN download sits around about 20 Mbps, which helps argue the bottleneck is not your local network fabric.

## Recommended graphs for the PDF

Use these in this order.

1. Daily WAN throughput envelope (most important)
- Dashboard: C (ISP Performance)
- Metric: prometheus_netspeed_download_mbps with run_mode="cold", size_profile="small"
- Show daily p50 and daily max over 14d.
- Why: repeated low daily ceiling is strong throttling evidence.

2. Download shortfall vs plan speed
- Dashboard: D (Evidence Pack) custom panel or Prometheus query
- Plot shortfall percentage against your contracted rate.
- Why: turns technical data into SLA language.

3. WAN quality context (loss and latency)
- Dashboard: A + C
- Show packet loss %, RTT p95, jitter p95 for same period.
- Why: if throughput is capped while loss/RTT are not extreme, it weakens "just congestion" arguments.

4. LAN control graph (internal unaffected)
- Dashboard: B
- iperf3 receive throughput p50/p95 over same dates.
- Why: demonstrates your internal network can move traffic far above observed WAN rate.

5. Interface error/discard context
- Dashboard: router metrics panels where available
- Include note about known physical interruption events (for example, manual cable unplug).
- Why: preempts ISP blame-shifting to your local cabling for unrelated windows.

## PromQL for 14-day evidence panels

Run these with dashboard time range set to last 14 days.

Daily WAN throughput p50 (small/cold):
quantile_over_time(0.50, prometheus_netspeed_download_mbps{size_profile="small",run_mode="cold"}[1d])

Daily WAN throughput p95 (small/cold):
quantile_over_time(0.95, prometheus_netspeed_download_mbps{size_profile="small",run_mode="cold"}[1d])

Daily WAN throughput max (small/cold):
max_over_time(prometheus_netspeed_download_mbps{size_profile="small",run_mode="cold"}[1d])

Daily upload p50 (small/cold):
quantile_over_time(0.50, prometheus_netspeed_upload_mbps{size_profile="small",run_mode="cold"}[1d])

Daily latency p95 (small/cold):
quantile_over_time(0.95, prometheus_netspeed_latency_ms_avg{size_profile="small",run_mode="cold"}[1d])

Daily jitter p95 (small/cold):
quantile_over_time(0.95, prometheus_netspeed_latency_ms_jitter{size_profile="small",run_mode="cold"}[1d])

Daily success rate (%):
100 * sum_over_time(prometheus_netspeed_run_success[1d]) / clamp_min(count_over_time(prometheus_netspeed_run_success[1d]), 1)

LAN control daily p50 (Mbps):
quantile_over_time(0.50, iperf3_end_sum_received_bits_per_second[1d]) / 1e6

LAN control daily p95 (Mbps):
quantile_over_time(0.95, iperf3_end_sum_received_bits_per_second[1d]) / 1e6

Shortfall against contracted download rate (replace 300 with your plan Mbps):
100 * clamp_min(1 - (prometheus_netspeed_download_mbps{size_profile="small",run_mode="cold"} / 300), 0)

## Suggested tables for the PDF

Table 1: 14-day summary (single row per metric)
- Download p50, p95, max
- Upload p50, p95
- Latency p50, p95
- Jitter p95
- Success rate
- Sample count

Table 2: Daily compliance table (14 rows)
- Date
- Daily max download
- Daily p50 download
- Shortfall % vs plan
- Packet loss avg
- RTT p95

Table 3: Control comparison
- WAN download p50 vs LAN iperf3 p50
- WAN download p95 vs LAN iperf3 p95
- Ratio WAN/LAN (%)

## Important caution when claiming throttling

Use "evidence consistent with throttling" unless you also show:
- repeated time-of-day shaping windows, and
- persistent rate ceilings across multiple test targets and file sizes, and
- no corresponding local faults.

This keeps the report credible and difficult to dismiss.

## Why you only saw 2-7 days in dashboards

Likely causes:
- Dashboard defaults were set to now-7d.
- Some panels used smoothing windows like last_over_time(...[6h]) that can obscure long-horizon variation.

Actions already applied in repo JSON:
- dash-C default range changed to now-14d.
- dash-D default range changed to now-14d.
- 14d added to quick time options for both dashboards.

## Export workflow for your PDF

1. Set both dashboards to last 14 days.
2. Export panel images for the 5 graph groups above.
3. Export or copy table data for daily compliance and summary tables.
4. Add one-page narrative:
   - "Internal LAN healthy"
   - "External throughput persistently low"
   - "Observed behavior is consistent with sustained line limitation"
5. Append raw query definitions (PromQL section) as methodology.
