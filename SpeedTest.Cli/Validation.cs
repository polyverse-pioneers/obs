namespace SpeedTest.Cli;

public sealed class Validator
{
    private readonly IReadOnlyList<Func<Options, string?>> _rules;

    internal Validator(IReadOnlyList<Func<Options, string?>> rules)
    {
        _rules = rules;
    }

    public string? Validate(Options options)
    {
        foreach (var rule in _rules)
        {
            var error = rule(options);
            if (!string.IsNullOrWhiteSpace(error))
            {
                return error;
            }
        }

        return null;
    }
}

public sealed class ValidationBuilder
{
    private readonly List<Func<Options, string?>> _rules = new();

    public ValidationBuilder Rule(Func<Options, bool> predicate, string message)
    {
        _rules.Add(options => predicate(options) ? null : message);
        return this;
    }

    public ValidationBuilder RuleWhen(Func<Options, bool> appliesTo, Func<Options, bool> predicate, string message)
    {
        _rules.Add(options => !appliesTo(options) || predicate(options) ? null : message);
        return this;
    }

    public Validator Build()
    {
        return new Validator(_rules);
    }
}

public static class ValidationRules
{
    public static Validator CreateDefault()
    {
        return new ValidationBuilder()
            .Rule(options => options.Backend.Equals("tcpdata", StringComparison.OrdinalIgnoreCase) ||
                             options.Backend.Equals("custom", StringComparison.OrdinalIgnoreCase),
                "Invalid backend. Use tcpdata or custom.")
            .Rule(options => options.Format.Equals("json", StringComparison.OrdinalIgnoreCase) ||
                             options.Format.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                             options.Format.Equals("prometheus", StringComparison.OrdinalIgnoreCase),
                "Invalid format. Use json, text, or prometheus.")
            .Rule(options => options.DownloadSizeBytes > 0, "download-size must be greater than 0")
            .Rule(options => options.UploadSizeBytes >= 0, "upload-size must be 0 or greater")
            .Rule(options => options.LatencySamples > 0, "latency-samples must be greater than 0")
            .Rule(options => options.Concurrency > 0, "concurrency must be greater than 0")
            .Rule(options => options.TimeoutSeconds > 0, "timeout must be greater than 0")
            .RuleWhen(
                options => options.Backend.Equals("custom", StringComparison.OrdinalIgnoreCase),
                options => options.DownloadUrl is not null,
                "--download-url is required when backend=custom")
            .RuleWhen(
                options => options.Backend.Equals("tcpdata", StringComparison.OrdinalIgnoreCase),
                options => options.UploadSizeBytes > 0,
                "upload-size must be greater than 0 when backend=tcpdata")
            .Build();
    }
}
