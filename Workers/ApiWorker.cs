using System.Net;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Workers;
public class ApiWorker(ILogger<ApiWorker> logger,
    IOptions<CertConfig> certOptions,
    IOptions<ApiConfig> apiOptions,
    AliCdnService aliCdnService,
    CertScanService certScanService
    ) : BackgroundService
{
    private readonly CertConfig _certConfig = certOptions.Value;
    private readonly ApiConfig _apiConfig = apiOptions.Value;

    private void Update()
    {
        foreach (var domain in _certConfig.DomainList)
        {
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
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
         {
             using var listener = new HttpListener();

             var ip = _apiConfig.IpAddress.MapToIPv4().ToString();

             logger.LogInformation("Start api listen on {ip}:{port}.", ip, _apiConfig.Port);

             if (_apiConfig.IpAddress.Equals(IPAddress.Any))
             {
                 ip = "+";
             }

             listener.Prefixes.Add($"http://{ip}:{_apiConfig.Port}/force_refresh/");

             listener.Start();

             while (!stoppingToken.IsCancellationRequested)
             {
                 var ctx = listener.GetContext();
                 Update();

                 using var resp = ctx.Response;
                 resp.StatusCode = (int)HttpStatusCode.OK;
             }
         }, stoppingToken);
    }
}
