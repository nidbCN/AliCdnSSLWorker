using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using AliCdnSSLWorker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AliCdnConfig>(
    builder.Configuration.GetSection(nameof(AliCdnConfig))
);
builder.Services.Configure<CertConfig>(
    builder.Configuration.GetSection(nameof(CertConfig))
);

builder.Services.AddSingleton<AliCdnService>();
builder.Services.AddSingleton<CertScanService>();
builder.Services.AddHostedService<SSLWorker>();
builder.Services.AddHostedService<ApiWorker>();

var host = builder.Build();
host.Run();
