using AliCdnSSLWorker.CertProvider;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Models;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Services;

public class CertService
{
    private readonly ILogger<CertService> _logger;
    private readonly IOptions<CertConfig> _options;
    private readonly IEnumerable<ICertProvider> _providers;

    private readonly Dictionary<DomainInfo, CertInfo> _normalCertDict = [];
    private readonly IList<CertInfo> _wildcardCertList = [];

    private DateTime _lastCertUpdateTime = DateTime.Now;

    public CertService(ILogger<CertService> logger, IOptions<CertConfig> options, IEnumerable<ICertProvider> providers)
    {
        _logger = logger;
        _options = options;
        _providers = providers;

        LoadAllAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task LoadAllAsync(CancellationToken token)
    {
        await Parallel.ForEachAsync(_providers, token, async (provider, innerToken) =>
        {
            _logger.LogInformation("Start parallel load from provider `{name}`.", provider.GetName());
            var list = await provider.GetAllCerts(innerToken);
            foreach (var cert in list)
            {
                // white list check
                if (!_options.Value.DomainWhiteList?.All(d =>
                    DomainInfo.Parse(d).MatchedCount(cert.CertCommonName) == 0) ?? false)
                {
                    // not match any domain in white list
                    _logger.LogWarning("Current added cert with CN `{cn}` not match white list, skip.", cert.CertCommonName);
                    continue;
                }

                // black list check
                if (_options.Value.DomainBlackList?.Any(d =>
                    DomainInfo.Parse(d).MatchedCount(cert.CertCommonName) != 0) ?? false)
                {
                    // has matched domain in black list
                    _logger.LogWarning("Current added cert with CN `{cn}` match black list, skip.", cert.CertCommonName);
                    continue;
                }

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

        _lastCertUpdateTime = DateTime.Now;
    }

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
        forceUpdate = forceUpdate
                      || DateTime.Now - _lastCertUpdateTime >= TimeSpan.FromMinutes(_options.Value.CacheTimeoutMin);

        if (forceUpdate)
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
