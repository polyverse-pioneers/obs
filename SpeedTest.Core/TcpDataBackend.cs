using System.Net.Http.Headers;

namespace SpeedTest.Core;

public sealed class TcpDataBackend : ISpeedTestBackend
{
    private const string BaseUrl = "https://tcpdata.com/speedtest";
    private const int DownloadAttempts = 3;

    private readonly IHttpClientProvider _http;

    public TcpDataBackend(IHttpClientProvider http)
    {
        _http = http;
    }

    public async Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct)
    {
        var latency = await MeasureLatencyAsync(config, ct).ConfigureAwait(false);
        var download = await MeasureDownloadAsync(config, ct).ConfigureAwait(false);
        var upload = await MeasureUploadAsync(config, ct).ConfigureAwait(false);

        return new SpeedTestResult
        {
            Timestamp = DateTimeOffset.UtcNow,
            Backend = "tcpdata",
            Endpoint = BaseUrl,
            Latency = latency,
            Download = download,
            Upload = upload,
            Metadata = config.Metadata
        };
    }

    private async Task<LatencyResult> MeasureLatencyAsync(SpeedTestConfig config, CancellationToken ct)
    {
        var samples = new List<double>(config.LatencySamples);

        for (var i = 0; i < config.LatencySamples; i++)
        {
            var url = $"{BaseUrl}?size=1";

            var (duration, _) = await Timing.MeasureAsync(async () =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await _http.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                return 0;
            }).ConfigureAwait(false);

            samples.Add(duration.TotalMilliseconds);
        }

        return LatencyCalculator.Compute(samples);
    }

    private async Task<ThroughputResult> MeasureDownloadAsync(SpeedTestConfig config, CancellationToken ct)
    {
        return await ExecuteWithRetryAsync(DownloadAttempts, () => MeasureDownloadOnceAsync(config, ct)).ConfigureAwait(false);
    }

    private async Task<ThroughputResult> MeasureDownloadOnceAsync(SpeedTestConfig config, CancellationToken ct)
    {
        var url = $"{BaseUrl}?size={config.DownloadSizeBytes}";

        var (duration, bytes) = await Timing.MeasureAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
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

    private async Task<ThroughputResult> MeasureUploadAsync(SpeedTestConfig config, CancellationToken ct)
    {
        if (config.UploadSizeBytes <= 0)
        {
            return new ThroughputResult();
        }

        using var content = new StreamContent(new RandomDataStream(config.UploadSizeBytes));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var (duration, _) = await Timing.MeasureAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = content
            };

            using var resp = await _http.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
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

    private static async Task<T> ExecuteWithRetryAsync<T>(int attempts, Func<Task<T>> action)
    {
        Exception? last = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                last = ex;
            }
            catch (Exception ex)
            {
                last = ex;
                break;
            }
        }

        throw new HttpRequestException($"Request failed after {attempts} attempts.", last);
    }
}
