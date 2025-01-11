using AliCdnSSLWorker.Models;

namespace AliCdnSSLWorker.CertProvider;

public interface ICertProvider
{
    public string GetName();

    public Task<CertInfo?> GetMatchedCertByDomain(DomainInfo domain)
        => GetMatchedCertByDomain(domain, CancellationToken.None);

    public Task<CertInfo?> GetMatchedCertByDomain(DomainInfo domain, CancellationToken token);

    public Task<CertInfo?> GetMatchedCertByDomain(string identify)
        => GetMatchedCertByDomain(identify, CancellationToken.None);

    public Task<CertInfo?> GetMatchedCertByDomain(string identify, CancellationToken token);

    public Task<IList<CertInfo>> GetAllCerts()
        => GetAllCerts(CancellationToken.None);

    public Task<IList<CertInfo>> GetAllCerts(CancellationToken token);
}
