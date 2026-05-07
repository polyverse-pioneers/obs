# Netspeed Implementation Plan

Status: in-progress
Source specs: docs/build.md, .github/copilot-instructions.md

Progress:
- Document update in progress on 2026-04-08: wired the Quarto ISP evidence pack to include `docs/spectrum-network.md` in the final report so regenerated PDF output carries the Spectrum topology/call script context.
- Phase 1 completed on 2026-03-18: solution and projects scaffolded, references wired, baseline build passed with `dotnet build SpeedTest.sln -c Release`.
- Phase 2 completed on 2026-03-18: test-first math and JSON contract tests added, core models/helpers implemented, and `dotnet test SpeedTest.Tests/SpeedTest.Tests.csproj -c Release` passed (7/7).
- Phase 3 completed on 2026-03-18: implemented HTTP abstractions, deterministic upload stream, tcpdata/custom backends, and retry behavior with test-first backend coverage passing via `dotnet test SpeedTest.Tests/SpeedTest.Tests.csproj -c Release` (10/10).
- Phase 4 completed on 2026-03-18: implemented CLI parsing/validation surface and entrypoint wiring, passed test-first CLI coverage via `dotnet test SpeedTest.Tests/SpeedTest.Tests.csproj -c Release` (15/15), and added full CLI help documentation at docs/cli-help.md. Approved deviation: parser implementation is currently manual in SpeedTest.Cli/App.cs rather than System.CommandLine.
- Post-Phase-4 refactor completed on 2026-03-18: CLI validation refactored into a fluent-style validator pipeline (`ValidationBuilder`/`Validator`/`ValidationRules`) with dedicated validator unit tests added and passing.
- Phase 5 completed on 2026-03-19: implemented `ResultFormatter` in `SpeedTest.Core` (json/text/prometheus output + JSON error payload); wired `Program.cs` to instantiate backends, call `RunAsync`, format output, and map exceptions to exit codes 2/3. 18 new output formatter tests added; total 40/40 passing.- Phase 6 completed on 2026-03-19: quality gates passed (40/40 tests, clean Release build). Published self-contained artifacts to `publish/linux-x64/pip-speed` (77K) and `publish/linux-arm64/pip-speed` (76K). linux-x64 smoke check: binary executes, CLI validation paths and exit codes verified. linux-arm64 artifact confirmed as ELF aarch64 with correct layout.
- Phase 5 completed on 2026-03-19: implemented `ResultFormatter` in `SpeedTest.Core` (json/text/prometheus output + JSON error payload); wired `Program.cs` to instantiate backends, call `RunAsync`, format output, and map exceptions to exit codes 2/3. 18 new output formatter tests added; total 40/40 passing.
- Phase 6 completed on 2026-03-19: quality gates passed (40/40 tests, clean Release build). Published self-contained artifacts to `publish/linux-x64/pip-speed` (77K) and `publish/linux-arm64/pip-speed` (76K). linux-x64 smoke check: binary executes, CLI validation paths and exit codes verified. linux-arm64 artifact confirmed as ELF aarch64 with correct layout.
- Phase 7 in progress on 2026-03-20: add split download timing to isolate ISP path effects. New download timing contract records (1) time-to-first-byte/headers and (2) payload transfer duration, while preserving existing total duration and Mbps output. JSON and Prometheus outputs gain download timing breakdown metrics.
- Phase 8 completed on 2026-03-20: added optional `--warmup-request` switch to run one unmeasured pre-flight request before timing. Config and backend paths now support warmup for tcpdata/custom backends; parsing/help/spec updated and regression tests added.
- Phase 9 completed on 2026-03-20: implemented a rate-safe Telegraf wrapper strategy for tcpdata. The wrapper now rotates one download size profile per 15-minute UTC slot, runs both cold and warm measurements for that profile with `--latency-samples 5`, emits run-health metrics even on child failures, and labels outputs for Grafana by `size_profile`, `download_size`, and `run_mode`. Dashboard legends and warmup-delta panels were updated to graph per-size-profile series.
- Phase 10 completed on 2026-05-06: replaced wrapper runtime dependency on the `pip-speed` tcpdata binary with scheduled `iperf3` probes (reverse mode download, optional forward upload) while preserving Telegraf Prometheus ingestion and run-health metrics. Deployment flow now ships wrapper-only; runtime requires `iperf3`, `jq`, and `IPERF3_ENDPOINTS` in Telegraf environment.
## 1. Scope And Decisions

- Runtime: .NET 10.
- CLI framework: System.CommandLine.
- Approved deviation: Phase 4 CLI parser is implemented with a deterministic manual parser (`SpeedTest.Cli/App.cs`) to avoid package/API mismatch in this environment.
- Retry policy: 3 attempts on tcpdata path before fallback or error.
- Build outputs: self-contained artifacts for linux-x64 and linux-arm64.
- Targets: Debian 13 x64 (current WSL host) and Debian 13 arm64.
- In scope for v1: TcpDataBackend and CustomHttpBackend.
- Out of scope for v1: LocalFileBackend and true multi-stream concurrency implementation.
- Deferred decision: fallback-url semantics and exact behavior.

## 2. Workflow Requirements (From Repo Instructions)

- Follow test-first delivery: write failing tests, then implement minimum code to pass.
- Keep changes small and dependencies minimal.
- Prefer deterministic tests with no real network calls.
- Validate all external inputs at boundaries.
- Include actionable error context and never swallow exceptions.
- Mark specs as implemented only after tests pass and code is merged.

## 3. Phased Implementation Plan

## Phase 0 - Planning And Traceability

1. Confirm requirements from build.md plus chat updates.
2. Keep this file updated as implementation source of truth.
3. Record deferred fallback semantics as unresolved requirement.

Acceptance criteria:
- This file reflects current requirements and decisions.
- Deferred items are explicitly listed.

## Phase 1 - Solution Scaffold

1. Create SpeedTest.sln with:
   - SpeedTest.Cli
   - SpeedTest.Core
   - SpeedTest.Tests
2. Configure all projects for .NET 10.
3. Wire project references:
   - SpeedTest.Cli -> SpeedTest.Core
   - SpeedTest.Tests -> SpeedTest.Core (and CLI parsing surface as needed)

Acceptance criteria:
- dotnet build succeeds for scaffolded projects.
- No unnecessary third-party packages added.

## Phase 2 - Core Contracts And Math (Test First)

1. Add failing tests for:
   - Throughput.CalculateMbps formula and edge cases.
   - LatencyCalculator.Compute average/min/max/jitter (MAD) and edge cases.
2. Implement only required core types and helpers:
   - SpeedTestConfig
   - SpeedTestResult
   - LatencyResult
   - ThroughputResult
   - Timing
   - Throughput
   - LatencyCalculator
3. Add JSON schema tests for stable field names and expected shape.

Acceptance criteria:
- All new unit tests pass.
- JSON contract uses snake_case keys and expected duration_ms/timestamp fields.

## Phase 3 - Backends And Retry (Test First)

1. Add failing tests with mocked HttpMessageHandler for:
   - Tcpdata latency/download/upload paths.
   - Byte counting and request behavior.
   - Error behavior and retry count.
2. Implement:
   - IHttpClientProvider
   - DefaultHttpClientProvider
   - RandomDataStream (deterministic)
   - TcpDataBackend
3. Add tests and implementation for retry behavior set to 3 attempts.
4. Implement CustomHttpBackend:
   - Required download URL behavior.
   - Optional upload URL behavior (skip upload when omitted).
5. Keep fallback-url behavior pluggable but unresolved.

Acceptance criteria:
- Backend tests pass without real network.
- Retry behavior is verifiably 3 attempts.
- Custom backend upload skip behavior is covered by tests.

## Phase 4 - CLI Parsing And Validation (Test First)

1. Add failing CLI tests for:
   - run command options and defaults.
   - backend-specific requirements.
   - invalid argument handling.
2. Implement CLI parser for netspeed run (target framework choice is System.CommandLine; approved temporary deviation allows manual parser implementation).
3. Map options into SpeedTestConfig.
4. Enforce exit code mapping:
   - 0 success
   - 1 CLI or argument error
   - 2 network or HTTP error
   - 3 internal error
5. Parse repeated --label key=value metadata entries.

Acceptance criteria:
- CLI parsing tests pass.
- Exit code mapping is deterministic and tested.
- Deviation is documented if parser implementation differs from the target framework choice.

## Phase 5 - Output Formatting And Error Payloads (Test First)

1. Add failing output tests for:
   - json format matching schema.
   - text format structure.
   - prometheus line metrics.
2. Implement formatter pipeline for json/text/prometheus.
3. Add and implement JSON error payload tests for json mode failures.

Acceptance criteria:
- Output tests pass and remain stable.
- JSON output is Telegraf-compatible per spec.

## Phase 6 - Publish And Verification

1. Run quality gates:
   - dotnet test -c Release
   - dotnet build -c Release
2. Publish self-contained artifacts:
   - dotnet publish SpeedTest.Cli -c Release -r linux-x64 --self-contained true
   - dotnet publish SpeedTest.Cli -c Release -r linux-arm64 --self-contained true
3. Perform smoke checks:
   - run linux-x64 output in current Debian 13 WSL.
   - validate linux-arm64 artifact layout and packaging readiness.

Acceptance criteria:
- Tests are green.
- Both self-contained artifacts are produced successfully.
- Smoke checks documented.

## Phase 7 - Split Download Timing (Test First)

1. Add failing tests for download timing breakdown fields:
   - total download duration remains populated
   - time-to-first-byte is captured
   - transfer duration is captured
2. Update core model contract:
   - extend `ThroughputResult` with `TimeToFirstByte` and `TransferDuration`
3. Update backends:
   - `TcpDataBackend` download path records separate timing phases
   - `CustomHttpBackend` download path records separate timing phases
4. Update output surfaces:
   - JSON includes `time_to_first_byte_ms` and `transfer_duration_ms` for download
   - Prometheus output includes `netspeed_download_ttfb_ms` and `netspeed_download_transfer_ms`

Acceptance criteria:
- New tests pass and existing tests remain green.
- Existing fields remain backward-compatible.
- Output includes new split download timing fields/metrics.

## Phase 10 - Telegraf Throughput Probe Migration

1. Replace tcpdata-wrapper invocation pattern with `iperf3` scheduled probing in `pip-speed-wrapper.sh`.
2. Use reverse-mode (`-R`) tests as download proxy and emit `netspeed_download_mbps` in Prometheus format.
3. Optionally run forward upload tests and emit `netspeed_upload_mbps`.
4. Continue emitting `netspeed_run_success` and `netspeed_run_exit_code` so failure visibility is preserved in Grafana.
5. Update deployment and Telegraf config docs to reflect wrapper-only rollout and required environment variables.

Acceptance criteria:
- Wrapper no longer depends on `/opt/pip-speed/pip-speed` binary.
- Telegraf `inputs.exec` remains Prometheus-formatted and interval-based.
- Run health metrics are emitted for both success and failure cases.
- Runtime requirements (`iperf3`, `jq`, `IPERF3_ENDPOINTS`) are documented.

## 4. Test Matrix

- Throughput math correctness and boundaries.
- Latency aggregate correctness and jitter MAD behavior.
- CLI parsing defaults, validation failures, and backend option rules.
- JSON schema stability and field naming contract.
- Backend retry behavior (exactly 3 attempts).
- Backend behavior with mocked HTTP only.
- Exit code mapping for CLI/network/internal failures.

## 5. Security And Correctness Checks

- No secrets in source, logs, or tests.
- Validate URL and numeric option boundaries before execution.
- Preserve explicit types and pure logic where possible.
- Avoid broad exception swallowing; wrap with actionable context.

## 6. Deferred Items

- Final fallback-url contract and semantics.
- Native AOT-specific enforcement beyond current AOT-friendly code patterns.

## 7. Completion Criteria

Implementation is complete when:

1. All phase acceptance criteria are satisfied.
2. All required tests pass deterministically.
3. Self-contained artifacts are produced for both target architectures.
4. Spec docs can be marked implemented after merge.
