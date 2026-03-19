using SpeedTest.Core;

namespace SpeedTest.Cli;

public sealed class ParseResult
{
    public int ExitCode { get; init; }

    public string Format { get; init; } = "json";

    public string? Error { get; init; }

    public SpeedTestConfig? Config { get; init; }
}
