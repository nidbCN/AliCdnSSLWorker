using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AliCdnSSLWorker.Extensions;

public static class X509Certificate2Extension
{
    public static bool TryExportPrivateKeyPem(this X509Certificate2 cert, out string? privateKey)
    {
        privateKey = null;

        if (!cert.HasPrivateKey)
            return false;

        var algorithmInfo = cert.SignatureAlgorithm.Value.AsSpan();

        AsymmetricAlgorithm? algorithm = null;

        if (algorithmInfo.StartsWith("1.2.840.10045.4.1")
            || algorithmInfo.StartsWith("1.2.840.10045.4.2")
            || algorithmInfo.StartsWith("1.2.840.10045.4.3"))
        {
            algorithm = cert.GetECDsaPrivateKey();
        }
        else if (algorithmInfo.StartsWith("1.2.840.10040.4.1"))
        {
            algorithm = cert.GetDSAPrivateKey();
        }
        else if (algorithmInfo.StartsWith("1.2.840.113549.1.1"))
        {
            algorithm = cert.GetRSAPrivateKey();
        }
        else if (algorithmInfo.StartsWith("1.3.132.1.12"))
        {
            algorithm = cert.GetECDiffieHellmanPrivateKey();
        }

        if (algorithm is null)
            return false;

        privateKey = algorithm.ExportPkcs8PrivateKeyPem();
        return true;
    }
}
