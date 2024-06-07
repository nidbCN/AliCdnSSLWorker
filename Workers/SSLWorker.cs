using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Workers;

public class SSLWorker(ILogger<SSLWorker> logger,
    IOptions<CertConfig> options,
    AliCdnService aliCdnService,
    CertScanService certScanService) : BackgroundService
{
    private readonly CertConfig _config = options.Value;
    private readonly TimeSpan _interval = TimeSpan.FromHours(options.Value.IntervalHour);

    public bool TryUpdate()
    {
        if (!aliCdnService.TryGetHttpsCerts(out var infos))
        {
            logger.LogError("Can not get CDN cert infos.");
            return false;
        }

        var infoDict = infos
            .ToDictionary(i => i.CertCommonName, i => i.CertExpireTime);

        foreach (var domain in _config.DomainList)
        {
            if (infoDict.TryGetValue(domain, out var time))
            {
                var timeToExpiry = time - DateTime.Now;

                if (timeToExpiry > _interval)
                {
                    logger.LogInformation("Domain {cn} has {d}d,{h}hr,{m}min expire.No need refresh.", domain, timeToExpiry.Days, timeToExpiry.Hours, timeToExpiry.Minutes);
                    continue;
                }

                logger.LogInformation("Domain {cn} has {d}d,{h}hr,{m}min expire.Upload new.", domain, timeToExpiry.Days, timeToExpiry.Hours, timeToExpiry.Minutes);
            }

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

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
