# 1. Project overview

**Goal:** Build a C# .NET console app that performs deterministic speed tests (download/upload/latency), with:

- **Rich CLI** (subcommands + options)
- **Pluggable test backends** (public HTTP endpoints + custom URLs)
- **JSON output** suitable for **Telegraf exec input → Prometheus**
- **No paid APIs** (free-only, no auth)
- **Solid unit tests** around measurement logic and CLI parsing

Target runtime: **.NET 8** (or 6 if you need LTS parity).

---

# 2. High-level architecture

## 2.1 Projects

Create a solution with three projects:

1. **SpeedTest.Cli**  
   - Console entry point  
   - CLI parsing, argument validation  
   - Wires options → core library  

2. **SpeedTest.Core**  
   - Core abstractions and implementations:
     - `ISpeedTestBackend`
     - `SpeedTestResult`
     - `SpeedTestConfig`
     - `LatencyResult`, `ThroughputResult`
   - HTTP client logic, measurement, JSON serialization  

3. **SpeedTest.Tests**  
   - xUnit or NUnit  
   - Tests for:
     - Throughput calculation
     - Latency measurement
     - CLI parsing
     - JSON schema stability

---

## 2.2 Core abstractions

In `SpeedTest.Core`:

- **`SpeedTestConfig`**
  - `string Backend` (e.g. `tcpdata`, `custom`)
  - `Uri? DownloadUrl`
  - `Uri? UploadUrl`
  - `int DownloadSizeBytes`
  - `int UploadSizeBytes`
  - `int LatencySamples`
  - `int Concurrency`
  - `TimeSpan Timeout`

- **`SpeedTestResult`**
  - `DateTimeOffset Timestamp`
  - `string Backend`
  - `string? Endpoint`
  - `LatencyResult Latency`
  - `ThroughputResult Download`
  - `ThroughputResult Upload`
  - `Dictionary<string,string>? Metadata`

- **`LatencyResult`**
  - `double AverageMs`
  - `double MinMs`
  - `double MaxMs`
  - `double JitterMs`
  - `int Samples`

- **`ThroughputResult`**
  - `double Mbps`
  - `long BytesTransferred`
  - `TimeSpan Duration`
    - `TimeSpan TimeToFirstByte`
    - `TimeSpan TransferDuration`

- **`ISpeedTestBackend`**
  - `Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct)`

Implement multiple backends:

- `TcpDataBackend` (using `https://tcpdata.com/speedtest`)   
- `CustomHttpBackend` (user-specified URLs)
- (Optional) `LocalFileBackend` for LAN tests

---

# 3. Backends and endpoints

## 3.1 Primary free backend: tcpdata.com

Use **tcpdata.com** speedtest API (no key required, free HTTP testing tool).   

- **Download test (GET)**  
  - Endpoint: `https://tcpdata.com/speedtest?size={sizeBytes}`  
  - Behavior: server sends `sizeBytes` of data  
    - Size source: value comes from CLI `--download-size` (default `10485760`) and is overrideable per run.
  - Measure:
        - Start stopwatch before request
        - Record **time to first byte / headers** at response header receipt
        - Measure **transfer duration** while reading the response stream fully
        - Keep existing total duration for backward compatibility
    - Compute Mbps:  
      

\[
      \text{Mbps} = \frac{\text{bytes} \cdot 8}{\text{seconds} \cdot 10^6}
      \]



- **Upload test (POST)**  
  - Endpoint: `https://tcpdata.com/speedtest`  
  - Behavior: client sends `sizeBytes` of data in body  
    - Size source: value comes from CLI `--upload-size` (default `10485760`) and is overrideable per run.
    - Validation rule for tcpdata backend: `--upload-size` must be greater than `0`.
  - Implementation:
    - Generate a buffer of zeros (or random) of requested size
    - Use `HttpClient.PostAsync` with `StreamContent` or `ByteArrayContent`
    - Measure time from first byte send to completion

- **Latency test**  
  - Use `HEAD` or small `GET` to `https://tcpdata.com/speedtest?size=1`  
  - Perform `N` samples (configurable, e.g. 10)
  - Record RTT per request
  - Compute:
    - `AverageMs`, `MinMs`, `MaxMs`
    - `JitterMs` as mean absolute deviation from average

### 3.1.1 Fallback strategy

If tcpdata is unreachable or times out:

1. Retry a small number of times (e.g. 2–3).
2. If still failing and user provided `--fallback-url`, switch to `CustomHttpBackend`.
3. If no fallback, exit with non-zero code and JSON error payload (for Telegraf).

---

## 3.2 Custom HTTP backend

Allow user to specify:

- `--download-url <url>`
- `--upload-url <url>`

Behavior:

- **Download**:
  - GET `<download-url>` with optional `?size=` parameter if supported
  - If no size parameter, just read full response and measure

- **Upload**:
  - POST `<upload-url>` with body of configured size

This supports:

- Self-hosted endpoints
- LAN tests
- Future migration to your own speedtest backend

---

# 4. CLI design

Use **System.CommandLine** or **Spectre.Console.Cli** for a rich CLI.

Implementation note (approved deviation for current build):

- Current Phase 4 implementation uses a deterministic manual parser in `SpeedTest.Cli/App.cs` instead of System.CommandLine due package/API mismatch in the current environment.
- Behavioral contract remains aligned to the same option set, defaults, and validation rules in this spec.

## 4.1 Top-level command

Executable name: `netspeed`

Usage patterns:

- Basic:
  - `netspeed run`
- With backend:
  - `netspeed run --backend tcpdata`
  - `netspeed run --backend custom --download-url https://example.com/file --upload-url https://example.com/upload`
- Output format:
  - `netspeed run --format json`
  - `netspeed run --format text`
  - `netspeed run --format prometheus`

## 4.2 Options

For `run` command:

- `--backend <tcpdata|custom>` (default: `tcpdata`)
- `--download-size <bytes>` (default: `10485760` = 10 MB)
- `--upload-size <bytes>` (default: `10485760` = 10 MB)
- `--latency-samples <int>` (default: `10`)
- `--concurrency <int>` (default: `1`, for future multi-stream)
- `--timeout <seconds>` (default: `30`)
- `--warmup-request` (optional boolean switch; runs one unmeasured pre-flight request before timing)
- `--download-url <url>` (required if `backend=custom` and no default)
- `--upload-url <url>` (optional; if omitted, skip upload)
- `--format <json|text|prometheus>` (default: `json`)
- `--label <key=value>` (repeatable; adds to metadata)

Backend-specific validation notes:

- For `backend=tcpdata`:
    - `--download-size` must be greater than `0`.
    - `--upload-size` must be greater than `0`.
    - Both options are intended as per-run override values and map directly into tcpdata request behavior.
- For `backend=custom`:
    - `--download-size` must be greater than `0`.
    - `--upload-size` may be `0` to allow upload skip behavior when combined with omitted `--upload-url`.

Exit codes:

- `0` = success
- `1` = CLI/argument error
- `2` = network/HTTP error
- `3` = internal error

## 4.3 Validation Architecture Decision

Validation logic should evolve from ad-hoc `if` chains toward a small composable validator pipeline (fluent-style rule composition) inside the CLI layer.

Decision and reasoning for current scope:

- Use an in-repo validator abstraction first (no additional package dependency).
- Keep parser and validation separated so validation rules are unit-testable in isolation.
- Split rules into:
    - common rules (sizes, timeout, format, label syntax)
    - backend-specific rules (`tcpdata`, `custom`)

Why not add a package right now:

- Repo priority explicitly favors minimizing dependencies and keeping changes small.
- Current rule set is still compact and localized to CLI parsing.
- Native AOT-friendly behavior is easier to reason about with explicit in-repo code paths.

When to revisit package adoption:

- If validation rules grow significantly or become duplicated across multiple layers.
- If richer features (rule reuse across modules, localization, advanced conditional rules) outweigh dependency cost.
- If team standardization across services requires a shared validation framework.

---

# 5. Output formats

## 5.1 JSON (for Telegraf exec input)

JSON schema (single object):

```json
{
  "timestamp": "2026-03-16T08:24:00Z",
  "backend": "tcpdata",
  "endpoint": "https://tcpdata.com/speedtest",
  "latency": {
    "average_ms": 23.4,
    "min_ms": 20.1,
    "max_ms": 30.7,
    "jitter_ms": 3.2,
    "samples": 10
  },
  "download": {
    "mbps": 215.3,
    "bytes": 10485760,
        "duration_ms": 388.0,
        "time_to_first_byte_ms": 34.2,
        "transfer_duration_ms": 353.8
  },
  "upload": {
    "mbps": 18.7,
    "bytes": 10485760,
    "duration_ms": 4475.0
  },
  "metadata": {
    "host": "planck",
    "isp": "example-isp",
        "label_region": "home",
        "run_mode": "warm"
  }
}
```

Telegraf inputs.exec config example:

```text
[[inputs.exec]]
  commands = ["netspeed run --backend tcpdata --format json"]
  timeout = "60s"
  data_format = "json"
  json_time_key = "timestamp"
  json_time_format = "2006-01-02T15:04:05Z07:00"
  tag_keys = ["backend", "endpoint", "metadata.host", "metadata.label_region"]
  json_string_fields = ["backend", "endpoint"]
```

Notes:
- CLI auto-populates `metadata.run_mode` as `warm` when `--warmup-request` is enabled, otherwise `cold`.
- Prometheus output includes metadata as metric labels, so warm/cold filtering can be done with selectors like `{run_mode="warm"}`.

### 5.1.1 Scheduled iperf3 wrapper strategy

Operational decision (2026-05-06): replace tcpdata wrapper execution with scheduled `iperf3` probing while keeping Telegraf `inputs.exec` and Prometheus-formatted output.

Recommended probe model:

- Telegraf runs wrapper every 15 minutes.
- Wrapper executes `iperf3` against one or more configured endpoints.
- Reverse TCP test (`-R`) is the canonical download metric path.
- Optional forward TCP test captures upload.
- Wrapper prints Prometheus metrics and exits `0` even on probe failure so Telegraf can ingest run-health fields.

Required runtime configuration:

- Environment variable `IPERF3_ENDPOINTS` with comma-separated `host[:port]` values.
- `iperf3` and `jq` installed on the probe host.

Optional runtime configuration:

- `IPERF3_ENABLE_UPLOAD` (`1` by default).
- `IPERF3_DURATION_SECONDS` (`30` default).
- `IPERF3_PARALLEL_STREAMS` (`1` default).
- `IPERF3_OMIT_SECONDS` (`2` default).

Emitted metric model:

- `netspeed_download_mbps{endpoint,protocol="tcp",parallel_streams}`
- `netspeed_upload_mbps{endpoint,protocol="tcp",parallel_streams}` (when upload enabled)
- `netspeed_test_duration_seconds{endpoint,direction,protocol="tcp"}`
- `netspeed_tcp_retransmits{endpoint,direction,protocol="tcp"}`
- `netspeed_run_success{endpoint,direction,protocol="tcp"}`
- `netspeed_run_exit_code{endpoint,direction,protocol="tcp"}`

Grafana guidance:

- Use `endpoint` label to compare path variance and detect source bottlenecks.
- Prefer reverse-test (`direction="download"`) series for ISP download trend analysis.
- Keep run-health panels adjacent to throughput panels to avoid mistaking probe failures for low throughput.

## 5.2 Text (human-readable)

```text
Backend: tcpdata
Endpoint: https://tcpdata.com/speedtest

Latency:
  Avg:   23.4 ms
  Min:   20.1 ms
  Max:   30.7 ms
  Jitter: 3.2 ms
  Samples: 10

Download:
  215.3 Mbps (10.0 MiB in 388 ms)
    Time to first byte: 34 ms
    Transfer duration: 354 ms

Upload:
  18.7 Mbps (10.0 MiB in 4475 ms)
```

## 5.3 Prometheus-style

Single-line metrics, suitable for scraping if you ever wrap it:

```text
netspeed_latency_ms_avg 23.4
netspeed_latency_ms_min 20.1
netspeed_latency_ms_max 30.7
netspeed_latency_ms_jitter 3.2
netspeed_download_mbps 215.3
netspeed_download_ttfb_ms 34.2
netspeed_download_transfer_ms 353.8
netspeed_upload_mbps 18.7
```

### 5.3.1 Run health metrics (for failure charting)

To make failures visible in Grafana (instead of only seeing missing performance points), the Telegraf wrapper should always emit two status metrics:

```text
netspeed_run_success 1
netspeed_run_exit_code 0
```

On failure, emit:

```text
netspeed_run_success 0
netspeed_run_exit_code <1|2|3>
```

Notes:

- Wrapper should exit `0` after printing status metrics so Telegraf can ingest failure state as metrics.
- Keep performance metrics and run health metrics on the same dashboard time range, but use a separate "Run Health" row/panels to avoid visual clutter in throughput/latency charts.

# 6. Implementation details (.NET 10, linux-arm64, AOT‑friendly)

This section defines the deterministic, AOT‑friendly implementation of the speed test tool. All code is written in standard C# and is safe for Native AOT compilation.

## 6.1 Core primitives: HTTP, timing, and configuration

### 6.1.1 SpeedTestConfig

```csharp
    public sealed class SpeedTestConfig
    {
        public string Backend { get; init; } = "tcpdata";
        public Uri? DownloadUrl { get; init; }
        public Uri? UploadUrl { get; init; }

        public int DownloadSizeBytes { get; init; } = 10 * 1024 * 1024;
        public int UploadSizeBytes { get; init; } = 10 * 1024 * 1024;

        public int LatencySamples { get; init; } = 10;
        public int Concurrency { get; init; } = 1;

        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

        public Dictionary<string, string> Metadata { get; init; } = new();
    }
```

### 6.1.2 Result types

```csharp
    public sealed class LatencyResult
    {
        public double AverageMs { get; init; }
        public double MinMs { get; init; }
        public double MaxMs { get; init; }
        public double JitterMs { get; init; }
        public int Samples { get; init; }
    }

    public sealed class ThroughputResult
    {
        public double Mbps { get; init; }
        public long BytesTransferred { get; init; }
        public TimeSpan Duration { get; init; }
        public TimeSpan TimeToFirstByte { get; init; }
        public TimeSpan TransferDuration { get; init; }
    }

    public sealed class SpeedTestResult
    {
        public DateTimeOffset Timestamp { get; init; }
        public string Backend { get; init; } = "";
        public string? Endpoint { get; init; }

        public LatencyResult Latency { get; init; } = new();
        public ThroughputResult Download { get; init; } = new();
        public ThroughputResult Upload { get; init; } = new();

        public Dictionary<string, string>? Metadata { get; init; }
    }
```

### 6.1.3 Backend abstraction

```csharp
    public interface ISpeedTestBackend
    {
        Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct);
    }
```

### 6.1.4 HttpClient provider (AOT‑safe)

```csharp
    public interface IHttpClientProvider
    {
        HttpClient Client { get; }
    }

    public sealed class DefaultHttpClientProvider : IHttpClientProvider, IDisposable
    {
        public HttpClient Client { get; }

        public DefaultHttpClientProvider(TimeSpan timeout)
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.None,
                AllowAutoRedirect = false,
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 4
            };

            Client = new HttpClient(handler)
            {
                Timeout = timeout
            };
        }

        public void Dispose() => Client.Dispose();
    }
```
### 6.1.5 Timing helper

```csharp
    public static class Timing
    {
        public static async Task<(TimeSpan Duration, T Result)> MeasureAsync<T>(Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            var result = await action().ConfigureAwait(false);
            sw.Stop();
            return (sw.Elapsed, result);
        }
    }
```

## 6.2 Throughput and latency math

### 6.2.1 Throughput

```csharp
    public static class Throughput
    {
        public static double CalculateMbps(long bytes, TimeSpan duration)
        {
            if (duration.TotalSeconds <= 0)
                return 0;

            var bits = bytes * 8.0;
            var mbits = bits / 1_000_000.0;
            return mbits / duration.TotalSeconds;
        }
    }
```

### 6.2.2 Latency + jitter

```csharp
    public static class LatencyCalculator
    {
        public static LatencyResult Compute(IReadOnlyList<double> samplesMs)
        {
            if (samplesMs.Count == 0)
                return new LatencyResult();

            var avg = samplesMs.Average();
            var jitter = samplesMs.Select(v => Math.Abs(v - avg)).Average();

            return new LatencyResult
            {
                AverageMs = avg,
                MinMs = samplesMs.Min(),
                MaxMs = samplesMs.Max(),
                JitterMs = jitter,
                Samples = samplesMs.Count
            };
        }
    }
```

## 6.3 TcpData backend (primary free backend, no API key)

### 6.3.1 Overview

- Base endpoint: https://tcpdata.com/speedtest  
- No API key required  
- Download: GET ?size={bytes}  
- Upload: POST with binary body  
- Latency: GET ?size=1  

### 6.3.2 Implementation skeleton

```csharp
    public sealed class TcpDataBackend : ISpeedTestBackend
    {
        private readonly IHttpClientProvider _http;

        public TcpDataBackend(IHttpClientProvider http) => _http = http;

        public async Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct)
        {
            var latency = await MeasureLatency(config, ct).ConfigureAwait(false);
            var download = await MeasureDownload(config, ct).ConfigureAwait(false);
            var upload = await MeasureUpload(config, ct).ConfigureAwait(false);

            return new SpeedTestResult
            {
                Timestamp = DateTimeOffset.UtcNow,
                Backend = "tcpdata",
                Endpoint = "https://tcpdata.com/speedtest",
                Latency = latency,
                Download = download,
                Upload = upload,
                Metadata = config.Metadata
            };
        }
    }
```

### 6.3.3 Latency measurement

```csharp
    private async Task<LatencyResult> MeasureLatency(SpeedTestConfig config, CancellationToken ct)
    {
        var samples = new List<double>(config.LatencySamples);

        for (int i = 0; i < config.LatencySamples; i++)
        {
            var url = "https://tcpdata.com/speedtest?size=1";

            var (duration, _) = await Timing.MeasureAsync(async () =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await _http.Client.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                ).ConfigureAwait(false);

                resp.EnsureSuccessStatusCode();
                return 0;
            }).ConfigureAwait(false);

            samples.Add(duration.TotalMilliseconds);
        }

        return LatencyCalculator.Compute(samples);
    }
```

### 6.3.4 Download measurement

```csharp
    private async Task<ThroughputResult> MeasureDownload(SpeedTestConfig config, CancellationToken ct)
    {
        var url = $"https://tcpdata.com/speedtest?size={config.DownloadSizeBytes}";

        var (duration, bytes) = await Timing.MeasureAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.Client.SendAsync(
                req,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            ).ConfigureAwait(false);

            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

            var buffer = new byte[64 * 1024];
            long total = 0;

            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
            {
                total += read;
            }

            return total;
        }).ConfigureAwait(false);

        return new ThroughputResult
        {
            Mbps = Throughput.CalculateMbps(bytes, duration),
            BytesTransferred = bytes,
            Duration = duration
        };
    }
```

### 6.3.5 Upload measurement

```csharp
    private async Task<ThroughputResult> MeasureUpload(SpeedTestConfig config, CancellationToken ct)
    {
        if (config.UploadSizeBytes <= 0)
            return new ThroughputResult();

        var url = "https://tcpdata.com/speedtest";

        using var content = new StreamContent(new RandomDataStream(config.UploadSizeBytes));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var (duration, _) = await Timing.MeasureAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            using var resp = await _http.Client.SendAsync(
                req,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            ).ConfigureAwait(false);

            resp.EnsureSuccessStatusCode();
            return 0;
        }).ConfigureAwait(false);

        return new ThroughputResult
        {
            Mbps = Throughput.CalculateMbps(config.UploadSizeBytes, duration),
            BytesTransferred = config.UploadSizeBytes,
            Duration = duration
        };
    }
```

## 6.4 RandomDataStream (upload generator)

```csharp
    public sealed class RandomDataStream : Stream
    {
        private readonly long _length;
        private long _position;
        private readonly Random _rng = new Random(12345);

        public RandomDataStream(long length)
        {
            _length = length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            var remaining = _length - _position;
            var toWrite = (int)Math.Min(count, remaining);

            _rng.NextBytes(buffer.AsSpan(offset, toWrite));
            _position += toWrite;
            return toWrite;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
```

## 6.5 Orchestration

### 6.5.1 SpeedTestRunner

```csharp
    public sealed class SpeedTestRunner
    {
        private readonly Dictionary<string, ISpeedTestBackend> _backends;

        public SpeedTestRunner(IHttpClientProvider http)
        {
            _backends = new Dictionary<string, ISpeedTestBackend>(StringComparer.OrdinalIgnoreCase)
            {
                ["tcpdata"] = new TcpDataBackend(http)
            };
        }

        public Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct)
        {
            if (!_backends.TryGetValue(config.Backend, out var backend))
                throw new InvalidOperationException($"Unknown backend: {config.Backend}");

            return backend.RunAsync(config, ct);
        }
    }
```

## 6.6 CLI entry point

### 6.6.1 Program.cs

```csharp
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var config = new SpeedTestConfig();

            using var http = new DefaultHttpClientProvider(config.Timeout);
            var runner = new SpeedTestRunner(http);

            var result = await runner.RunAsync(config, CancellationToken.None);

            Console.WriteLine($"Backend: {result.Backend}");
            Console.WriteLine($"Latency: {result.Latency.AverageMs:F2} ms");
            Console.WriteLine($"Download: {result.Download.Mbps:F2} Mbps");
            Console.WriteLine($"Upload: {result.Upload.Mbps:F2} Mbps");

            return 0;
        }
    }
```