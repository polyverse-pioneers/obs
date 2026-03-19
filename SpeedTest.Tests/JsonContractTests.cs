using System.Text.Json;
using SpeedTest.Core;

namespace SpeedTest.Tests;

public sealed class JsonContractTests
{
    [Fact]
    public void Serialize_UsesExpectedContractShape_AndFieldNames()
    {
        var result = new SpeedTestResult
        {
            Timestamp = DateTimeOffset.Parse("2026-03-16T08:24:00Z"),
            Backend = "tcpdata",
            Endpoint = "https://tcpdata.com/speedtest",
            Latency = new LatencyResult
            {
                AverageMs = 23.4,
                MinMs = 20.1,
                MaxMs = 30.7,
                JitterMs = 3.2,
                Samples = 10
            },
            Download = new ThroughputResult
            {
                Mbps = 215.3,
                BytesTransferred = 10_485_760,
                Duration = TimeSpan.FromMilliseconds(388)
            },
            Upload = new ThroughputResult
            {
                Mbps = 18.7,
                BytesTransferred = 10_485_760,
                Duration = TimeSpan.FromMilliseconds(4475)
            },
            Metadata = new Dictionary<string, string>
            {
                ["host"] = "planck",
                ["label_region"] = "home"
            }
        };

        var json = JsonSerializer.Serialize(result);

        Assert.Contains("\"timestamp\":\"2026-03-16T08:24:00+00:00\"", json);
        Assert.Contains("\"backend\":\"tcpdata\"", json);
        Assert.Contains("\"endpoint\":\"https://tcpdata.com/speedtest\"", json);

        Assert.Contains("\"latency\":", json);
        Assert.Contains("\"average_ms\":23.4", json);
        Assert.Contains("\"min_ms\":20.1", json);
        Assert.Contains("\"max_ms\":30.7", json);
        Assert.Contains("\"jitter_ms\":3.2", json);
        Assert.Contains("\"samples\":10", json);

        Assert.Contains("\"download\":", json);
        Assert.Contains("\"mbps\":215.3", json);
        Assert.Contains("\"bytes\":10485760", json);
        Assert.Contains("\"duration_ms\":388", json);

        Assert.Contains("\"upload\":", json);
        Assert.Contains("\"duration_ms\":4475", json);

        Assert.Contains("\"metadata\":", json);
        Assert.Contains("\"host\":\"planck\"", json);
        Assert.Contains("\"label_region\":\"home\"", json);
    }
}
