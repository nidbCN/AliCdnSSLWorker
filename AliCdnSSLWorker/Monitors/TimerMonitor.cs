using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class TimerMonitor(ILogger<TimerMonitor> logger,
    IOptions<CertConfig> options,
    IOptions<TimerMonitorConfig> monitorOptions,
    AliCdnService aliCdnService,
    CertScanService certScanService) : BackgroundService
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
            .Where(i => options.Value.DomainWhiteList?.Contains(i.Name)
                        ?? options.Value.DomainBlackList?.Contains(i.Name)
                        ?? true)
            .Select(i => (i.Name, i.CertCommonName, i.CertExpireTime - now))
            .Where(tuple => tuple.Item3 <= monitorOptions.Value.RefreshInterval)
            .ToList();

        foreach (var (domain, cn, expiredTime) in willExpiredDomainList)
        {
            logger.LogInformation("CDN {cdn name} cert `{cn}` has {t:c} expire. Upload local cert.", domain, cn, expiredTime);

            logger.LogInformation("Update domain {d}", domain);
            var certPair = certScanService.GetCertByDomain(domain);
            if (certPair is null)
            {
                logger.LogError("Can not found cert for {d}", domain);
                continue;
            }

            if (aliCdnService.TryUploadCert(domain, certPair.Value))
                logger.LogInformation("Success upload cert for {d}.", domain);
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
