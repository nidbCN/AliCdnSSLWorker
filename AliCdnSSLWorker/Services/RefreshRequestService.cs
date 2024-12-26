using AliCdnSSLWorker.Clients;

namespace AliCdnSSLWorker.Services;

public class RefreshRequestService(ILogger<RefreshRequestService> logger, RefreshRequestClient client)
{
    public async Task Update()
    {
        var req = client.RequestAsync();
        logger.LogInformation("Send refresh request to {url}", client.Client.BaseAddress);
        (await req).EnsureSuccessStatusCode();
    }

}
