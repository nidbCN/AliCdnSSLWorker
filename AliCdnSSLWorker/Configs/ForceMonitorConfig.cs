using System.Net;

namespace AliCdnSSLWorker.Configs;

public record ForceMonitorConfig : MonitorConfigBase
{
    public required uint Port { get; init; } = 5057;

    public required string Ip { get; init; } = IPAddress.Loopback.ToString();

    public IPAddress GetIpAddress() => IPAddress.Parse(Ip);
}
