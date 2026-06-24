#!/usr/bin/env bash

set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
config_dir="$repo_root/backups/hosts/qubit/configs/latest"
snapshot_dir="$repo_root/backups/hosts/qubit/snapshots"
timestamp=$(date +"%Y-%m-%d_%H%M%S")
host="${QUBIT_HOST:-192.168.40.20}"
ssh_key="${QUBIT_SSH_KEY:-$HOME/.ssh/wsl-qubit}"
ssh_opts=(-i "$ssh_key" -o StrictHostKeyChecking=accept-new -o BatchMode=yes)

mkdir -p "$config_dir" "$snapshot_dir"

if ! ssh "${ssh_opts[@]}" "$host" 'true' >/dev/null 2>&1; then
  echo "error: unable to reach qubit host '$host' via SSH" >&2
  echo "hint: set QUBIT_HOST (for example QUBIT_HOST=qubit or QUBIT_HOST=192.168.40.20)" >&2
  exit 1
fi

# Pull selected config paths from qubit. Keep this list explicit and reviewable.
rsync -av --mkpath \
  --ignore-missing-args \
  -e "ssh -i $ssh_key -o StrictHostKeyChecking=accept-new -o BatchMode=yes" \
  --relative \
  "$host":/etc/ssh/sshd_config \
  "$host":/etc/hosts \
  "$host":/etc/hostname \
  "$host":/etc/fstab \
  "$host":/etc/systemd/system/ \
  "$host":/etc/netplan/ \
  "$config_dir/"

snapshot_file="$snapshot_dir/${timestamp}_qubit-system.txt"

{
  echo "# qubit system snapshot"
  echo "# generated: $(date -Iseconds)"
  echo
  echo "## hostname"
  ssh "${ssh_opts[@]}" "$host" 'hostnamectl --static'
  echo
  echo "## kernel"
  ssh "${ssh_opts[@]}" "$host" 'uname -a'
  echo
  echo "## ip -brief addr"
  ssh "${ssh_opts[@]}" "$host" 'ip -brief addr'
  echo
  echo "## default route"
  ssh "${ssh_opts[@]}" "$host" 'ip route show default'
} > "$snapshot_file"

echo "qubit backup complete: $config_dir"
echo "qubit snapshot written: $snapshot_file"
