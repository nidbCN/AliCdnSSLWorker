using AliCdnSSLWorker.Configs;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AliCdnSSLWorker.Services;

public class CertScanService
{
    private readonly ILogger<CertScanService> _logger;
    private readonly CertConfig _options;
    private DateTime _lastScan;
    private readonly HashSet<string> _domainList;

    private readonly IDictionary<string, (string, string)> _certList
        = new Dictionary<string, (string, string)>();

    public CertScanService(ILogger<CertScanService> logger, IOptions<CertConfig> options)
    {
        _logger = logger;

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _domainList = _options.DomainList;

        UpdateCertAsync().Wait();
    }

    public (string, string)? GetCertByDomain(string domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        var time = DateTime.Now - _lastScan;
        if (time > TimeSpan.FromMinutes(_options.CacheTimeoutMin))
        {
            UpdateCertAsync().Wait();
            _lastScan = DateTime.Now;
        }

        _certList.TryGetValue(domain, out var certPair);
        return certPair;
    }

    private async Task UpdateCertAsync()
    {
        var dir = new DirectoryInfo(_options.CertSerchPath);
        if (!dir.Exists)
        {
            _logger.LogError("Dir {d} isn't exists!", dir.FullName);
            return;
        }

        var dirList = dir.GetDirectories();
        foreach (var subDir in dirList)
        {

            var certFile = new FileInfo(Path.Combine(dir.FullName, "cert.pem"));
            var privKeyFile = new FileInfo(Path.Combine(dir.FullName, "privkey.pem"));

            if (!certFile.Exists || !privKeyFile.Exists)
            {
                continue;
            }

            using var fs = certFile.OpenRead();
            using var reader = new StreamReader(fs);
            var line = await reader.ReadLineAsync();
            const string CERT_BEGIN = "-----BEGIN CERTIFICATE-----";
            const string CERT_END = "-----END CERTIFICATE-----";

            if (line is null || !line.Contains("-----BEGIN CERTIFICATE-----"))
            {
                continue;
            }

            // 是证书
            var stringBuilder = new StringBuilder((int)certFile.Length);
            var lines = new LinkedList<string>();

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("-----END CERTIFICATE-----"))
                    break;

                stringBuilder.Append(line);
            }

            var certContent = stringBuilder.ToString();
            var cert = new X509Certificate2(Convert.FromBase64String(certContent));

            var commonName = cert.GetNameInfo(X509NameType.SimpleName, false);

            if (commonName is null || !_domainList.Contains(commonName))
            {
                // 不监听此域名
                continue;
            }

            using var keyReader = new StreamReader(privKeyFile.OpenRead());
            var keyPEM = await keyReader.ReadToEndAsync();

            var certPEM = stringBuilder.Insert(0, CERT_BEGIN).Append(CERT_END).ToString();

            if (_certList.ContainsKey(commonName))
            {
                _certList[commonName] = (certPEM, keyPEM);
            }
            else
            {
                _certList.Add(commonName, (certPEM, keyPEM));
            }
        }
    }
}
