#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: ./scripts/backup-quantum-wsl.sh [--git-commit] [--git-push] [--commit-message MESSAGE]

Options:
  --git-commit            Stage WSL backup paths and create a commit when there are changes.
  --git-push              Same as --git-commit, then push after a successful commit.
  --commit-message TEXT   Commit message to use with --git-commit/--git-push.
  -h, --help              Show this help message.
EOF
}

repo_root=$(cd "$(dirname "$0")/.." && pwd)
config_dir="$repo_root/backups/hosts/quantum-wsl-debian/configs/latest"
snapshot_dir="$repo_root/backups/hosts/quantum-wsl-debian/snapshots"
timestamp=$(date +"%Y-%m-%d_%H%M%S")

git_commit=0
git_push=0
commit_message="update quantum WSL backups"

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

if [[ $git_commit -eq 1 ]]; then
  cd "$repo_root"

  git_paths=(
    backups/hosts/quantum-wsl-debian/configs/latest
    backups/hosts/quantum-wsl-debian/snapshots
  )

  git add "${git_paths[@]}"

  if git diff --cached --quiet -- "${git_paths[@]}"; then
    echo "No WSL backup changes to commit"
    exit 0
  fi

  git commit -m "$commit_message"

  if [[ $git_push -eq 1 ]]; then
    git push
  fi
fi
