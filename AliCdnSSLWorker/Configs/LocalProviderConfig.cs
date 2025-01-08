namespace AliCdnSSLWorker.Configs;

public record LocalCertProviderConfig
{
    public required string SearchPath { get; set; }
    public required bool RecursionSearch { get; set; }
    public required string CertFileName { get; set; } = "fullchain.pem";
    public required string PrivateKeyFileName { get; set; } = "privkey.pem";
}
