using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker;

public class Worker(ILogger<Worker> logger,
    IOptions<CertConfig> options,
    AliCdnService aliCdnService,
    CertScanService certScanService) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly AliCdnService _aliCdnService = aliCdnService;
    private readonly CertScanService _certScanService = certScanService;
    private readonly TimeSpan _interval = TimeSpan.FromHours(options.Value.IntervalHour);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_aliCdnService.TryGetHttpsCerts(out var infos))
            {
                _logger.LogError("Can not get CDN cert infos.");
                continue;
            }

            foreach (var info in infos)
            {
                var domain = info.CertCommonName;
                var expireTime = info.CertExpireTime - DateTime.Now;
                if (expireTime > _interval)
                {
                    _logger.LogInformation("Domain {cn} has {d}d,{h}hr,{m}min expire.No need refresh.", domain, expireTime.Days, expireTime.Hours, expireTime.Minutes);
                    continue;
                }

                _logger.LogInformation("Domain {cn} has {d}d,{h}hr,{m}min expire.Upload new.", domain, expireTime.Days, expireTime.Hours, expireTime.Minutes);

                var certPair = _certScanService.GetCertByDomain(domain);
                if (certPair is null)
                {
                    _logger.LogError("Can not found cert for {d}", domain);
                    continue;
                }

                if (_aliCdnService.TryUploadCert(domain, certPair.Value))
                    _logger.LogInformation("Success upload cert for {d}.", domain);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
