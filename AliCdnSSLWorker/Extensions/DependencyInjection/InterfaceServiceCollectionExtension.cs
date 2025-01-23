using AliCdnSSLWorker.CertProvider;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Monitors;
using AliCdnSSLWorker.Services;

namespace AliCdnSSLWorker.Extensions.DependencyInjection;

public static class InterfaceServiceCollectionExtension
{
    public static void AddMonitors(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<CertConfig>()
            .Bind(builder.Configuration.GetSection(nameof(CertConfig)));

        var forceMonitorConfig = builder.Configuration.GetSection(nameof(ForceMonitorConfig));
        if (forceMonitorConfig.Get<ForceMonitorConfig>()?.Enable ?? false)
        {
            builder.Services
                .AddOptions<ForceMonitorConfig>()
                .Bind(forceMonitorConfig);
            builder.Services.AddHostedService<ForceMonitor>();
        }

        var timerMonitorConfig = builder.Configuration.GetSection(nameof(TimerMonitorConfig));
        if (timerMonitorConfig.Get<TimerMonitorConfig>()?.Enable ?? false)
        {
            builder.Services
                .AddOptions<TimerMonitorConfig>()
                .Bind(timerMonitorConfig);
            builder.Services.AddHostedService<TimerMonitor>();
        }

        builder.Services.AddSingleton<CertService>();
    }

    public static void AddCertProviders(this IHostApplicationBuilder builder)
    {
        var localProviderConfig = builder.Configuration.GetSection(nameof(LocalCertProviderConfig));
        if (localProviderConfig.Get<LocalCertProviderConfig>() is not null)
        {
            builder.Services
                .AddOptions<LocalCertProviderConfig>()
                .Bind(localProviderConfig);
            builder.Services.AddSingleton<ICertProvider, LocalCertProvider>();
        }
    }
}
