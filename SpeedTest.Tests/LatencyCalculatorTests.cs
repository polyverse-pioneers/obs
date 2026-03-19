using SpeedTest.Core;

namespace SpeedTest.Tests;

public sealed class LatencyCalculatorTests
{
    [Fact]
    public void Compute_ReturnsDefaults_ForEmptySamples()
    {
        var result = LatencyCalculator.Compute(Array.Empty<double>());

        Assert.Equal(0d, result.AverageMs);
        Assert.Equal(0d, result.MinMs);
        Assert.Equal(0d, result.MaxMs);
        Assert.Equal(0d, result.JitterMs);
        Assert.Equal(0, result.Samples);
    }

    [Fact]
    public void Compute_ReturnsExpectedValues_ForKnownSamples()
    {
        var result = LatencyCalculator.Compute(new[] { 10d, 20d, 30d });

        Assert.Equal(20d, result.AverageMs, precision: 6);
        Assert.Equal(10d, result.MinMs, precision: 6);
        Assert.Equal(30d, result.MaxMs, precision: 6);
        Assert.Equal(6.666667d, result.JitterMs, precision: 5);
        Assert.Equal(3, result.Samples);
    }

    [Fact]
    public void Compute_ReturnsZeroJitter_ForSingleSample()
    {
        var result = LatencyCalculator.Compute(new[] { 42d });

        Assert.Equal(42d, result.AverageMs, precision: 6);
        Assert.Equal(42d, result.MinMs, precision: 6);
        Assert.Equal(42d, result.MaxMs, precision: 6);
        Assert.Equal(0d, result.JitterMs, precision: 6);
        Assert.Equal(1, result.Samples);
    }
}
