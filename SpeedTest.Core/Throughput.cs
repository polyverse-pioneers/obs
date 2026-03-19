namespace SpeedTest.Core;

public static class Throughput
{
    public static double CalculateMbps(long bytes, TimeSpan duration)
    {
        if (duration.TotalSeconds <= 0)
        {
            return 0;
        }

        var bits = bytes * 8d;
        var mbits = bits / 1_000_000d;

        return mbits / duration.TotalSeconds;
    }
}
