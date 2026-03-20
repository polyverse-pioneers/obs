using System.Text.Json.Serialization;

namespace SpeedTest.Core;

[JsonSerializable(typeof(SpeedTestResult))]
[JsonSerializable(typeof(ErrorPayload))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
internal sealed partial class SpeedTestJsonContext : JsonSerializerContext
{
}
