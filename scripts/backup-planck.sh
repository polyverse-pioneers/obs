#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: ./scripts/backup-planck.sh [--git-commit] [--git-push] [--commit-message MESSAGE]

Options:
  --git-commit            Stage Planck backup paths and create a commit when there are changes.
  --git-push              Same as --git-commit, then push after a successful commit.
  --commit-message TEXT   Commit message to use with --git-commit/--git-push.
  -h, --help              Show this help message.
EOF
}

repo_root=$(cd "$(dirname "$0")/.." && pwd)
config_dir="$repo_root/backups/hosts/planck/configs/latest"
snapshot_dir="$repo_root/backups/hosts/planck/snapshots"
timestamp=$(date +"%Y-%m-%d_%H%M%S")
host="planck-primary"
git_commit=0
git_push=0
commit_message="refresh planck host backups"

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

if [[ $git_commit -eq 1 ]]; then
  cd "$repo_root"

  git_paths=(
    backups/hosts/planck/configs/latest
    backups/hosts/planck/snapshots
  )

  git add "${git_paths[@]}"

  if git diff --cached --quiet -- "${git_paths[@]}"; then
    echo "No Planck backup changes to commit"
    exit 0
  fi

  git commit -m "$commit_message"

  if [[ $git_push -eq 1 ]]; then
    git push
  fi
fi
