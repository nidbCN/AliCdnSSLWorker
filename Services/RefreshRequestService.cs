using AliCdnSSLWorker.Clients;

namespace AliCdnSSLWorker.Services;

public class RefreshRequestService(RefreshRequestClient client)
{
    public async Task Update()
        => (await client.RequestAsync())
            .EnsureSuccessStatusCode();

}
