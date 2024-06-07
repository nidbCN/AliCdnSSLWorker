using AliCdnSSLWorker.Clients;

namespace AliCdnSSLWorker.Services;

public class RefreshRequestService(ILogger<RefreshRequestService> logger, RefreshRequestClient client)
{
    public async Task Update()
    {
        logger.LogInformation("Send refresh request to {url}", client.Client.BaseAddress);
        var resp = await client.RequestAsync();
        resp.EnsureSuccessStatusCode();
    }

}
