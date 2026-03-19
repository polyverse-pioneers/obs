using System.Diagnostics;

namespace SpeedTest.Core;

public static class Timing
{
    public static async Task<(TimeSpan Duration, T Result)> MeasureAsync<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await action().ConfigureAwait(false);
        stopwatch.Stop();

        return (stopwatch.Elapsed, result);
    }
}
