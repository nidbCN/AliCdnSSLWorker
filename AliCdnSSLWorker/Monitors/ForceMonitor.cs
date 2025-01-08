using System.Net;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Extensions;
using AliCdnSSLWorker.Models;
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
    private void Update(DomainInfo domain)
    {
        logger.LogInformation("Update domain {d}", domain);

        if (certService.TryGetCertByDomain(domain, out var localCert, true))
        {
            if (aliCdnService.TryUploadCert(domain.OriginString, localCert!))
            {
                logger.LogInformation("Success upload cert for {d}.", domain);
            }
        }
        else
        {
            {
                logger.LogError("Can not found cert for {d}", domain);
            }
        }
    }

    private void UpdateAll()
    {
        foreach (var domain in certOptions.Value.DomainList)
        {
            if (DomainInfo.TryParse(domain, out var domainInfo))
            {
                Update(domainInfo);
            }
            else
            {
                logger.LogWarning("Domain string `{str}` in {opt} can not be parse as domain, skip.", domain, nameof(CertConfig));
            }
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
                 using var resp = ctx.Response;

                 try
                 {
                     UpdateAll();
                     resp.StatusCode = (int)HttpStatusCode.OK;
                 }
                 catch (Exception e)
                 {
                     logger.LogError(e, "An error occured during update all certs.");
                     resp.StatusCode = (int)HttpStatusCode.InternalServerError;
                 }
                 
                 resp.Close();
             }
         }, stoppingToken);
    }
}
