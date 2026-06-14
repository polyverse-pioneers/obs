#!/usr/bin/env bash

set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
config_dir="$repo_root/backups/hosts/planck/configs/latest"
snapshot_dir="$repo_root/backups/hosts/planck/snapshots"
timestamp=$(date +"%Y-%m-%d_%H%M%S")
host="planck-primary"

mkdir -p "$config_dir" "$snapshot_dir"

# Pull selected config paths from planck. Keep this list explicit and reviewable.
rsync -av --mkpath \
  --rsync-path='sudo rsync' \
  --relative \
  "$host":/etc/unbound/ \
  "$host":/etc/systemd/system/unbound.service.d/ \
  "$host":/etc/ssh/sshd_config \
  "$host":/etc/hosts \
  "$host":/etc/hosts.pre-unbound.bak \
  "$host":/etc/NetworkManager/system-connections/ \
  "$config_dir/"

snapshot_file="$snapshot_dir/${timestamp}_planck-system.txt"

{
  echo "# planck system snapshot"
  echo "# generated: $(date -Iseconds)"
  echo
  echo "## hostname"
  ssh "$host" 'hostnamectl --static'
  echo
  echo "## kernel"
  ssh "$host" 'uname -a'
  echo
  echo "## ip -brief addr"
  ssh "$host" 'ip -brief addr'
  echo
  echo "## default route"
  ssh "$host" 'ip route show default'
  echo
  echo "## unbound status"
  ssh "$host" 'systemctl is-active unbound || true'
} > "$snapshot_file"

echo "planck backup complete: $config_dir"
echo "planck snapshot written: $snapshot_file"
