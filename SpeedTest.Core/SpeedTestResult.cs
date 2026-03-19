using System.Text.Json.Serialization;

namespace SpeedTest.Core;

public sealed class SpeedTestResult
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("backend")]
    public string Backend { get; init; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; init; }

    [JsonPropertyName("latency")]
    public LatencyResult Latency { get; init; } = new();

    [JsonPropertyName("download")]
    public ThroughputResult Download { get; init; } = new();

    [JsonPropertyName("upload")]
    public ThroughputResult Upload { get; init; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}
