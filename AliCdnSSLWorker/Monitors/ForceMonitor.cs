using System.Net;
using System.Text;
using System.Text.Json;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Services;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Monitors;

public class ForceMonitor(
    ILogger<ForceMonitor> logger,
    IOptions<ForceMonitorConfig> monitorOptions,
    AliCdnService aliCdnService
    ) : BackgroundService
{
    public bool TryUpdateAll()
        => aliCdnService.TryUploadAllCert(r => true);

    private JsonSerializerOptions _jsonOption = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static void GetNotFoundResponse(HttpListenerResponse resp)
    {
        resp.StatusCode = (int)HttpStatusCode.NotFound;
    }

    private static void GetDefaultResponse(HttpListenerResponse resp)
    {
        string responseString = "HTTP Service is running successfully!";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);

        resp.ContentType = "text/plain";
        resp.ContentEncoding = Encoding.UTF8;
        resp.ContentLength64 = buffer.Length;
        resp.StatusCode = (int)HttpStatusCode.OK;

        resp.OutputStream.Write(buffer, 0, buffer.Length);
    }
    private void GetForceRefreshResponse(HttpListenerResponse resp)
    {
        try
        {
            TryUpdateAll();
            resp.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured during update all certs.");
            resp.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    private void GetCertsResponse(HttpListenerResponse resp)
    {
        try
        {
            if (!aliCdnService.TryGetRemoteCerts(out var infos))
            {
                resp.StatusCode = (int)HttpStatusCode.InternalServerError;
                resp.StatusDescription = "Certificates not found";
                return;
            }
            var json = JsonSerializer.Serialize(infos, _jsonOption);
            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.StatusCode = (int)HttpStatusCode.OK;

            using var writer = new StreamWriter(resp.OutputStream, Encoding.UTF8);
            writer.Write(json);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving certificates");
            resp.StatusCode = (int)HttpStatusCode.InternalServerError;
            resp.StatusDescription = "Internal Server Error";
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new HttpListener();

        var ip = monitorOptions.Value.GetIpAddress().ToString();
        var port = monitorOptions.Value.Port;

        if (monitorOptions.Value.GetIpAddress().Equals(IPAddress.Any))
            ip = "+";

        listener.Prefixes.Add($"http://{ip}:{port}/");

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            if (ex.ErrorCode == 5) // Access denied
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    logger.LogError($"Administrator privileges required to register URL! Please run as administrator: netsh http add urlacl url=http://{ip}:{port}/ user=Everyone");
                else
                    logger.LogError($"Administrator privileges required to register URL!");
            }
            else if (ex.ErrorCode == 32) // Port is occupied!
            {
                logger.LogError("Port is occupied!");
            }
            return;
        }
        finally
        {
            logger.LogInformation("Start api listen on {ip}:{port}.", ip, port);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var ctx = await listener.GetContextAsync();
            using var resp = ctx.Response;
            switch (ctx.Request.RawUrl)
            {
                case "/certs":
                    GetCertsResponse(resp);
                    break;
                case "/force_refresh":
                    GetForceRefreshResponse(resp);
                    break;
                case "/":
                    GetDefaultResponse(resp);
                    break;
                default:
                    GetNotFoundResponse(resp);
                    break;
            }

            resp.Close();
        }
    }
}
