namespace AliCdnSSLWorker.Models;

public class DomainCertInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime CertExpireTime { get; set; }
    public string CertCommonName { get; set; } = string.Empty;
}
