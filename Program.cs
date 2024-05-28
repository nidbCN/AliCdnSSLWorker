using AliCdnSSLWorker;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using AliCdnSSLWorker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ApiConfig>(
    builder.Configuration.GetSection(nameof(ApiConfig))
);

builder.Services.Configure<CertConfig>(
    builder.Configuration.GetSection(nameof(CertConfig))
);

builder.Services.AddSingleton<AliCdnService>();
builder.Services.AddSingleton<CertScanService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ApiWorker>();

var host = builder.Build();
host.Run();
