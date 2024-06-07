namespace AliCdnSSLWorker.Configs;

public record AliCdnConfig
{
    public required string AccessKeyId { get; init; }
    public required string AccessKeySecret { get; init; }
    public required string Endpoint { get; init; }
}
