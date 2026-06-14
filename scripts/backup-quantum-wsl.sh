#!/usr/bin/env bash

set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
config_dir="$repo_root/backups/hosts/quantum-wsl-debian/configs/latest"
snapshot_dir="$repo_root/backups/hosts/quantum-wsl-debian/snapshots"
timestamp=$(date +"%Y-%m-%d_%H%M%S")

mkdir -p "$config_dir" "$snapshot_dir"

# Local endpoint config copies (non-secret paths only).
cp -f /etc/resolv.conf "$config_dir/resolv.conf"
cp -f /etc/hosts "$config_dir/hosts"

if [[ -f /etc/wsl.conf ]]; then
  cp -f /etc/wsl.conf "$config_dir/wsl.conf"
fi

if [[ -f "$HOME/.ssh/config" ]]; then
  cp -f "$HOME/.ssh/config" "$config_dir/ssh_config"
fi

snapshot_file="$snapshot_dir/${timestamp}_wsl-system.txt"

{
  echo "# quantum-wsl-debian system snapshot"
  echo "# generated: $(date -Iseconds)"
  echo
  echo "## hostname"
  hostname
  echo
  echo "## kernel"
  uname -a
  echo
  echo "## ip -brief addr"
  ip -brief addr
  echo
  echo "## default route"
  ip route show default
  echo
  echo "## resolv.conf"
  cat /etc/resolv.conf
} > "$snapshot_file"

echo "wsl backup complete: $config_dir"
echo "wsl snapshot written: $snapshot_file"
