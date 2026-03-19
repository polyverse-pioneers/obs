using SpeedTest.Cli;

namespace SpeedTest.Tests;

public sealed class CliValidatorTests
{
    [Fact]
    public void DefaultValidator_AcceptsValidTcpDataOptions()
    {
        var validator = ValidationRules.CreateDefault();
        var options = new Options();

        var error = validator.Validate(options);

        Assert.Null(error);
    }

    [Fact]
    public void DefaultValidator_RejectsUnknownBackend()
    {
        var validator = ValidationRules.CreateDefault();
        var options = new Options
        {
            Backend = "unknown"
        };

        var error = validator.Validate(options);

        Assert.Equal("Invalid backend. Use tcpdata or custom.", error);
    }

    [Fact]
    public void DefaultValidator_RejectsMissingDownloadUrlForCustomBackend()
    {
        var validator = ValidationRules.CreateDefault();
        var options = new Options
        {
            Backend = "custom",
            DownloadUrl = null
        };

        var error = validator.Validate(options);

        Assert.Equal("--download-url is required when backend=custom", error);
    }

    [Fact]
    public void DefaultValidator_RejectsNonPositiveUploadSizeForTcpData()
    {
        var validator = ValidationRules.CreateDefault();
        var options = new Options
        {
            Backend = "tcpdata",
            UploadSizeBytes = 0
        };

        var error = validator.Validate(options);

        Assert.Equal("upload-size must be greater than 0 when backend=tcpdata", error);
    }

    [Fact]
    public void ValidationBuilder_AppliesRuleWhenOnlyWhenConditionMatches()
    {
        var validator = new ValidationBuilder()
            .RuleWhen(
                options => options.Backend.Equals("custom", StringComparison.OrdinalIgnoreCase),
                options => options.DownloadUrl is not null,
                "custom requires download url")
            .Build();

        var tcpdataOptions = new Options
        {
            Backend = "tcpdata",
            DownloadUrl = null
        };

        var customOptions = new Options
        {
            Backend = "custom",
            DownloadUrl = null
        };

        Assert.Null(validator.Validate(tcpdataOptions));
        Assert.Equal("custom requires download url", validator.Validate(customOptions));
    }
}
