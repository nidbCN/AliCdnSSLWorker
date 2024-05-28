using AliCdnSSLWorker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AliCdnSSLWorker.Workers;
public class ApiWorker(ILogger<ApiWorker> logger, Worker worker) : BackgroundService
{
    private readonly ILogger<ApiWorker> logger;
    private readonly Worker _worker = worker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://0.0.0.0:5057/force_refresh");

        listener.Start();
        logger.LogInformation("Api server listen on 0.0.0.0:5057.");

        while (true)
        {
            var ctx = await listener.GetContextAsync();
            using var resp = ctx.Response;
            if (_worker.TryUpdate())
            {
                resp.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                resp.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
    }
}
