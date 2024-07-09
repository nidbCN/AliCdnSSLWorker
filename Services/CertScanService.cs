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

    private readonly Dictionary<string, (string, string)> _certList = [];

    public CertScanService(ILogger<CertScanService> logger, IOptions<CertConfig> options)
    {
        _logger = logger;

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _domainList = _options.DomainList;

        ScanCertAsync().GetAwaiter().GetResult();
    }

    public (string, string)? GetCertByDomain(string domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        var time = DateTime.Now - _lastScan;
        if (time > TimeSpan.FromMinutes(_options.CacheTimeoutMin))
        {
            ScanCertAsync().Wait();
            _lastScan = DateTime.Now;
        }

        _certList.TryGetValue(domain, out var certPair);
        return certPair;
    }

    private async Task ScanCertAsync()
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
            var certFile = new FileInfo(Path.Combine(subDir.FullName, "cert.pem"));
            var privateKeyFile = new FileInfo(Path.Combine(subDir.FullName, "privkey.pem"));

            if (!certFile.Exists || !privateKeyFile.Exists)
            {
                _logger.LogWarning("Can not found cert or private key file, skip {d}", subDir.Name);
                continue;
            }

            await using var fs = certFile.OpenRead();
            using var reader = new StreamReader(fs);
            var line = await reader.ReadLineAsync();

            const string CERT_BEGIN = "-----BEGIN CERTIFICATE-----";
            const string CERT_END = "-----END CERTIFICATE-----";

            if (line is null || !line.Contains("-----BEGIN CERTIFICATE-----"))
            {
                _logger.LogWarning("Can not found BEGIN CERT flag, skip.");
                continue;
            }

            // 是证书
            var stringBuilder = new StringBuilder((int)certFile.Length);

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("-----END CERTIFICATE-----"))
                    break;

                stringBuilder.Append(line);
            }

            var certContent = stringBuilder.ToString();
            var cert = new X509Certificate2(Convert.FromBase64String(certContent));

            var commonName = cert.GetNameInfo(X509NameType.SimpleName, false);

            if (!_domainList.Contains(commonName))
            {
                // 不监听此域名
                _logger.LogInformation("Domain {d} is not in list, skip.", commonName);
                continue;
            }

            _logger.LogInformation("Scan cert for domain {d}", commonName);

            using var keyReader = new StreamReader(privateKeyFile.OpenRead());
            var keyPem = await keyReader.ReadToEndAsync();

            var certPem = stringBuilder.Insert(0, CERT_BEGIN + "\n").Append("\n" + CERT_END).ToString();

            _logger.LogInformation("Success load cert, cert {c}, private {p}", certPem, keyPem[..24]);

            _certList[commonName] = (certPem, keyPem);
        }
    }
}
