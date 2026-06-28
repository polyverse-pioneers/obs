# Host Backup Scripts

These scripts write host backups under `../backups/hosts/`.

## Scripts

- `backup-planck.sh`
- `backup-hadron.sh`
- `backup-qubit.sh`
- `backup-quantum-wsl.sh`

## Usage

Run from repo root:

```bash
./scripts/backup-planck.sh
./scripts/backup-hadron.sh
./scripts/backup-qubit.sh
./scripts/backup-quantum-wsl.sh
./scripts/backup-planck.sh --git-commit
./scripts/backup-hadron.sh --git-commit
./scripts/backup-qubit.sh --git-commit
./scripts/backup-quantum-wsl.sh --git-commit
./scripts/backup-planck.sh --git-push
./scripts/backup-hadron.sh --git-push
./scripts/backup-qubit.sh --git-push
./scripts/backup-quantum-wsl.sh --git-push
```

Common git flags:

- `--git-commit`: stage backup paths and commit only when changed.
- `--git-push`: same as `--git-commit`, then push.
- `--commit-message "..."`: override default commit message.

Notes:

- `backup-hadron.sh` defaults to `HADRON_HOST=hadron`.
- Override hadron target with `HADRON_HOST=192.168.99.20` when DNS is unstable.
- Optionally set `HADRON_SSH_KEY=/path/to/key` if host alias key routing is not available.
- `backup-hadron.sh` and `backup-qubit.sh` prioritize active
  `systemd-networkd`/`systemd-resolved` config paths.
- Legacy `ifupdown` and `NetworkManager` artifacts are captured only from
  `.bak` paths when present.
- `backup-qubit.sh` uses key `~/.ssh/wsl-qubit` by default.
- Override qubit key with `QUBIT_SSH_KEY=/path/to/key`.
- `backup-qubit.sh` defaults to `QUBIT_HOST=192.168.40.20`; override with
  `QUBIT_HOST=qubit` (or another host/IP) when needed.
- `backup-quantum-wsl.sh` captures non-secret WSL resolver and host config,
  including `/etc/wsl.conf` (when present), `/etc/resolv.conf`, and `/etc/hosts`.
- `backup-quantum-wsl.sh` can optionally stage and commit only WSL backup paths
  with `--git-commit`, and can push after commit with `--git-push`.
