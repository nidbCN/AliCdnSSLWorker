using AliCdnSSLWorker.Services;
using System.Text.Json;

namespace AliCdnSSLWorker;

public class Worker : BackgroundService
{

    private readonly ILogger<Worker> _logger;
    private readonly AliCdnService _service;

    public Worker(ILogger<Worker> logger, AliCdnService aliCdnService)
    {
        _logger = logger;
        _service = aliCdnService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                var _ = _service.TryGetHttpsCerts(out var infos);

                foreach (var info in infos)
                {
                    await Console.Out.WriteLineAsync(JsonSerializer.Serialize(info));
                }
            }
            await Task.Delay(100000, stoppingToken);
        }
    }
}
