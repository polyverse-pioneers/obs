# Netspeed

Deterministic .NET console speed test tool (download, upload, latency) with JSON output for Telegraf/Prometheus workflows.

## Documentation

- Specification: [docs/build.md](docs/build.md)
- Implementation plan: [docs/build-impl.md](docs/build-impl.md)
- Repository workflow and coding rules: [.github/copilot-instructions.md](.github/copilot-instructions.md)

## Scripts

### publish-all

Builds self-contained NativeAOT binaries for both Linux targets:

- `linux-x64` output: `publish/linux-x64/pip-speed`
- `linux-arm64` output: `publish/linux-arm64/pip-speed`

Run:

```bash
./publish-all
```

Notes:

- The script invokes `dotnet publish` for each runtime identifier.
- The ARM64 publish uses `clang` (`CC=clang CXX=clang++`).

### deploy

Deploys the ARM64 binary to a remote host over SSH.

- Source binary: `publish/linux-arm64/pip-speed`
- Default host: `planck-primary`
- Default destination directory: `/opt/pip-speed`

Run:

```bash
./deploy
```

What it does:

- Validates the local binary exists.
- Copies it to the remote host at `/tmp/pip-speed` via `scp`.
- Creates the destination directory with `sudo` if needed.
- Moves the binary to `/opt/pip-speed/pip-speed` and sets mode `755`.

Requirements:

- SSH access to the target host.
- `scp` and `ssh` available locally.
- `sudo` privileges on the target host.
