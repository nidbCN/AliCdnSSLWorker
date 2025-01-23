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

    /// <summary>
    /// Get matched cert by domain and provider
    /// </summary>
    /// <param name="domain"></param>
    /// <param name="providerName"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<CertInfo?> GetMatchedCertByDomainFromProvider(DomainInfo domain, string providerName, CancellationToken token)
            => (await GetAllCerts(token)).OrderBy(c => c.CertCommonName.MatchedCount(domain))
             .FirstOrDefault(c => c.Provider.GetName() == providerName);

    /// <summary>
    /// Get matched cert by file identify
    /// </summary>
    /// <param name="path">file full name</param>
    /// <param name="token"></param>
    /// <returns>Found <see cref="DomainInfo"/> or <see langword="null"/></returns>
    public Task<CertInfo?> GetMatchedCertByDomain(string path, CancellationToken token)
    {
        try
        {
            var cert = X509Certificate2.CreateFromPemFile(Path.Combine(path, options.Value.CertFileName), Path.Combine(path, options.Value.PrivateKeyFileName));
            var certName = cert.GetNameInfo(X509NameType.SimpleName, false);
            var certContent = cert.ExportCertificatePem();

            // export success
            if (cert.TryExportPrivateKeyPem(out var keyContent))
                return Task.FromResult<CertInfo?>(new()
                {
                    CertCommonName = DomainInfo.Parse(certName),
                    CertExpireDate = cert.NotAfter,
                    FullChain = certContent,
                    PrivateKey = keyContent!,
                    IdentityName = path,
                    Provider = this
                });

            // export failed
            logger.LogWarning("UnSupport signature algorithm `{oid}`", cert.SignatureAlgorithm);
            return Task.FromResult<CertInfo?>(null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Load cert error.");
        }

        return Task.FromResult<CertInfo?>(null);
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
            await Parallel.ForEachAsync(dirList, token, async (subDir, innerToken) =>
                await GetAllCertsCore(subDir, list.Add, innerToken));
        }
        else
        {
            await GetAllCertsCore(dir, list.Add, token);
        }

        return list;
    }

    private async ValueTask GetAllCertsCore(DirectoryInfo dir, Action<CertInfo> invoked, CancellationToken token)
    {
        var certFile = dir.GetFiles(options.Value.CertFileName);
        var privateFile = dir.GetFiles(options.Value.PrivateKeyFileName);

        if (certFile.Length != 1 || privateFile.Length != 1)
        {
            logger.LogWarning("Can not found cert or private key file, skip {d}", dir.Name);
            return;
        }

        var cert = await GetMatchedCertByDomain(dir.FullName, token);

        if (cert is null)
        {
            logger.LogWarning("Could not load cert in path `{path}`, skip.", dir.FullName);
            return;
        }

        invoked(cert);
    }

    public string GetName() => nameof(LocalCertProvider);
}
