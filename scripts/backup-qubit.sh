#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: ./scripts/backup-qubit.sh [--git-commit] [--git-push] [--commit-message MESSAGE]

Options:
  --git-commit            Stage Qubit backup paths and create a commit when there are changes.
  --git-push              Same as --git-commit, then push after a successful commit.
  --commit-message TEXT   Commit message to use with --git-commit/--git-push.
  -h, --help              Show this help message.
EOF
}

repo_root=$(cd "$(dirname "$0")/.." && pwd)
config_dir="$repo_root/backups/hosts/qubit/configs/latest"
snapshot_dir="$repo_root/backups/hosts/qubit/snapshots"
timestamp=$(date +"%Y-%m-%d_%H%M%S")
host="${QUBIT_HOST:-192.168.40.20}"
ssh_key="${QUBIT_SSH_KEY:-$HOME/.ssh/wsl-qubit}"
ssh_opts=(-i "$ssh_key" -o StrictHostKeyChecking=accept-new -o BatchMode=yes)
git_commit=0
git_push=0
commit_message="refresh qubit host backups"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --git-commit)
      git_commit=1
      shift
      ;;
    --git-push)
      git_commit=1
      git_push=1
      shift
      ;;
    --commit-message)
      if [[ $# -lt 2 ]]; then
        echo "error: --commit-message requires a value" >&2
        usage
        exit 2
      fi
      commit_message="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "error: unknown option: $1" >&2
      usage
      exit 2
      ;;
  esac
done

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
  "$host":/etc/systemd/network/ \
  "$host":/etc/systemd/networkd.conf \
  "$host":/etc/systemd/resolved.conf \
  "$host":/etc/systemd/networkd.conf.d/ \
  "$host":/etc/systemd/resolved.conf.d/ \
  "$host":/etc/systemd/system/network-online.target.wants/ \
  "$host":/etc/systemd/system/systemd-networkd.service.wants/ \
  "$host":/etc/systemd/system/systemd-resolved.service.wants/ \
  "$host":/etc/network/interfaces.bak \
  "$host":/etc/network/interfaces.d.bak/ \
  "$host":/etc/NetworkManager.bak/ \
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
  echo
  echo "## systemd-networkd state"
  ssh "${ssh_opts[@]}" "$host" 'systemctl is-enabled systemd-networkd || true; systemctl is-active systemd-networkd || true'
  echo
  echo "## systemd-resolved state"
  ssh "${ssh_opts[@]}" "$host" 'systemctl is-enabled systemd-resolved || true; systemctl is-active systemd-resolved || true'
} > "$snapshot_file"

echo "qubit backup complete: $config_dir"
echo "qubit snapshot written: $snapshot_file"

if [[ $git_commit -eq 1 ]]; then
  cd "$repo_root"

  git_paths=(
    backups/hosts/qubit/configs/latest
    backups/hosts/qubit/snapshots
  )

  git add "${git_paths[@]}"

  if git diff --cached --quiet -- "${git_paths[@]}"; then
    echo "No Qubit backup changes to commit"
    exit 0
  fi

  git commit -m "$commit_message"

  if [[ $git_push -eq 1 ]]; then
    git push
  fi
fi
