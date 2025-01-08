using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Monitors;
using AliCdnSSLWorker.Services;

namespace AliCdnSSLWorker.Extensions.DependencyInjection;

public static class InterfaceServiceCollectionExtension
{
    public static void AddMonitors(this HostApplicationBuilder builder)
    {
        builder.Services.Configure<CertConfig>(
            builder.Configuration.GetSection(nameof(CertConfig))
        );

        var forceMonitorConfig = builder.Configuration.GetSection(nameof(ForceMonitorConfig));
        if (forceMonitorConfig.Get<ForceMonitorConfig>()?.Enable ?? false)
        {
            builder.Services.Configure<ForceMonitorConfig>(forceMonitorConfig);
            builder.Services.AddHostedService<ForceMonitor>();
        }

        var timerMonitorConfig = builder.Configuration.GetSection(nameof(TimerMonitorConfig));
        if (timerMonitorConfig.Get<TimerMonitorConfig>()?.Enable ?? false)
        {
            builder.Services.Configure<TimerMonitorConfig>(timerMonitorConfig);
            builder.Services.AddHostedService<ForceMonitor>();
        }

        builder.Services.AddSingleton<CertService>();
    }
}
