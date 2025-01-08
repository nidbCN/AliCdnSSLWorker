namespace AliCdnSSLWorker.Models;

public record CertInfo
{
    public required DateTime CertExpireDate { get; set; }
    public required DomainInfo CertCommonName { get; init; }
    public required string? FullChain { get; set; }
    public required string? PrivateKey { get; set; }
    public required string IdentityName { get; set; }
}
