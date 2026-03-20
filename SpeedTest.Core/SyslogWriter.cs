using System.Net.Sockets;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Writes structured log messages to syslog via Unix domain socket.
/// Uses the BSD syslog protocol (RFC 3164) on /dev/log (canonical Linux).
/// </summary>
public sealed class SyslogWriter : IAsyncDisposable
{
    private UnixDomainSocketEndPoint? _endpoint;
    private Socket? _socket;
    private const string SyslogSocketPath = "/dev/log";

    // Facility codes per RFC 3164
    private const int LocalUserFacility = 1; // local1

    public SyslogWriter()
    {
        try
        {
            if (File.Exists(SyslogSocketPath))
            {
                _endpoint = new UnixDomainSocketEndPoint(SyslogSocketPath);
                _socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
            }
        }
        catch
        {
            // Socket may fail to initialize; degraded logging will fall back to stderr
        }
    }

    /// <summary>
    /// Log a message to syslog.
    /// severity: 0=emergency, 1=alert, 2=critical, 3=error, 4=warning, 5=notice, 6=info, 7=debug
    /// </summary>
    public async Task LogAsync(int severity, string tag, string message)
    {
        if (_socket is null || _endpoint is null)
        {
            return; // Syslog not available; silent fallback
        }

        try
        {
            // Priority = (Facility * 8) + Severity
            int priority = (LocalUserFacility * 8) + severity;
            
            // RFC 3164: <PRI>TAG: MESSAGE
            string syslogMessage = $"<{priority}>{tag}: {message}";
            byte[] data = Encoding.UTF8.GetBytes(syslogMessage);

            await _socket.SendToAsync(new ArraySegment<byte>(data), SocketFlags.None, _endpoint).ConfigureAwait(false);
        }
        catch
        {
            // If send fails, silently ignore (don't spam stderr)
        }
    }

    public async ValueTask DisposeAsync()
    {
        _socket?.Dispose();
        await ValueTask.CompletedTask;
    }
}
