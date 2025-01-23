using AliCdnSSLWorker.Clients;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Extensions.DependencyInjection;
using AliCdnSSLWorker.Services;
using Microsoft.ApplicationInsights.Extensibility;

var builder = Host.CreateApplicationBuilder(args);

builder.AddMonitors();
builder.AddCertProviders();

builder.Services
    .AddOptions<AliCdnConfig>()
    .Bind(builder.Configuration.GetSection(nameof(AliCdnConfig)));

builder.Services.Configure<TelemetryConfiguration>(
    builder.Configuration.GetSection(
        $"Logging:ApplicationInsights:{nameof(TelemetryConfiguration)}")
);

builder.Logging.AddApplicationInsights(_ => { });

builder.Services.AddHttpClient<RefreshRequestClient>();
builder.Services.AddSingleton<RefreshRequestService>();
builder.Services.AddSingleton<AliCdnService>();

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
