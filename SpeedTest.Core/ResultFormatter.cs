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
        var labels = FormatPrometheusLabels(result.Metadata);
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_avg{labels} {result.Latency.AverageMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_min{labels} {result.Latency.MinMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_max{labels} {result.Latency.MaxMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_latency_ms_jitter{labels} {result.Latency.JitterMs:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_download_mbps{labels} {result.Download.Mbps:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_download_ttfb_ms{labels} {result.Download.TimeToFirstByte.TotalMilliseconds:G}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"netspeed_download_transfer_ms{labels} {result.Download.TransferDuration.TotalMilliseconds:G}");
        sb.Append(CultureInfo.InvariantCulture, $"netspeed_upload_mbps{labels} {result.Upload.Mbps:G}");
        return sb.ToString();
    }

    private static string FormatPrometheusLabels(Dictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return string.Empty;
        }

        var labels = metadata
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{SanitizeLabelName(pair.Key)}=\"{EscapeLabelValue(pair.Value)}\"")
            .ToArray();

        return labels.Length == 0 ? string.Empty : $"{{{string.Join(',', labels)}}}";
    }

    private static string SanitizeLabelName(string key)
    {
        var chars = key.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            var valid = char.IsLetterOrDigit(ch) || ch == '_';

            if (!valid)
            {
                chars[i] = '_';
            }
        }

        if (chars.Length == 0 || !(char.IsLetter(chars[0]) || chars[0] == '_'))
        {
            return $"label_{new string(chars)}";
        }

        return new string(chars);
    }

    private static string EscapeLabelValue(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

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
