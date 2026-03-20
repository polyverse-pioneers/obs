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

    [JsonPropertyName("time_to_first_byte_ms")]
    [JsonConverter(typeof(MillisecondsTimeSpanConverter))]
    public TimeSpan TimeToFirstByte { get; init; }

    [JsonPropertyName("transfer_duration_ms")]
    [JsonConverter(typeof(MillisecondsTimeSpanConverter))]
    public TimeSpan TransferDuration { get; init; }
}
