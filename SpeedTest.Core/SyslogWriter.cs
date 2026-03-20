using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Writes structured log messages to syslog via UDP.
/// Uses the BSD syslog protocol (RFC 3164) on localhost:514.
/// </summary>
public sealed class SyslogWriter : IAsyncDisposable
{
    private readonly UdpClient _client;
    private const int SyslogPort = 514;
    private const string SyslogHost = "127.0.0.1";

    // Facility codes per RFC 3164
    private const int LocalUserFacility = 1; // local1

    public SyslogWriter()
    {
        _client = new UdpClient();
    }

    /// <summary>
    /// Log a message to syslog.
    /// severity: 0=emergency, 1=alert, 2=critical, 3=error, 4=warning, 5=notice, 6=info, 7=debug
    /// </summary>
    public async Task LogAsync(int severity, string tag, string message)
    {
        try
        {
            // Priority = (Facility * 8) + Severity
            int priority = (LocalUserFacility * 8) + severity;
            
            // RFC 3164: <PRI>TAG: MESSAGE
            string syslogMessage = $"<{priority}>{tag}: {message}";
            byte[] data = Encoding.UTF8.GetBytes(syslogMessage);

            await _client.SendAsync(data, data.Length, SyslogHost, SyslogPort).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // If syslog is unavailable, write to console as fallback
            Console.Error.WriteLine($"syslog error: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        await ValueTask.CompletedTask;
    }
}
