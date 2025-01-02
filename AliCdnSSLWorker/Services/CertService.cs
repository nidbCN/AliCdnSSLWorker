using System.Security.Cryptography.X509Certificates;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Models;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Services;

public class CertService
{
    private readonly ILogger<CertService> _logger;
    private readonly IOptions<CertConfig> _options;
    private DateTime _lastScan;
    private readonly Dictionary<string, DomainCertInfo> _certList2 = [];

    private readonly Dictionary<string, (string, string)> _certList = [];

    private const string CertBeginFlag = "-----BEGIN CERTIFICATE-----";
    private const string CertEndFlag = "-----END CERTIFICATE-----";

    public CertService(ILogger<CertService> logger, IOptions<CertConfig> options)
    {
        _logger = logger;
        _options = options;

        ScanLocalCertAsync().GetAwaiter().GetResult();
    }

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
        var certDomain = cert.GetNameInfo(X509NameType.SimpleName, false);

    }

    public bool TryGetCertByDomain(string domain, out DomainCertInfo? result)
    {
        if (_certList2.TryGetValue(domain, out result))
        {
            return true;
        }

        var span = domain.AsSpan();
    }

    public (string, string)? GetCertByDomain(string domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        var time = DateTime.Now - _lastScan;
        if (time > TimeSpan.FromMinutes(_options.Value.CacheTimeoutMin))
        {
            ScanLocalCertAsync().Wait();
            _lastScan = DateTime.Now;
        }

        _certList.TryGetValue(domain, out var certPair);
        return certPair;
    }

    private async Task ScanLocalCertAsync()
    {
        var dir = new DirectoryInfo(_options.Value.CertSearchPath);
        if (!dir.Exists)
        {
            _logger.LogError("Dir {d} isn't exists!", dir.FullName);
            return;
        }

        var dirList = _options.Value.RecursionSearch
            ? dir.GetDirectories()
            : [dir];

        foreach (var subDir in dirList)
        {
            var certFile = subDir
                .GetFiles()
                .FirstOrDefault(f => f.Name == _options.Value.CertFileName);

            var privateKeyFile = subDir
                .GetFiles()
                .FirstOrDefault(f => f.Name == _options.Value.PrivateKeyFileName);

            if (certFile is null || privateKeyFile is null)
            {
                _logger.LogWarning("Can not found cert or private key file, skip {d}", subDir.Name);
                continue;
            }

            using var certReader = certFile.OpenText();
            var certPem = await certReader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(certPem) || !certPem.StartsWith(CertBeginFlag))
            {
                _logger.LogWarning("Can not found BEGIN CERT flag, skip.");
                continue;
            }

            // 是证书
            var certEndIndex = certPem.IndexOf(CertEndFlag, StringComparison.Ordinal);
            var certContent = certPem.Substring(CertBeginFlag.Length, certEndIndex - CertBeginFlag.Length);
            var cert = new X509Certificate2(Convert.FromBase64String(certContent));

            var certDomain = cert.GetNameInfo(X509NameType.SimpleName, false);

            IList<string> matchedDomainList;

            if (certDomain.StartsWith("*."))
            {
                matchedDomainList = _options.Value
                    .DomainList
                    .Where(certDomain.EndsWith)
                    .ToArray();
            }
            else if (_options.Value.DomainList.Contains(certDomain))
            {
                matchedDomainList = new List<string> { certDomain };
            }
            else
            {
                // 不监听此域名
                _logger.LogInformation("CN `{cert domain}` in cert not match any domain, skip.", certDomain);
                continue;
            }

            _logger.LogInformation("CN `{cert domain}` in cert matched domain `{match list}`", certDomain, string.Join(',', matchedDomainList));

            var keyPem = await privateKeyFile.OpenText().ReadToEndAsync();

            _logger.LogInformation("Success load cert, cert content: `{c}`, private `{p}`", certPem, keyPem);

            foreach (var matchDomain in matchedDomainList)
            {
                _certList[matchDomain] = (certPem, keyPem);
            }
        }
    }
}
