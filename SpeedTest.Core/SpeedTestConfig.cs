namespace SpeedTest.Core;

public sealed class SpeedTestConfig
{
    public string Backend { get; init; } = "tcpdata";

    public Uri? DownloadUrl { get; init; }

    public Uri? UploadUrl { get; init; }

    public int DownloadSizeBytes { get; init; } = 10 * 1024 * 1024;

    public int UploadSizeBytes { get; init; } = 10 * 1024 * 1024;

    public int LatencySamples { get; init; } = 10;

    public int Concurrency { get; init; } = 1;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public bool WarmupRequest { get; init; }

    public Dictionary<string, string> Metadata { get; init; } = new();
}
