using AlibabaCloud.OpenApiClient.Models;
using AliCdnSSLWorker;
using AliCdnSSLWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<Config>(
    builder.Configuration.GetSection("ApiConfig")
);

builder.Services.AddSingleton<AliCdnService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
