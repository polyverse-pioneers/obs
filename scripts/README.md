# Host Backup Scripts

These scripts write host backups under `../backups/hosts/`.

## Scripts

- `backup-planck.sh`
- `backup-qubit.sh`
- `backup-quantum-wsl.sh`

## Usage

Run from repo root:

```bash
./scripts/backup-planck.sh
./scripts/backup-qubit.sh
./scripts/backup-quantum-wsl.sh
./scripts/backup-quantum-wsl.sh --git-commit
./scripts/backup-quantum-wsl.sh --git-push
```

Notes:

- `backup-qubit.sh` uses key `~/.ssh/wsl-qubit` by default.
- Override qubit key with `QUBIT_SSH_KEY=/path/to/key`.
- `backup-qubit.sh` defaults to `QUBIT_HOST=192.168.40.20`; override with
    `QUBIT_HOST=qubit` (or another host/IP) when needed.
- `backup-quantum-wsl.sh` captures non-secret WSL resolver and host config,
  including `/etc/wsl.conf` (when present), `/etc/resolv.conf`, and `/etc/hosts`.
- `backup-quantum-wsl.sh` can optionally stage and commit only WSL backup paths
    with `--git-commit`, and can push after commit with `--git-push`.
