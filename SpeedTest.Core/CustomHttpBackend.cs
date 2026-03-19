namespace SpeedTest.Core;

public sealed class CustomHttpBackend : ISpeedTestBackend
{
    private readonly IHttpClientProvider _http;

    public CustomHttpBackend(IHttpClientProvider http)
    {
        _http = http;
    }

    public async Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct)
    {
        if (config.DownloadUrl is null)
        {
            throw new InvalidOperationException("Custom backend requires DownloadUrl.");
        }

        var download = await MeasureDownloadAsync(config.DownloadUrl, ct).ConfigureAwait(false);
        var upload = await MeasureUploadAsync(config.UploadUrl, config.UploadSizeBytes, ct).ConfigureAwait(false);

        return new SpeedTestResult
        {
            Timestamp = DateTimeOffset.UtcNow,
            Backend = "custom",
            Endpoint = config.DownloadUrl.ToString(),
            Latency = new LatencyResult(),
            Download = download,
            Upload = upload,
            Metadata = config.Metadata
        };
    }

    private async Task<ThroughputResult> MeasureDownloadAsync(Uri downloadUrl, CancellationToken ct)
    {
        var (duration, bytes) = await Timing.MeasureAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
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

    private async Task<ThroughputResult> MeasureUploadAsync(Uri? uploadUrl, int uploadSizeBytes, CancellationToken ct)
    {
        if (uploadUrl is null || uploadSizeBytes <= 0)
        {
            return new ThroughputResult();
        }

        using var content = new StreamContent(new RandomDataStream(uploadSizeBytes));

        var (duration, _) = await Timing.MeasureAsync(async () =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
            {
                Content = content
            };
            using var resp = await _http.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return 0;
        }).ConfigureAwait(false);

        return new ThroughputResult
        {
            Mbps = Throughput.CalculateMbps(uploadSizeBytes, duration),
            BytesTransferred = uploadSizeBytes,
            Duration = duration
        };
    }
}
