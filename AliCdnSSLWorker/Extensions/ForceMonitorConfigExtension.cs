using System.Net;
using AliCdnSSLWorker.Configs;

namespace AliCdnSSLWorker.Extensions;

public static class ForceMonitorConfigExtension
{
    public static IPAddress GetIpAddress(this ForceMonitorConfig config)
        => IPAddress.TryParse(config.Ip, out var ip) ? ip : IPAddress.Any;
}
