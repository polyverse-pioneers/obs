using SpeedTest.Core;

namespace SpeedTest.Tests;

public sealed class ThroughputTests
{
    [Fact]
    public void CalculateMbps_ReturnsExpectedValue_ForKnownInput()
    {
        var bytes = 10_000_000L;
        var duration = TimeSpan.FromSeconds(1);

        var mbps = Throughput.CalculateMbps(bytes, duration);

        Assert.Equal(80d, mbps, precision: 6);
    }

    [Fact]
    public void CalculateMbps_ReturnsZero_WhenDurationIsZero()
    {
        var mbps = Throughput.CalculateMbps(1234, TimeSpan.Zero);

        Assert.Equal(0d, mbps);
    }

    [Fact]
    public void CalculateMbps_ReturnsZero_WhenDurationIsNegative()
    {
        var mbps = Throughput.CalculateMbps(1234, TimeSpan.FromSeconds(-1));

        Assert.Equal(0d, mbps);
    }
}
