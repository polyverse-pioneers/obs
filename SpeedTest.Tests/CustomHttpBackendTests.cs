using System.Net;
using SpeedTest.Core;

namespace SpeedTest.Tests;

public sealed class CustomHttpBackendTests
{
    [Fact]
    public async Task RunAsync_SkipsUpload_WhenUploadUrlIsMissing()
    {
        var handler = new ScriptedHttpMessageHandler((request, _, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(ScriptedHttpMessageHandler.OkBytes(24));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var backend = new CustomHttpBackend(new TestHttpClientProvider(httpClient));

        var result = await backend.RunAsync(
            new SpeedTestConfig
            {
                Backend = "custom",
                DownloadUrl = new Uri("https://example.test/download"),
                UploadUrl = null,
                DownloadSizeBytes = 24,
                UploadSizeBytes = 10,
                LatencySamples = 1
            },
            CancellationToken.None);

        Assert.Equal("custom", result.Backend);
        Assert.Equal(24, result.Download.BytesTransferred);
        Assert.Equal(0, result.Upload.BytesTransferred);
        Assert.True(result.Download.Duration >= result.Download.TimeToFirstByte);
        Assert.True(result.Download.Duration >= result.Download.TransferDuration);

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
    }
}
