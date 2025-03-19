using System.Security.Cryptography.X509Certificates;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Extensions;
using AliCdnSSLWorker.Models;
using Microsoft.Extensions.Options;

namespace AliCdnSSLWorker.CertProvider;

public class LocalCertProvider(
    ILogger<LocalCertProvider> logger,
    IOptions<LocalCertProviderConfig> options)
        : ICertProvider
{
    /// <summary>
    /// Get matched cert by domain
    /// </summary>
    /// <param name="domain">domain info</param>
    /// <param name="token"></param>
    /// <returns>domain info or null</returns>
    public async Task<CertInfo?> GetMatchedCertByDomain(DomainInfo domain, CancellationToken token)
        => (await GetAllCerts(token)).OrderBy(c => c.CertCommonName.MatchedCount(domain))
             .FirstOrDefault();

    public CertInfo? GetMatchedCertByDomain(string identify, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get matched cert by domain and provider
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<CertInfo?> GetMatchedCertByDomainFromProvider<TProvider>(DomainInfo domain, CancellationToken token)
            => (await GetAllCerts(token)).OrderBy(c => c.CertCommonName.MatchedCount(domain))
             .FirstOrDefault(c => c.Provider is TProvider);

    /// <summary>
    /// Get matched cert by file identify
    /// </summary>
    /// <param name="path">file full name</param>
    /// <returns>Found <see cref="DomainInfo"/> or <see langword="null"/></returns>
    public CertInfo? GetMatchedCertByDomain(string path)
    {
        try
        {
            var (result, cert, certContent, keyContent) = ReadCertFromPath(path);
            var certName = cert.GetNameInfo(X509NameType.SimpleName, false);

            // export success
            if (result)
                return new()
                {
                    CertCommonName = DomainInfo.Parse(certName),
                    CertExpireDate = cert.NotAfter,
                    FullChain = certContent,
                    PrivateKey = keyContent!,
                    IdentityName = path,
                    Provider = this
                };

            // export failed
            logger.LogWarning("UnSupport signature algorithm `{oid}`", cert.SignatureAlgorithm);
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured while loading cert.");
        }

        return null;
    }

    private (bool, X509Certificate2, string, string?) ReadCertFromPath(string path)
    {
        var cert = X509Certificate2.CreateFromPemFile(Path.Combine(path, options.Value.CertFileName), Path.Combine(path, options.Value.PrivateKeyFileName));
        var certContent = cert.ExportCertificatePem();

        // export success
        return cert.TryExportPrivateKeyPem(out var keyContent)
            ? (true, cert, certContent, keyContent)
            : (false, cert, certContent, keyContent);
    }

    public async Task<IList<CertInfo>> GetAllCerts(CancellationToken token)
    {
        var list = new List<CertInfo>();

        var dir = new DirectoryInfo(options.Value.SearchPath);
        if (!dir.Exists)
        {
            logger.LogError("Dir {d} isn't exists!", dir.FullName);
            return list;
        }

        if (options.Value.RecursionSearch)
        {
            var dirList = dir.GetDirectories();
            Parallel.ForEach(dirList, (subDir, _) =>
            {
                GetAllCertsCore(subDir, list.Add);
            });
        }
        else
        {
            GetAllCertsCore(dir, list.Add);
        }

        return list;
    }

    private void GetAllCertsCore(DirectoryInfo dir, Action<CertInfo> invoked)
    {
        var certFile = dir.GetFiles(options.Value.CertFileName);
        var privateFile = dir.GetFiles(options.Value.PrivateKeyFileName);

        if (certFile.Length != 1 || privateFile.Length != 1)
        {
            logger.LogWarning("Can not found cert or private key file, skip {d}", dir.Name);
            return;
        }

        logger.LogDebug("Found cert file `{cert}` and key file `{key}`.", certFile[0].FullName, privateFile[0].FullName);

        var cert = GetMatchedCertByDomain(dir.FullName);

        if (cert is null)
        {
            logger.LogWarning("Could not load cert in path `{path}`, skip.", dir.FullName);
            return;
        }

        invoked(cert);
    }

    public string GetName() => nameof(LocalCertProvider);
}
