#!/usr/bin/env bash
set -u -o pipefail

IPERF3_BIN="${IPERF3_BIN:-/usr/bin/iperf3}"
IPERF3_DURATION_SECONDS="${IPERF3_DURATION_SECONDS:-30}"
IPERF3_TIMEOUT_SECONDS="${IPERF3_TIMEOUT_SECONDS:-90}"
IPERF3_PARALLEL_STREAMS="${IPERF3_PARALLEL_STREAMS:-1}"
IPERF3_OMIT_SECONDS="${IPERF3_OMIT_SECONDS:-2}"
IPERF3_ENABLE_UPLOAD="${IPERF3_ENABLE_UPLOAD:-1}"
IPERF3_ENDPOINTS="${IPERF3_ENDPOINTS:-}"

log_info() {
	if command -v logger >/dev/null 2>&1; then
		logger -t pip-speed-wrapper "$1"
	fi
}

emit_run_health() {
	local endpoint="$1"
	local direction="$2"
	local exit_code="$3"
	local success="0"

	if [[ "$exit_code" -eq 0 ]]; then
		success="1"
	fi

	printf 'netspeed_run_success{endpoint="%s",direction="%s",protocol="tcp"} %s\n' \
		"$endpoint" "$direction" "$success"
	printf 'netspeed_run_exit_code{endpoint="%s",direction="%s",protocol="tcp"} %s\n' \
		"$endpoint" "$direction" "$exit_code"
}

extract_number() {
	local json="$1"
	local jq_expr="$2"
	printf '%s' "$json" | jq -r "$jq_expr // 0"
}

run_test() {
	local endpoint="$1"
	local direction="$2"
	local host="$endpoint"
	local port="5201"

	if [[ "$endpoint" == *":"* ]]; then
		host="${endpoint%%:*}"
		port="${endpoint##*:}"
	fi

	local -a cmd=(
		"$IPERF3_BIN"
		-c "$host"
		-p "$port"
		-J
		-t "$IPERF3_DURATION_SECONDS"
		--connect-timeout "$((IPERF3_TIMEOUT_SECONDS * 1000))"
		-P "$IPERF3_PARALLEL_STREAMS"
		-O "$IPERF3_OMIT_SECONDS"
	)

	if [[ "$direction" == "download" ]]; then
		cmd+=( -R )
	fi

	local output=""
	local exit_code=0
	if ! output=$("${cmd[@]}" 2>&1); then
		exit_code=$?
		log_info "endpoint=$endpoint direction=$direction exit_code=$exit_code output=$(printf '%s' "$output" | tr '\n' ' ')"
		emit_run_health "$endpoint" "$direction" "$exit_code"
		return
	fi

	local reported_error
	reported_error=$(printf '%s' "$output" | jq -r '.error // ""' 2>/dev/null)
	if [[ -n "$reported_error" ]]; then
		log_info "endpoint=$endpoint direction=$direction iperf3_error=$reported_error"
		emit_run_health "$endpoint" "$direction" 2
		return
	fi

	local bps="0"
	local duration_s="0"
	local retransmits="0"

	if [[ "$direction" == "download" ]]; then
		bps=$(extract_number "$output" '.end.sum_received.bits_per_second')
		duration_s=$(extract_number "$output" '.end.sum_received.seconds')
		retransmits=$(extract_number "$output" '.end.sum_sent.retransmits')
	else
		bps=$(extract_number "$output" '.end.sum_sent.bits_per_second')
		duration_s=$(extract_number "$output" '.end.sum_sent.seconds')
		retransmits=$(extract_number "$output" '.end.sum_sent.retransmits')
	fi

	local mbps
	mbps=$(awk -v bits="$bps" 'BEGIN { printf "%.6f", bits / 1000000.0 }')

	printf 'netspeed_%s_mbps{endpoint="%s",protocol="tcp",parallel_streams="%s"} %s\n' \
		"$direction" "$endpoint" "$IPERF3_PARALLEL_STREAMS" "$mbps"
	printf 'netspeed_test_duration_seconds{endpoint="%s",direction="%s",protocol="tcp"} %s\n' \
		"$endpoint" "$direction" "$duration_s"
	printf 'netspeed_tcp_retransmits{endpoint="%s",direction="%s",protocol="tcp"} %s\n' \
		"$endpoint" "$direction" "$retransmits"

	emit_run_health "$endpoint" "$direction" 0
}

if [[ -z "$IPERF3_ENDPOINTS" ]]; then
	log_info "missing required env var: IPERF3_ENDPOINTS"
	emit_run_health missing-config download 64
	if [[ "$IPERF3_ENABLE_UPLOAD" == "1" ]]; then
		emit_run_health missing-config upload 64
	fi
	exit 0
fi

if [[ ! -x "$IPERF3_BIN" ]]; then
	log_info "iperf3 binary missing or not executable: $IPERF3_BIN"
	emit_run_health missing-iperf3 download 127
	if [[ "$IPERF3_ENABLE_UPLOAD" == "1" ]]; then
		emit_run_health missing-iperf3 upload 127
	fi
	exit 0
fi

if ! command -v jq >/dev/null 2>&1; then
	log_info "jq is required to parse iperf3 json output"
	emit_run_health missing-jq download 127
	if [[ "$IPERF3_ENABLE_UPLOAD" == "1" ]]; then
		emit_run_health missing-jq upload 127
	fi
	exit 0
fi

IFS=',' read -r -a endpoints <<< "$IPERF3_ENDPOINTS"
for endpoint in "${endpoints[@]}"; do
	trimmed_endpoint="$(printf '%s' "$endpoint" | xargs)"
	if [[ -z "$trimmed_endpoint" ]]; then
		continue
	fi

	run_test "$trimmed_endpoint" download
	if [[ "$IPERF3_ENABLE_UPLOAD" == "1" ]]; then
		run_test "$trimmed_endpoint" upload
	fi
done

exit 0