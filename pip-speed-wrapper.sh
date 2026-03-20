#!/usr/bin/env bash
set -u -o pipefail

PIP_SPEED_BIN="${PIP_SPEED_BIN:-/opt/pip-speed/pip-speed}"
LATENCY_SAMPLES="${PIP_SPEED_LATENCY_SAMPLES:-5}"
ROTATION_SECONDS="${PIP_SPEED_ROTATION_SECONDS:-900}"
SLOT_OVERRIDE="${PIP_SPEED_SLOT_INDEX:-}"
NOW_EPOCH="${PIP_SPEED_NOW_EPOCH:-}"

select_profile() {
	local slot_index="$1"

	case "$slot_index" in
		0)
			SIZE_PROFILE="small"
			DOWNLOAD_SIZE="1048576"
			;;
		1)
			SIZE_PROFILE="medium"
			DOWNLOAD_SIZE="5242880"
			;;
		2)
			SIZE_PROFILE="large"
			DOWNLOAD_SIZE="10485760"
			;;
		*)
			SIZE_PROFILE="small"
			DOWNLOAD_SIZE="1048576"
			;;
	esac
}

emit_run_health() {
	local run_mode="$1"
	local exit_code="$2"
	local success="0"

	if [[ "$exit_code" -eq 0 ]]; then
		success="1"
	fi

	printf 'netspeed_run_success{download_size="%s",run_mode="%s",size_profile="%s"} %s\n' \
		"$DOWNLOAD_SIZE" "$run_mode" "$SIZE_PROFILE" "$success"
	printf 'netspeed_run_exit_code{download_size="%s",run_mode="%s",size_profile="%s"} %s\n' \
		"$DOWNLOAD_SIZE" "$run_mode" "$SIZE_PROFILE" "$exit_code"
}

run_mode() {
	local run_mode="$1"
	shift

	local -a cmd=(
		"$PIP_SPEED_BIN"
		run
		--backend tcpdata
		--format prometheus
		--latency-samples "$LATENCY_SAMPLES"
		--download-size "$DOWNLOAD_SIZE"
		--label "size_profile=$SIZE_PROFILE"
		--label "download_size=$DOWNLOAD_SIZE"
	)

	if [[ "$run_mode" == "warm" ]]; then
		cmd+=(--warmup-request)
	fi

	local output=""
	local exit_code=0
	if ! output=$("${cmd[@]}" 2>&1); then
		exit_code=$?
		if command -v logger >/dev/null 2>&1; then
			logger -t pip-speed-wrapper "run_mode=$run_mode size_profile=$SIZE_PROFILE download_size=$DOWNLOAD_SIZE exit_code=$exit_code output=$(printf '%s' "$output" | tr '\n' ' ')"
		fi
	else
		if [[ -n "$output" ]]; then
			printf '%s\n' "$output"
		fi
	fi

	emit_run_health "$run_mode" "$exit_code"
}

if [[ -n "$SLOT_OVERRIDE" ]]; then
	SLOT_INDEX="$SLOT_OVERRIDE"
else
	if [[ -z "$NOW_EPOCH" ]]; then
		NOW_EPOCH="$(date -u +%s)"
	fi

	SLOT_INDEX=$(( (NOW_EPOCH / ROTATION_SECONDS) % 3 ))
fi

select_profile "$SLOT_INDEX"

if command -v logger >/dev/null 2>&1; then
	logger -t pip-speed-wrapper "invoked uid=$(id -u) ppid=$PPID parent=$(cat /proc/$PPID/comm 2>/dev/null) slot_index=$SLOT_INDEX size_profile=$SIZE_PROFILE download_size=$DOWNLOAD_SIZE"
fi

if [[ ! -x "$PIP_SPEED_BIN" ]]; then
	if command -v logger >/dev/null 2>&1; then
		logger -t pip-speed-wrapper "binary missing or not executable: $PIP_SPEED_BIN"
	fi

	emit_run_health cold 127
	emit_run_health warm 127
	exit 0
fi

run_mode cold
run_mode warm
exit 0