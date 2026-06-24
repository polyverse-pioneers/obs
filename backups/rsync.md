# Planck backup sync workflow

This runbook covers host backups pulled from `planck-primary` only.
WSL local host backups for `quantum-wsl-debian` are handled separately by
`./scripts/backup-quantum-wsl.sh`.

## 1) Preview changes (dry-run)

```bash
rsync -avzn --rsync-path='sudo rsync' --files-from=./backups/planck.list planck-primary:/ ./backups/
```

## 2) Sync for real

```bash
rsync -avz --rsync-path='sudo rsync' --files-from=./backups/planck.list planck-primary:/ ./backups/
```

`--delete` is intentionally omitted. With `--files-from` rooted at `/`, delete mode can propose broad removals under `./backups/`.

`--rsync-path='sudo rsync'` is required because several tracked files are only readable as root on Planck, including Grafana and nginx configuration.

## 3) Review what changed in git

```bash
git status --short backups/ .gitignore
git diff -- backups/
```

## 4) Commit and push only when there are backup changes

```bash
if ! git diff --quiet -- backups/ .gitignore; then
	git add backups/ .gitignore
	git commit -m "update Planck config backups"
	git push
else
	echo "No backup changes to commit"
fi
```

## Optional: One-command helper

```bash
backup-sync-push() {
	rsync -avz --rsync-path='sudo rsync' --files-from=./backups/planck.list planck-primary:/ ./backups/ || return 1
	if ! git diff --quiet -- backups/ .gitignore; then
		git add backups/ .gitignore
		git commit -m "update Planck config backups"
		git push
	else
		echo "No backup changes to commit"
	fi
}
```

## Optional: Controlled prune pass

Only run this if you explicitly want stale mirrored files removed.

```bash
rsync -avzn --delete --rsync-path='sudo rsync' --exclude=planck.list --exclude=rsync.md --exclude=.gitkeep --files-from=./backups/planck.list planck-primary:/ ./backups/
```

## WSL local backup workflow (`quantum-wsl-debian`)

Run this from repo root on WSL:

```bash
./scripts/backup-quantum-wsl.sh
./scripts/backup-quantum-wsl.sh --git-commit
./scripts/backup-quantum-wsl.sh --git-push
```

The script updates these tracked files under `backups/hosts/quantum-wsl-debian/configs/latest/`:

- `wsl.conf` (when `/etc/wsl.conf` exists)
- `resolv.conf`
- `hosts`
- `ssh_config` (when `~/.ssh/config` exists)

It also writes a timestamped snapshot under
`backups/hosts/quantum-wsl-debian/snapshots/`.