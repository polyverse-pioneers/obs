using System.Net;
using SpeedTest.Core;

namespace SpeedTest.Tests;

internal sealed class TestHttpClientProvider : IHttpClientProvider
{
    public TestHttpClientProvider(HttpClient client)
    {
        Client = client;
    }

    public HttpClient Client { get; }
}

internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> _responder;

    public ScriptedHttpMessageHandler(Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return _responder(request, Requests.Count, cancellationToken);
    }

    public static HttpResponseMessage OkBytes(int size)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[size])
        };
    }
}
