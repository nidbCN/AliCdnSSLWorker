using System.Net;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Extensions;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class ForceMonitor(ILogger<ForceMonitor> logger,
    IOptions<CertConfig> certOptions,
    IOptions<ForceMonitorConfig> forceMonitorOptions,
    AliCdnService aliCdnService,
    CertService certService
    ) : BackgroundService
{
    private void Update()
    {
        foreach (var domain in certOptions.Value.DomainList)
        {
            logger.LogInformation("Update domain {d}", domain);
            var certPair = certService.GetCertByDomain(domain);
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

             var ip = forceMonitorOptions.Value.GetIpAddress().ToString();
             var port = forceMonitorOptions.Value.Port;

             logger.LogInformation("Start api listen on {ip}:{port}.", ip, port);

             if (forceMonitorOptions.Value.GetIpAddress().Equals(IPAddress.Any))
                 ip = "+";

             listener.Prefixes.Add($"http://{ip}:{port}/force_refresh/");

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
