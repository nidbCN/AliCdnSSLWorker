﻿using AliCdnSSLWorker.CertProvider;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Models;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Services;

public class CertService(
    ILogger<CertService> logger,
    IOptions<CertConfig> options,
    IList<ICertProvider> providers)
{
    private readonly Dictionary<DomainInfo, CertInfo> _normalCertDict = [];
    private readonly IList<CertInfo> _wildcardCertList = [];

    public async Task LoadAllAsync(CancellationToken token)
        => await Parallel.ForAsync(0, providers.Count, token, async (i, innerToken) =>
        {
            var provider = providers[i];
            logger.LogInformation("Start parallel load from provider[{index}].", i);
            var list = await provider.GetAllCerts(innerToken);
            foreach (var cert in list)
            {
                if (cert.CertCommonName.IsWildcard())
                {
                    _wildcardCertList.Add(cert);
                }
                else
                {
                    _normalCertDict.Add(cert.CertCommonName, cert);
                }
            }
        });

    public bool TryGetCertByDomain(string domain, out CertInfo? result, bool forceUpdate = false)
    {
        // parse success, use domain
        if (DomainInfo.TryParse(domain, out var domainInfo))
            return TryGetCertByDomain(domainInfo, out result, forceUpdate);

        // parse failed
        result = null;
        return false;
    }

    public bool TryGetCertByDomain(DomainInfo domain, out CertInfo? result, bool forceUpdate = false)
    {
        if(forceUpdate)
            LoadAllAsync(CancellationToken.None).GetAwaiter().GetResult();

        // full-matched
        if (_normalCertDict.TryGetValue(domain, out result))
            return true;

        // wildcard matched
        return TryGetWildcardCert(domain, out result);
    }

    private bool TryGetWildcardCert(DomainInfo domain, out CertInfo? result)
    {
        result = _wildcardCertList
            .OrderBy(c => c.CertCommonName.MatchedCount(domain))
            .FirstOrDefault();

        return result is not null;
    }
}
