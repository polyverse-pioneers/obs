using SpeedTest.Core;

namespace SpeedTest.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parseResult = App.Parse(args);

        if (parseResult.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(parseResult.Error))
            {
                Console.Error.WriteLine(parseResult.Error);
            }

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
            return 0;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ResultFormatter.FormatError(ex.Message, format));
            return 2;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine(ResultFormatter.FormatError(ex.Message, format));
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ResultFormatter.FormatError(ex.Message, format));
            return 3;
        }
    }
}
