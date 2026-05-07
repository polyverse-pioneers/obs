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

Deploys the iperf3 Telegraf wrapper to a remote host over SSH.

- Default host: `planck-primary`
- Default destination directory: `/opt/pip-speed`

Run:

```bash
./deploy
```

What it does:

- Validates the local wrapper script exists.
- Copies it to the remote host at `/tmp/pip-speed-wrapper.sh` via `scp`.
- Creates the destination directory with `sudo` if needed.
- Moves the wrapper to `/opt/pip-speed/pip-speed-wrapper.sh` and sets mode `755`.

Wrapper runtime requirements:

- `iperf3` and `jq` installed on the target host.
- `IPERF3_ENDPOINTS` set in the Telegraf service environment (comma-separated `host[:port]` list).
- Telegraf input should execute `/opt/pip-speed/pip-speed-wrapper.sh` with `data_format = "prometheus"`.

Requirements:

- SSH access to the target host.
- `scp` and `ssh` available locally.
- `sudo` privileges on the target host.
