using SpeedTest.Core;

namespace SpeedTest.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        await using var syslog = new SyslogWriter();

        // Log execution start
        await syslog.LogAsync(6, "pip-speed", $"start: {string.Join(" ", args)}").ConfigureAwait(false);

        var parseResult = App.Parse(args);

        if (parseResult.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(parseResult.Error))
            {
                Console.Error.WriteLine(parseResult.Error);
            }

            // Log validation failure
            await syslog.LogAsync(4, "pip-speed", $"exit code {parseResult.ExitCode}: validation error").ConfigureAwait(false);
            return parseResult.ExitCode;
        }

        var config = parseResult.Config!;
        var format = parseResult.Format;

        using var httpProvider = new DefaultHttpClientProvider(config.Timeout);
        ISpeedTestBackend backend = config.Backend switch
        {
            "custom" => new CustomHttpBackend(httpProvider),
            _ => new TcpDataBackend(httpProvider)
        };

        try
        {
            var result = await backend.RunAsync(config, CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine(ResultFormatter.Format(result, format));
            
            // Log successful execution
            await syslog.LogAsync(6, "pip-speed", $"end: exit code 0 (success)").ConfigureAwait(false);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ResultFormatter.FormatError(ex.Message, format));
            
            // Log network error
            await syslog.LogAsync(3, "pip-speed", $"exit code 2: {ex.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine(ResultFormatter.FormatError(ex.Message, format));
            
            // Log timeout error
            await syslog.LogAsync(3, "pip-speed", $"exit code 2: {ex.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ResultFormatter.FormatError(ex.Message, format));
            
            // Log internal error
            await syslog.LogAsync(2, "pip-speed", $"exit code 3: {ex.Message}").ConfigureAwait(false);
            return 3;
        }
    }
}
