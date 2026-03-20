using System.Net;
using SpeedTest.Core;

namespace SpeedTest.Tests;

public sealed class TcpDataBackendTests
{
    [Fact]
    public async Task RunAsync_UsesExpectedTcpDataEndpoints_AndTransfersConfiguredBytes()
    {
        var handler = new ScriptedHttpMessageHandler(async (request, _, ct) =>
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (request.Method == HttpMethod.Get && uri.Contains("size=16", StringComparison.Ordinal))
            {
                return ScriptedHttpMessageHandler.OkBytes(16);
            }

            if (request.Method == HttpMethod.Get && uri.Contains("size=1", StringComparison.Ordinal))
            {
                return ScriptedHttpMessageHandler.OkBytes(1);
            }

            if (request.Method == HttpMethod.Post && uri.EndsWith("/speedtest", StringComparison.Ordinal))
            {
                var body = await request.Content!.ReadAsByteArrayAsync(ct);
                Assert.Equal(8, body.Length);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var config = new SpeedTestConfig
        {
            LatencySamples = 2,
            DownloadSizeBytes = 16,
            UploadSizeBytes = 8,
            Metadata = new Dictionary<string, string>
            {
                ["label_region"] = "lab"
            }
        };

        var backend = new TcpDataBackend(new TestHttpClientProvider(httpClient));

        var result = await backend.RunAsync(config, CancellationToken.None);

        Assert.Equal("tcpdata", result.Backend);
        Assert.Equal("https://tcpdata.com/speedtest", result.Endpoint);
        Assert.Equal(2, result.Latency.Samples);
        Assert.Equal(16, result.Download.BytesTransferred);
        Assert.Equal(8, result.Upload.BytesTransferred);
        Assert.Equal("lab", result.Metadata!["label_region"]);
        Assert.True(result.Download.Duration >= result.Download.TimeToFirstByte);
        Assert.True(result.Download.Duration >= result.Download.TransferDuration);

        Assert.Equal(4, handler.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_RetriesThreeAttempts_WhenDownloadRequestFailsTwice()
    {
        var downloadAttempts = 0;

        var handler = new ScriptedHttpMessageHandler((request, _, _) =>
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (request.Method == HttpMethod.Get && uri.Contains("size=1", StringComparison.Ordinal))
            {
                return Task.FromResult(ScriptedHttpMessageHandler.OkBytes(1));
            }

            if (request.Method == HttpMethod.Get && uri.Contains("size=32", StringComparison.Ordinal))
            {
                downloadAttempts++;

                if (downloadAttempts < 3)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }

                return Task.FromResult(ScriptedHttpMessageHandler.OkBytes(32));
            }

            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var backend = new TcpDataBackend(new TestHttpClientProvider(httpClient));

        var result = await backend.RunAsync(
            new SpeedTestConfig
            {
                LatencySamples = 1,
                DownloadSizeBytes = 32,
                UploadSizeBytes = 1
            },
            CancellationToken.None);

        Assert.Equal(32, result.Download.BytesTransferred);
        Assert.Equal(3, downloadAttempts);
    }

    [Fact]
    public async Task RunAsync_PerformsWarmupRequest_WhenEnabled()
    {
        var handler = new ScriptedHttpMessageHandler(async (request, _, ct) =>
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (request.Method == HttpMethod.Get && uri.Contains("size=16", StringComparison.Ordinal))
            {
                return ScriptedHttpMessageHandler.OkBytes(16);
            }

            if (request.Method == HttpMethod.Get && uri.Contains("size=1", StringComparison.Ordinal))
            {
                return ScriptedHttpMessageHandler.OkBytes(1);
            }

            if (request.Method == HttpMethod.Post && uri.EndsWith("/speedtest", StringComparison.Ordinal))
            {
                var body = await request.Content!.ReadAsByteArrayAsync(ct);
                Assert.Single(body);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var backend = new TcpDataBackend(new TestHttpClientProvider(httpClient));

        var _ = await backend.RunAsync(
            new SpeedTestConfig
            {
                WarmupRequest = true,
                LatencySamples = 1,
                DownloadSizeBytes = 16,
                UploadSizeBytes = 1
            },
            CancellationToken.None);

        Assert.Equal(4, handler.Requests.Count);
    }
}
