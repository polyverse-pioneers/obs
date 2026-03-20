using SpeedTest.Core;

namespace SpeedTest.Cli;

public static class App
{
    private static readonly Validator DefaultValidator = ValidationRules.CreateDefault();

    public static ParseResult Parse(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase))
        {
            return new ParseResult
            {
                ExitCode = 1,
                Error = "Missing required command: run"
            };
        }

        var options = new Options();

        for (var i = 1; i < args.Length; i++)
        {
            var token = args[i];

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return new ParseResult
                {
                    ExitCode = 1,
                    Error = $"Unexpected argument: {token}"
                };
            }

            if (string.Equals(token, "--warmup-request", StringComparison.Ordinal))
            {
                options.WarmupRequest = true;
                continue;
            }

            if (i + 1 >= args.Length)
            {
                return new ParseResult
                {
                    ExitCode = 1,
                    Error = $"Missing value for option: {token}"
                };
            }

            var value = args[++i];

            switch (token)
            {
                case "--backend":
                    options.Backend = value;
                    break;
                case "--download-size":
                    if (!int.TryParse(value, out var downloadSize))
                    {
                        return Error("download-size must be an integer");
                    }

                    options.DownloadSizeBytes = downloadSize;

                    break;
                case "--upload-size":
                    if (!int.TryParse(value, out var uploadSize))
                    {
                        return Error("upload-size must be an integer");
                    }

                    options.UploadSizeBytes = uploadSize;

                    break;
                case "--latency-samples":
                    if (!int.TryParse(value, out var latencySamples))
                    {
                        return Error("latency-samples must be an integer");
                    }

                    options.LatencySamples = latencySamples;

                    break;
                case "--concurrency":
                    if (!int.TryParse(value, out var concurrency))
                    {
                        return Error("concurrency must be an integer");
                    }

                    options.Concurrency = concurrency;

                    break;
                case "--timeout":
                    if (!int.TryParse(value, out var timeoutSeconds))
                    {
                        return Error("timeout must be an integer");
                    }

                    options.TimeoutSeconds = timeoutSeconds;

                    break;
                case "--download-url":
                    if (!Uri.TryCreate(value, UriKind.Absolute, out var downloadUrl))
                    {
                        return Error("Invalid download-url value");
                    }

                    options.DownloadUrl = downloadUrl;
                    break;
                case "--upload-url":
                    if (!Uri.TryCreate(value, UriKind.Absolute, out var uploadUrl))
                    {
                        return Error("Invalid upload-url value");
                    }

                    options.UploadUrl = uploadUrl;
                    break;
                case "--format":
                    options.Format = value;
                    break;
                case "--label":
                    var separator = value.IndexOf('=');
                    if (separator <= 0 || separator == value.Length - 1)
                    {
                        return Error("Invalid --label value. Use key=value");
                    }

                    var key = value[..separator].Trim();
                    var labelValue = value[(separator + 1)..].Trim();
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(labelValue))
                    {
                        return Error("Invalid --label value. Use key=value");
                    }

                    options.Metadata[key] = labelValue;
                    break;
                default:
                    return Error($"Unknown option: {token}");
            }
        }

        var validationError = DefaultValidator.Validate(options);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return new ParseResult
            {
                ExitCode = 1,
                Error = validationError
            };
        }

        options.Metadata["run_mode"] = options.WarmupRequest ? "warm" : "cold";

        return new ParseResult
        {
            ExitCode = 0,
            Format = options.Format.ToLowerInvariant(),
            Config = new SpeedTestConfig
            {
                Backend = options.Backend.ToLowerInvariant(),
                DownloadUrl = options.DownloadUrl,
                UploadUrl = options.UploadUrl,
                DownloadSizeBytes = options.DownloadSizeBytes,
                UploadSizeBytes = options.UploadSizeBytes,
                LatencySamples = options.LatencySamples,
                Concurrency = options.Concurrency,
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                WarmupRequest = options.WarmupRequest,
                Metadata = options.Metadata
            }
        };

        static ParseResult Error(string message)
        {
            return new ParseResult
            {
                ExitCode = 1,
                Error = message
            };
        }
    }
}
