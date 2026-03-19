using System.Text.Json.Serialization;

namespace SpeedTest.Core;

public sealed class LatencyResult
{
    [JsonPropertyName("average_ms")]
    public double AverageMs { get; init; }

    [JsonPropertyName("min_ms")]
    public double MinMs { get; init; }

    [JsonPropertyName("max_ms")]
    public double MaxMs { get; init; }

    [JsonPropertyName("jitter_ms")]
    public double JitterMs { get; init; }

    [JsonPropertyName("samples")]
    public int Samples { get; init; }
}
