using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeedTest.Core;

public static class ResultFormatter
{
    public static string Format(SpeedTestResult result, string format) =>
        format.ToLowerInvariant() switch
        {
            "json" => FormatJson(result),
            "text" => FormatText(result),
            "prometheus" => FormatPrometheus(result),
            _ => throw new ArgumentException($"Unknown output format: '{format}'. Use json, text, or prometheus.", nameof(format))
        };

    public static string FormatError(string message, string format) =>
        format.ToLowerInvariant() == "json"
            ? FormatErrorJson(message)
            : $"Error: {message}";

    private static string FormatJson(SpeedTestResult result) =>
        JsonSerializer.Serialize(result, SpeedTestJsonContext.Default.SpeedTestResult);

    private static string FormatText(SpeedTestResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $"Backend: {result.Backend}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Endpoint: {result.Endpoint}");
        sb.AppendLine();

        sb.AppendLine("Latency:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Avg:    {result.Latency.AverageMs:G} ms");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Min:    {result.Latency.MinMs:G} ms");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Max:    {result.Latency.MaxMs:G} ms");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Jitter: {result.Latency.JitterMs:G} ms");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Samples: {result.Latency.Samples}");
        sb.AppendLine();

        var downloadMib = result.Download.BytesTransferred / (1024.0 * 1024.0);
        var downloadMs = (long)result.Download.Duration.TotalMilliseconds;
        var downloadTtfbMs = (long)result.Download.TimeToFirstByte.TotalMilliseconds;
        var downloadTransferMs = (long)result.Download.TransferDuration.TotalMilliseconds;
        sb.AppendLine("Download:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  {result.Download.Mbps:G} Mbps ({downloadMib:0.0} MiB in {downloadMs} ms)");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Time to first byte: {downloadTtfbMs} ms");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Transfer duration: {downloadTransferMs} ms");
        sb.AppendLine();

        var uploadMib = result.Upload.BytesTransferred / (1024.0 * 1024.0);
        var uploadMs = (long)result.Upload.Duration.TotalMilliseconds;
        sb.AppendLine("Upload:");
        sb.Append(CultureInfo.InvariantCulture, $"  {result.Upload.Mbps:G} Mbps ({uploadMib:0.0} MiB in {uploadMs} ms)");

        return sb.ToString();
    }

    private static string FormatPrometheus(SpeedTestResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_avg {result.Latency.AverageMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_min {result.Latency.MinMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_max {result.Latency.MaxMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_jitter {result.Latency.JitterMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_download_mbps {result.Download.Mbps:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_download_ttfb_ms {result.Download.TimeToFirstByte.TotalMilliseconds:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_download_transfer_ms {result.Download.TransferDuration.TotalMilliseconds:G}");
        sb.Append(CultureInfo.InvariantCulture, $"netspeed_upload_mbps {result.Upload.Mbps:G}");
        return sb.ToString();
    }

    private static string FormatErrorJson(string message)
    {
        var payload = new ErrorPayload
        {
            Error = message,
            Timestamp = DateTimeOffset.UtcNow
        };
        return JsonSerializer.Serialize(payload, SpeedTestJsonContext.Default.ErrorPayload);
    }
}

internal sealed class ErrorPayload
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }
}
