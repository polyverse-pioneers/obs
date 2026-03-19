using SpeedTest.Cli;

namespace SpeedTest.Tests;

public sealed class CliParsingTests
{
    [Fact]
    public void ParseRunCommand_UsesExpectedDefaults()
    {
        var result = CliApp.Parse(new[] { "run" });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Config);

        Assert.Equal("tcpdata", result.Config!.Backend);
        Assert.Equal(10 * 1024 * 1024, result.Config.DownloadSizeBytes);
        Assert.Equal(10 * 1024 * 1024, result.Config.UploadSizeBytes);
        Assert.Equal(10, result.Config.LatencySamples);
        Assert.Equal(1, result.Config.Concurrency);
        Assert.Equal(TimeSpan.FromSeconds(30), result.Config.Timeout);
        Assert.Equal("json", result.Format);
    }

    [Fact]
    public void ParseRunCommand_ReturnsCliError_ForUnknownBackend()
    {
        var result = CliApp.Parse(new[] { "run", "--backend", "bogus" });

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("backend", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRunCommand_RequiresDownloadUrl_ForCustomBackend()
    {
        var result = CliApp.Parse(new[] { "run", "--backend", "custom" });

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("download-url", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRunCommand_AllowsMissingUploadUrl_ForCustomBackend()
    {
        var result = CliApp.Parse(new[]
        {
            "run",
            "--backend", "custom",
            "--download-url", "https://example.test/download"
        });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Config);
        Assert.Equal("custom", result.Config!.Backend);
        Assert.NotNull(result.Config.DownloadUrl);
        Assert.Null(result.Config.UploadUrl);
    }

    [Fact]
    public void ParseRunCommand_ParsesRepeatableLabels_IntoMetadata()
    {
        var result = CliApp.Parse(new[]
        {
            "run",
            "--label", "host=planck",
            "--label", "region=home"
        });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Config);

        Assert.Equal("planck", result.Config!.Metadata["host"]);
        Assert.Equal("home", result.Config.Metadata["region"]);
    }

    [Fact]
    public void ParseRunCommand_AcceptsTcpDataSizeOverrides()
    {
        var result = CliApp.Parse(new[]
        {
            "run",
            "--backend", "tcpdata",
            "--download-size", "5242880",
            "--upload-size", "2097152"
        });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Config);
        Assert.Equal(5 * 1024 * 1024, result.Config!.DownloadSizeBytes);
        Assert.Equal(2 * 1024 * 1024, result.Config.UploadSizeBytes);
    }

    [Fact]
    public void ParseRunCommand_RejectsNonPositiveUploadSize_ForTcpData()
    {
        var result = CliApp.Parse(new[]
        {
            "run",
            "--backend", "tcpdata",
            "--upload-size", "0"
        });

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("upload-size", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
