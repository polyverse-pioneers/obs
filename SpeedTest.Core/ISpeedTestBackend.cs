namespace SpeedTest.Core;

public interface ISpeedTestBackend
{
    Task<SpeedTestResult> RunAsync(SpeedTestConfig config, CancellationToken ct);
}
