using System.Text.Json;
using SpeedTest.Core;

namespace SpeedTest.Tests;

public sealed class OutputFormatterTests
{
    private static readonly SpeedTestResult SampleResult = new()
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
            Duration = TimeSpan.FromMilliseconds(388),
            TimeToFirstByte = TimeSpan.FromMilliseconds(34),
            TransferDuration = TimeSpan.FromMilliseconds(354)
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

    // --- JSON format ---

    [Fact]
    public void Format_Json_ProducesValidJson()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        var doc = JsonDocument.Parse(output); // throws if invalid
        Assert.NotNull(doc);
    }

    [Fact]
    public void Format_Json_ContainsCoreFields()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        var root = JsonDocument.Parse(output).RootElement;

        Assert.Equal("tcpdata", root.GetProperty("backend").GetString());
        Assert.Equal("https://tcpdata.com/speedtest", root.GetProperty("endpoint").GetString());
    }

    [Fact]
    public void Format_Json_ContainsLatencyFields()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        var latency = JsonDocument.Parse(output).RootElement.GetProperty("latency");

        Assert.Equal(23.4, latency.GetProperty("average_ms").GetDouble());
        Assert.Equal(20.1, latency.GetProperty("min_ms").GetDouble());
        Assert.Equal(30.7, latency.GetProperty("max_ms").GetDouble());
        Assert.Equal(3.2, latency.GetProperty("jitter_ms").GetDouble());
        Assert.Equal(10, latency.GetProperty("samples").GetInt32());
    }

    [Fact]
    public void Format_Json_ContainsDownloadFields()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        var download = JsonDocument.Parse(output).RootElement.GetProperty("download");

        Assert.Equal(215.3, download.GetProperty("mbps").GetDouble());
        Assert.Equal(10_485_760, download.GetProperty("bytes").GetInt64());
        Assert.Equal(388, download.GetProperty("duration_ms").GetDouble(), precision: 0);
    }

    [Fact]
    public void Format_Json_ContainsUploadFields()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        var upload = JsonDocument.Parse(output).RootElement.GetProperty("upload");

        Assert.Equal(18.7, upload.GetProperty("mbps").GetDouble());
        Assert.Equal(4475, upload.GetProperty("duration_ms").GetDouble(), precision: 0);
    }

    [Fact]
    public void Format_Json_TimestampIsIso8601()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        Assert.Contains("\"timestamp\":\"2026-03-16T08:24:00", output);
    }

    [Fact]
    public void Format_Json_MetadataIsIncluded()
    {
        var output = ResultFormatter.Format(SampleResult, "json");
        Assert.Contains("\"host\":\"planck\"", output);
        Assert.Contains("\"label_region\":\"home\"", output);
    }

    // --- Text format ---

    [Fact]
    public void Format_Text_ContainsBackendAndEndpoint()
    {
        var output = ResultFormatter.Format(SampleResult, "text");
        Assert.Contains("Backend: tcpdata", output);
        Assert.Contains("Endpoint: https://tcpdata.com/speedtest", output);
    }

    [Fact]
    public void Format_Text_ContainsLatencySection()
    {
        var output = ResultFormatter.Format(SampleResult, "text");
        Assert.Contains("Latency:", output);
        Assert.Contains("23.4 ms", output);
        Assert.Contains("20.1 ms", output);
        Assert.Contains("30.7 ms", output);
        Assert.Contains("3.2 ms", output);
        Assert.Contains("Samples: 10", output);
    }

    [Fact]
    public void Format_Text_ContainsDownloadSection()
    {
        var output = ResultFormatter.Format(SampleResult, "text");
        Assert.Contains("Download:", output);
        Assert.Contains("215.3 Mbps", output);
        Assert.Contains("10.0 MiB", output);
        Assert.Contains("388 ms", output);
        Assert.Contains("Time to first byte: 34 ms", output);
        Assert.Contains("Transfer duration: 354 ms", output);
    }

    [Fact]
    public void Format_Text_ContainsUploadSection()
    {
        var output = ResultFormatter.Format(SampleResult, "text");
        Assert.Contains("Upload:", output);
        Assert.Contains("18.7 Mbps", output);
        Assert.Contains("4475 ms", output);
    }

    // --- Prometheus format ---

    [Fact]
    public void Format_Prometheus_ContainsLatencyMetrics()
    {
        var output = ResultFormatter.Format(SampleResult, "prometheus");
        Assert.Contains("netspeed_latency_ms_avg{host=\"planck\",label_region=\"home\"} 23.4", output);
        Assert.Contains("netspeed_latency_ms_min{host=\"planck\",label_region=\"home\"} 20.1", output);
        Assert.Contains("netspeed_latency_ms_max{host=\"planck\",label_region=\"home\"} 30.7", output);
        Assert.Contains("netspeed_latency_ms_jitter{host=\"planck\",label_region=\"home\"} 3.2", output);
    }

    [Fact]
    public void Format_Prometheus_ContainsThroughputMetrics()
    {
        var output = ResultFormatter.Format(SampleResult, "prometheus");
        Assert.Contains("netspeed_download_mbps{host=\"planck\",label_region=\"home\"} 215.3", output);
        Assert.Contains("netspeed_download_ttfb_ms{host=\"planck\",label_region=\"home\"} 34", output);
        Assert.Contains("netspeed_download_transfer_ms{host=\"planck\",label_region=\"home\"} 354", output);
        Assert.Contains("netspeed_upload_mbps{host=\"planck\",label_region=\"home\"} 18.7", output);
    }

    [Fact]
    public void Format_Prometheus_EachMetricOnOwnLine()
    {
        var output = ResultFormatter.Format(SampleResult, "prometheus");
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 6);
        Assert.All(lines, line => Assert.Matches(@"^netspeed_\w+(\{.*\})? \S+$", line.Trim()));
    }

    [Fact]
    public void Format_Prometheus_EscapesAndSanitizesMetadataLabels()
    {
        var result = new SpeedTestResult
        {
            Latency = new LatencyResult(),
            Download = new ThroughputResult(),
            Upload = new ThroughputResult(),
            Metadata = new Dictionary<string, string>
            {
                ["9-probe"] = "line\n\"a\""
            }
        };

        var output = ResultFormatter.Format(result, "prometheus");

        Assert.Contains("label_9_probe=\"line\\n\\\"a\\\"\"", output);
    }

    // --- Error payload ---

    [Fact]
    public void FormatError_JsonFormat_ReturnsValidJsonWithErrorField()
    {
        var output = ResultFormatter.FormatError("connection timed out", "json");
        var root = JsonDocument.Parse(output).RootElement;

        Assert.Equal("connection timed out", root.GetProperty("error").GetString());
        Assert.True(root.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public void FormatError_TextFormat_ContainsMessage()
    {
        var output = ResultFormatter.FormatError("connection timed out", "text");
        Assert.Contains("connection timed out", output);
    }

    [Fact]
    public void FormatError_PrometheusFormat_ContainsMessage()
    {
        var output = ResultFormatter.FormatError("connection timed out", "prometheus");
        Assert.Contains("connection timed out", output);
    }

    // --- Routing ---

    [Fact]
    public void Format_UnknownFormat_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ResultFormatter.Format(SampleResult, "bogus"));
    }
}
