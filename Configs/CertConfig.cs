namespace AliCdnSSLWorker.Configs;

public record CertConfig
{
    public required string CertSerchPath { get; init; }
    public bool RecursionSearch { get; init; } = true;
    public uint TntervalHour { get; init; } = 24;
}
