using System.Security.Cryptography.X509Certificates;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Models;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Services;

[Obsolete]
public class CertService(
    ILogger<CertService> logger,
    IOptions<CertConfig> options)
{
    private readonly ILogger<CertService> _logger = logger;
    private DateTime _lastScan;

    private readonly Dictionary<string, CertInfo> _normalCertDict = [];
    private readonly IList<CertInfo> _wildcardCertList = [];

    private const string CertBeginFlag = "-----BEGIN CERTIFICATE-----";
    private const string CertEndFlag = "-----END CERTIFICATE-----";

    public void AddCert(string fullChain, string privateKey)
    {
        if (!fullChain.StartsWith(CertBeginFlag))
        {
            throw new ArgumentOutOfRangeException(nameof(fullChain));
        }

        var span = fullChain.AsSpan(CertBeginFlag.Length);

        if (!fullChain.EndsWith(CertBeginFlag))
        {
            throw new ArgumentOutOfRangeException(nameof(fullChain));
        }

        var content = span[..^fullChain.Length];
        var cert = new X509Certificate2(Convert.FromBase64String(content.ToString()));

        var certName = cert.GetNameInfo(X509NameType.SimpleName, false);
        if (DomainInfo.TryParse(certName, out var certDomain))
        {
            CertInfo certInfo = new()
            {
                CertCommonName = certDomain,
                CertExpireDate = cert.NotAfter,
                FullChain = fullChain,
                PrivateKey = privateKey
            };

            if (certDomain.IsWildcard())
            {
                _wildcardCertList.Add(certInfo);
            }
            else
            {
                _normalCertDict.Add(certDomain.OriginString, certInfo);
            }
        }
        else
        {
            throw new ArgumentException("Invalid public key.", nameof(fullChain));
        }
    }

    public bool TryGetCertByDomain(string domain, out CertInfo? result)
    {
        if (_normalCertDict.TryGetValue(domain, out result))
            return true;

        if (!DomainInfo.TryParse(domain, out var domainInfo))
            return false;

        return TryGetCertByDomainCore(domainInfo, out result);
    }

    public bool TryGetCertByDomain(DomainInfo domain, out CertInfo? result)
    {
        if (_normalCertDict.TryGetValue(domain.OriginString, out result))
            return true;

        return TryGetCertByDomainCore(domain, out result);
    }

    public bool TryGetCertByDomainCore(DomainInfo domain, out CertInfo? result)
    {
        result = _wildcardCertList
            .OrderBy(c => c.CertCommonName.MatchedCount(domain))
            .FirstOrDefault();

        return result is not null;
    }

}
