using System.Net;
using System.Text.Json.Serialization;

namespace AliCdnSSLWorker.Configs;
public record ApiConfig
{
    public required uint Port { get; init; } = 5057;

    [JsonConverter(typeof(JsonIPAddressConverter))]
    public required IPAddress IpAddress { get; init; } = IPAddress.Any;
}
