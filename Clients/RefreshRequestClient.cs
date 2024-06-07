using System.Net;
using AliCdnSSLWorker.Configs;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Clients;
public class RefreshRequestClient
{
    public HttpClient Client { get; }

    public RefreshRequestClient(HttpClient httpClient, IOptions<ApiConfig> options)
    {
        var ip = options.Value.IpAddress.ToString();
        if (options.Value.IpAddress.Equals(IPAddress.Any))
        {
            ip = IPAddress.Loopback.ToString();
        }

        httpClient.BaseAddress = new($"http://{ip}:{options.Value.Port}/");
        httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            $"AliCdnSSLWorker/{GetType().Assembly.GetName().Version}"
        );

        Client = httpClient;
    }

    public async Task<HttpResponseMessage> RequestAsync()
    {
        return await Client.GetAsync("/force_refresh/");
    }
}
