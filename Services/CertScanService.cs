using System.Security.Cryptography.X509Certificates;
using System.Text;
using AliCdnSSLWorker.Configs;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.Services;

public class CertScanService
{
    private readonly ILogger<CertScanService> _logger;
    private readonly IOptions<CertConfig> _options;
    private DateTime _lastScan;
    private readonly HashSet<string> _domainList;

    private readonly Dictionary<string, (string, string)> _certList = [];

    public CertScanService(ILogger<CertScanService> logger, IOptions<CertConfig> options)
    {
        _logger = logger;
        _options = options;

        _domainList = _options.Value.DomainList;

        ScanCertAsync().GetAwaiter().GetResult();
    }

    public (string, string)? GetCertByDomain(string domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        var time = DateTime.Now - _lastScan;
        if (time > TimeSpan.FromMinutes(_options.Value.CacheTimeoutMin))
        {
            ScanCertAsync().Wait();
            _lastScan = DateTime.Now;
        }

        _certList.TryGetValue(domain, out var certPair);
        return certPair;
    }

    private async Task ScanCertAsync()
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

            await using var certStream = certFile.OpenRead();
            using var certReader = new StreamReader(certStream);
            var line = await certReader.ReadLineAsync();

            const string certBeginFlag = "-----BEGIN CERTIFICATE-----";
            const string certEndFlag = "-----END CERTIFICATE-----";

            if (line is null || !line.Contains(certBeginFlag))
            {
                _logger.LogWarning("Can not found BEGIN CERT flag, skip.");
                continue;
            }

            // 是证书
            var stringBuilder = new StringBuilder((int)certFile.Length);

            while (!string.IsNullOrWhiteSpace(line = await certReader.ReadLineAsync()))
            {
                if (line.Contains(certEndFlag))
                    break;

                stringBuilder.Append(line);
            }

            var certContent = stringBuilder.ToString();
            var cert = new X509Certificate2(Convert.FromBase64String(certContent));

            var certDomain = cert.GetNameInfo(X509NameType.SimpleName, false);

            IList<string> matchedDomainList;

            if (certDomain.StartsWith("*."))
            {
                matchedDomainList = _domainList
                    .Where(certDomain.EndsWith)
                    .ToArray();
            }
            else if (_domainList.Contains(certDomain))
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

            using var keyReader = new StreamReader(privateKeyFile.OpenRead());
            var keyPem = await keyReader.ReadToEndAsync();
            certStream.Seek(0, SeekOrigin.Begin);
            var certPem = await certReader.ReadToEndAsync();

            _logger.LogInformation("Success load cert, cert content: `{c}`, private `{p}`", certPem, keyPem);

            foreach (var matchDomain in matchedDomainList)
            {
                _certList[matchDomain] = (certPem, keyPem);
            }
        }
    }
}
