namespace SpeedTest.Core;

public interface IHttpClientProvider
{
    HttpClient Client { get; }
}
