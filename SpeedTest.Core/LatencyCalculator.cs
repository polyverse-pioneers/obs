namespace SpeedTest.Core;

public static class LatencyCalculator
{
    public static LatencyResult Compute(IReadOnlyList<double> samplesMs)
    {
        if (samplesMs.Count == 0)
        {
            return new LatencyResult();
        }

        var average = samplesMs.Average();
        var jitter = samplesMs.Select(value => Math.Abs(value - average)).Average();

        return new LatencyResult
        {
            AverageMs = average,
            MinMs = samplesMs.Min(),
            MaxMs = samplesMs.Max(),
            JitterMs = jitter,
            Samples = samplesMs.Count
        };
    }
}
