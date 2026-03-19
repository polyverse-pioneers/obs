using System.Net;

namespace SpeedTest.Core;

public sealed class DefaultHttpClientProvider : IHttpClientProvider, IDisposable
{
    public DefaultHttpClientProvider(TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
            AllowAutoRedirect = false,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 4
        };

        Client = new HttpClient(handler)
        {
            Timeout = timeout
        };
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
    }
}
