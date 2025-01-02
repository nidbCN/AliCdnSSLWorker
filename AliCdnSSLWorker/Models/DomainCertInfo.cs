namespace AliCdnSSLWorker.Models;

public record DomainCertInfo
{
    public string DomainName { get; set; } = string.Empty;
    public required DateTime CertExpireDate { get; set; }
    public required string CertCommonName { get; init; }
    public required string FullChain { get; set; }
    public required string PrivateKey { get; set; }
}
