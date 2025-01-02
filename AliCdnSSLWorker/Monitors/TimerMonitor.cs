using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class TimerMonitor(ILogger<TimerMonitor> logger,
    IOptions<CertConfig> options,
    IOptions<TimerMonitorConfig> monitorOptions,
    AliCdnService aliCdnService,
    CertService certService) : BackgroundService
{
    public bool TryUpdate()
    {
        if (!aliCdnService.TryGetHttpsCerts(out var infos))
        {
            logger.LogError("Can not get CDN cert infos.");
            return false;
        }

        var now = DateTime.Now;

        // ReSharper disable once PossibleMultipleEnumeration
        var willExpiredDomainList = infos
            .Where(i => options.Value.DomainList.Contains(i.DomainName))
            .Where(i => i.CertExpireDate - now <= monitorOptions.Value.RefreshInterval)
            .ToList();

        foreach (var cert in willExpiredDomainList)
        {
            logger.LogInformation("CDN {cdn name} cert `{cn}` has {t:c} expire. Upload local cert.", domain, cn, expiredTime);

            logger.LogInformation("Update domain {d}", cert.DomainName);
            var certPair = certService.GetCertByDomain(cert.DomainName);
            if (certPair is null)
            {
                logger.LogError("Can not found cert for {d}", cert.DomainName);
                continue;
            }

            if (aliCdnService.TryUploadCert(cert.DomainName, certPair.Value))
                logger.LogInformation("Success upload cert for {d}.", cert.DomainName);
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
