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
```

Notes:

- `backup-qubit.sh` uses key `~/.ssh/wsl-qubit` by default.
- Override qubit key with `QUBIT_SSH_KEY=/path/to/key`.
