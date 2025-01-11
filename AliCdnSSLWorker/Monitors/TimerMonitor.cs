using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class TimerMonitor(
    ILogger<TimerMonitor> logger,
    IOptions<TimerMonitorConfig> monitorOptions,
    AliCdnService aliCdnService
    ) : BackgroundService
{
    private bool TryUpdate()
    {
        // select certs expire before next update.
        var now = DateTime.Now;
        var interval = monitorOptions.Value.RefreshInterval;
        return aliCdnService.TryUploadAllCert(r => r.CertExpireDate - interval > now);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!TryUpdate())
                {
                    logger.LogWarning("Upload failed.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Upload crashed.");
            }

            await Task.Delay(monitorOptions.Value.RefreshInterval, stoppingToken);
        }
    }
}
