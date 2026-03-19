namespace SpeedTest.Cli;

public static class Program
{
	public static int Main(string[] args)
	{
		var parseResult = CliApp.Parse(args);

		if (parseResult.ExitCode != 0)
		{
			if (!string.IsNullOrWhiteSpace(parseResult.Error))
			{
				Console.Error.WriteLine(parseResult.Error);
			}

			return parseResult.ExitCode;
		}

		return 0;
	}
}
