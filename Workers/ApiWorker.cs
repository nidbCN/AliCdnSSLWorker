using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;
using System.Net;

namespace AliCdnSSLWorker.Workers;
public class ApiWorker(ILogger<ApiWorker> logger,
    IOptions<CertConfig> options,
    AliCdnService aliCdnService,
    CertScanService certScanService
    ) : BackgroundService
{
    private readonly ILogger<ApiWorker> _logger = logger;
    private readonly AliCdnService _aliCdnService = aliCdnService;
    private readonly CertConfig _config = options.Value;
    private readonly CertScanService _certScanService = certScanService;

    private void Update()
    {
        foreach (var domain in _config.DomainList)
        {
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
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://0.0.0.0:5057/force_refresh/");

        listener.Start();
        _logger.LogInformation("Api server listen on 0.0.0.0:5057.");

        while (true)
        {
            var ctx = listener.GetContext();
            Update();
            using var resp = ctx.Response;
            resp.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
