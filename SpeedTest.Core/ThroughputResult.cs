using System.Text.Json.Serialization;

namespace SpeedTest.Core;

public sealed class ThroughputResult
{
    [JsonPropertyName("mbps")]
    public double Mbps { get; init; }

    [JsonPropertyName("bytes")]
    public long BytesTransferred { get; init; }

    [JsonPropertyName("duration_ms")]
    [JsonConverter(typeof(MillisecondsTimeSpanConverter))]
    public TimeSpan Duration { get; init; }
}
