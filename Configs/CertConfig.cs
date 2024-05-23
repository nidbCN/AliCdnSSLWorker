namespace AliCdnSSLWorker.Configs;

public record CertConfig
{
    public required string CertSerchPath { get; init; }
    public bool RecursionSearch { get; init; } = true;
    public uint IntervalHour { get; init; } = 24;
    public uint CacheTimeoutMin { get; init; } = 30;
    public HashSet<string> DomainList { get; init; } = [];
}
