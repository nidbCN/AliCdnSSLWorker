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
    private readonly CertConfig _config = options.Value;
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

            var infoDict = infos
                .ToDictionary(i => i.CertCommonName, i => i.CertExpireTime);

            foreach (var domain in _config.DomainList)
            {
                if (infoDict.TryGetValue(domain, out var time))
                {
                    var timeToExpiry = time - DateTime.Now;

                    if (timeToExpiry > _interval)
                    {
                        _logger.LogInformation("Domain {cn} has {d}d,{h}hr,{m}min expire.No need refresh.", domain, timeToExpiry.Days, timeToExpiry.Hours, timeToExpiry.Minutes);
                        continue;
                    }

                    _logger.LogInformation("Domain {cn} has {d}d,{h}hr,{m}min expire.Upload new.", domain, timeToExpiry.Days, timeToExpiry.Hours, timeToExpiry.Minutes);
                }

		_logger.LogInformation("Update domain {d}", domain);
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
