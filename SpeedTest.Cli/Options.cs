namespace SpeedTest.Cli;

public sealed class Options
{
    public string Backend { get; set; } = "tcpdata";

    public string Format { get; set; } = "json";

    public int DownloadSizeBytes { get; set; } = 10 * 1024 * 1024;

    public int UploadSizeBytes { get; set; } = 10 * 1024 * 1024;

    public int LatencySamples { get; set; } = 10;

    public int Concurrency { get; set; } = 1;

    public int TimeoutSeconds { get; set; } = 30;

    public bool WarmupRequest { get; set; }

    public Uri? DownloadUrl { get; set; }

    public Uri? UploadUrl { get; set; }

    public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
}
