using SpeedTest.Core;

namespace SpeedTest.Cli;

public static class CliApp
{
    public static CliParseResult Parse(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase))
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "Missing required command: run"
            };
        }

        var backend = "tcpdata";
        var format = "json";
        var downloadSize = 10 * 1024 * 1024;
        var uploadSize = 10 * 1024 * 1024;
        var latencySamples = 10;
        var concurrency = 1;
        var timeoutSeconds = 30;
        string? downloadUrlText = null;
        string? uploadUrlText = null;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < args.Length; i++)
        {
            var token = args[i];

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return new CliParseResult
                {
                    ExitCode = 1,
                    Error = $"Unexpected argument: {token}"
                };
            }

            if (i + 1 >= args.Length)
            {
                return new CliParseResult
                {
                    ExitCode = 1,
                    Error = $"Missing value for option: {token}"
                };
            }

            var value = args[++i];

            switch (token)
            {
                case "--backend":
                    backend = value;
                    break;
                case "--download-size":
                    if (!int.TryParse(value, out downloadSize))
                    {
                        return Error("download-size must be an integer");
                    }

                    break;
                case "--upload-size":
                    if (!int.TryParse(value, out uploadSize))
                    {
                        return Error("upload-size must be an integer");
                    }

                    break;
                case "--latency-samples":
                    if (!int.TryParse(value, out latencySamples))
                    {
                        return Error("latency-samples must be an integer");
                    }

                    break;
                case "--concurrency":
                    if (!int.TryParse(value, out concurrency))
                    {
                        return Error("concurrency must be an integer");
                    }

                    break;
                case "--timeout":
                    if (!int.TryParse(value, out timeoutSeconds))
                    {
                        return Error("timeout must be an integer");
                    }

                    break;
                case "--download-url":
                    downloadUrlText = value;
                    break;
                case "--upload-url":
                    uploadUrlText = value;
                    break;
                case "--format":
                    format = value;
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

                    metadata[key] = labelValue;
                    break;
                default:
                    return Error($"Unknown option: {token}");
            }
        }

        if (!backend.Equals("tcpdata", StringComparison.OrdinalIgnoreCase) &&
            !backend.Equals("custom", StringComparison.OrdinalIgnoreCase))
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "Invalid backend. Use tcpdata or custom."
            };
        }

        if (!format.Equals("json", StringComparison.OrdinalIgnoreCase) &&
            !format.Equals("text", StringComparison.OrdinalIgnoreCase) &&
            !format.Equals("prometheus", StringComparison.OrdinalIgnoreCase))
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "Invalid format. Use json, text, or prometheus."
            };
        }

        if (downloadSize <= 0)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "download-size must be greater than 0"
            };
        }

        if (uploadSize < 0)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "upload-size must be 0 or greater"
            };
        }

        if (latencySamples <= 0)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "latency-samples must be greater than 0"
            };
        }

        if (concurrency <= 0)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "concurrency must be greater than 0"
            };
        }

        if (timeoutSeconds <= 0)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "timeout must be greater than 0"
            };
        }

        Uri? downloadUrl = null;
        Uri? uploadUrl = null;

        if (!string.IsNullOrWhiteSpace(downloadUrlText))
        {
            if (!Uri.TryCreate(downloadUrlText, UriKind.Absolute, out downloadUrl))
            {
                return new CliParseResult
                {
                    ExitCode = 1,
                    Error = "Invalid download-url value"
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(uploadUrlText))
        {
            if (!Uri.TryCreate(uploadUrlText, UriKind.Absolute, out uploadUrl))
            {
                return new CliParseResult
                {
                    ExitCode = 1,
                    Error = "Invalid upload-url value"
                };
            }
        }

        if (backend.Equals("custom", StringComparison.OrdinalIgnoreCase) && downloadUrl is null)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "--download-url is required when backend=custom"
            };
        }

        if (backend.Equals("tcpdata", StringComparison.OrdinalIgnoreCase) && uploadSize <= 0)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = "upload-size must be greater than 0 when backend=tcpdata"
            };
        }

        return new CliParseResult
        {
            ExitCode = 0,
            Format = format.ToLowerInvariant(),
            Config = new SpeedTestConfig
            {
                Backend = backend.ToLowerInvariant(),
                DownloadUrl = downloadUrl,
                UploadUrl = uploadUrl,
                DownloadSizeBytes = downloadSize,
                UploadSizeBytes = uploadSize,
                LatencySamples = latencySamples,
                Concurrency = concurrency,
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                Metadata = metadata
            }
        };

        static CliParseResult Error(string message)
        {
            return new CliParseResult
            {
                ExitCode = 1,
                Error = message
            };
        }
    }
}
