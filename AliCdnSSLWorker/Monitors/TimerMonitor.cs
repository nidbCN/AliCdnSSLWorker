using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class TimerMonitor(ILogger<TimerMonitor> logger,
    IOptions<TimerMonitorConfig> monitorOptions,
    AliCdnService aliCdnService,
    CertService certService) : BackgroundService
{
    public bool TryUpdate()
    {
        if (!aliCdnService.TryGetRemoteCerts(out var infos))
        {
            logger.LogError("Can not get CDN cert infos.");
            return false;
        }

        // select certs expire before next update.
        var now = DateTime.Now;
        var willExpiredDomainList = infos
            .Where(i => i.CertExpireDate - now < monitorOptions.Value.RefreshInterval);

        foreach (var remoteCert in willExpiredDomainList)
        {
            logger.LogInformation("Remote CDN cert `{cn}` will expire at {t:c}. Upload local cert.", remoteCert.CertCommonName, remoteCert.CertExpireDate);

            if (certService.TryGetCertByDomain(remoteCert.CertCommonName, out var localCert))
            {
                if (aliCdnService.TryUploadCert(remoteCert.CertCommonName.OriginString, localCert!))
                    logger.LogInformation("Success upload cert for {d}.", remoteCert.CertCommonName);
            }
            else
            {
                logger.LogWarning("Can not found cert for `{cn}`, skip.", remoteCert.CertCommonName);
            }
        }

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TryUpdate();

            await Task.Delay(monitorOptions.Value.RefreshInterval, stoppingToken);
        }
    }
}
