namespace AliCdnSSLWorker.Configs;

public record CertConfig
{
    public required string CertSearchPath { get; init; }

    // ReSharper disable once StringLiteralTypo
    public string CertFileName { get; set; } = "fullchain.pem";
    // ReSharper disable once StringLiteralTypo
    public string PrivateKeyFileName { get; set; } = "privkey.pem";

    public bool RecursionSearch { get; init; } = true;
    public uint CacheTimeoutMin { get; init; } = 30;

    public HashSet<string> DomainList { get; init; } = [];
}
