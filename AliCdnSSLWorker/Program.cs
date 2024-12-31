using AliCdnSSLWorker.Clients;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Monitors;
using AliCdnSSLWorker.Services;
using Microsoft.ApplicationInsights.Extensibility;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AliCdnConfig>(
    builder.Configuration.GetSection(nameof(AliCdnConfig))
);
builder.Services.Configure<CertConfig>(
    builder.Configuration.GetSection(nameof(CertConfig))
);
builder.Services.Configure<ForceMonitorConfig>(
    builder.Configuration.GetSection(nameof(ForceMonitorConfig))
);

builder.Services.Configure<TelemetryConfiguration>(
    builder.Configuration.GetSection(
        $"Logging:ApplicationInsights:{nameof(TelemetryConfiguration)}")
);

builder.Logging.AddApplicationInsights(_ => { });

builder.Services.AddHttpClient<RefreshRequestClient>();
builder.Services.AddSingleton<RefreshRequestService>();
builder.Services.AddSingleton<AliCdnService>();
builder.Services.AddSingleton<CertService>();

builder.Services.AddHostedService<TimerMonitor>();
builder.Services.AddHostedService<ForceMonitor>();

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Welcome to {name} {version}",
    typeof(Program).Assembly.GetName().Name,
    typeof(Program).Assembly.GetName().Version);

if (args.Length > 0)
{
    if (args[0] == "-r" || args[0] == "--refresh")
    {
        logger.LogWarning("Quick update mode. `-r` will request a refresh to running worker.");

        var requestService = host.Services.GetRequiredService<RefreshRequestService>();
        await requestService.Update();

        logger.LogInformation("Request force refresh success.");

        return;
    }

    logger.LogInformation("Usage: AliCdnSSLWorker [options]\n"
                          + "\t --refresh, -r \tRequest a refresh to running worker."
                          + "Notes: other arguments will pass to .NET Host.");
}

host.Run();
