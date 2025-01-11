using System.Net;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class ForceMonitor(
    ILogger<ForceMonitor> logger,
    IOptions<ForceMonitorConfig> monitorOptions,
    AliCdnService aliCdnService
    ) : BackgroundService
{
    private bool TryUpdateAll()
        => aliCdnService.TryUploadAllCert(r => true);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
         {
             using var listener = new HttpListener();

             var ip = monitorOptions.Value.GetIpAddress().ToString();
             var port = monitorOptions.Value.Port;

             logger.LogInformation("Start api listen on {ip}:{port}.", ip, port);

             if (monitorOptions.Value.GetIpAddress().Equals(IPAddress.Any))
                 ip = "+";

             listener.Prefixes.Add($"http://{ip}:{port}/force_refresh/");

             listener.Start();

             while (!stoppingToken.IsCancellationRequested)
             {
                 var ctx = listener.GetContext();
                 using var resp = ctx.Response;

                 try
                 {
                     TryUpdateAll();
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
