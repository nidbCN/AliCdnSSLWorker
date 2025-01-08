using System.Net;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Extensions;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Clients;

public class RefreshRequestClient
{
    public HttpClient Client { get; }

    public RefreshRequestClient(HttpClient httpClient, IOptions<ForceMonitorConfig> options)
    {
        var ipAddress = options.Value.GetIpAddress();

        var ip = ipAddress.Equals(IPAddress.Any)
            ? IPAddress.Loopback
            : ipAddress;
        var port = options.Value.Port;

        httpClient.BaseAddress = new($"http://{ip}:{port}/");
        httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            $"{nameof(AliCdnSSLWorker)}/{GetType().Assembly.GetName().Version}"
        );

        Client = httpClient;
    }

    public async Task<HttpResponseMessage> RequestAsync()
    {
        return await Client.GetAsync("/force_refresh/");
    }
}
