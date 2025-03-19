using System.Text.Json;
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

    private readonly Dictionary<string, CertInfo> _normalCertDict = [];
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
            var allCerts = await provider.GetAllCerts(innerToken);
            foreach (var cert in allCerts)
            {
                // white list check
                if (_options.Value.DomainWhiteList?.All(d =>
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

                var name = cert.CertCommonName.ToString();
                if (cert.CertCommonName.IsWildcard())
                {
                    _logger.LogDebug("Cert with CN {cn} is wildcard, add to wildcard cache.", cert.CertCommonName);

                    var certInList = _wildcardCertList
                        .FirstOrDefault(c => c.CertCommonName.ToString() == name);

                    if (certInList != null)
                        _wildcardCertList.Remove(certInList);

                    _wildcardCertList.Add(cert);
                }
                else
                {
                    _logger.LogDebug("Cert with CN {cn} is not wildcard, add to normal cache.", cert.CertCommonName);
                    _normalCertDict[name] = cert;
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

        _logger.LogDebug("Search cert for domain {d}, force {f}", domain, forceUpdate);

        // full-matched
        return _normalCertDict.TryGetValue(domain.ToString(), out result) ||
               // wildcard matched
               TryGetWildcardCert(domain, out result);
    }

    private bool TryGetWildcardCert(DomainInfo domain, out CertInfo? result)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Try search wildcard cert for {d} from list.", domain);
            _logger.LogDebug("Wildcard cache list: {list}", JsonSerializer.Serialize(_wildcardCertList));
        }

        var maxMatchIndex = -1;
        var maxMatch = -1;

        for (var i = 0; i < _wildcardCertList.Count; i++)
        {
            var thisMatch = _wildcardCertList[i]
                .CertCommonName
                .MatchedCount(domain);

            if (thisMatch <= maxMatch) continue;

            maxMatch = thisMatch;
            maxMatchIndex = i;
        }

        if (maxMatch <= 0)
        {
            result = null;
            return false;
        }

        result = _wildcardCertList[maxMatchIndex];
        return true;
    }
}
