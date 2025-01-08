using AlibabaCloud.SDK.Cdn20180510.Models;
using AlibabaCloud.TeaUtil.Models;
using AliCdnSSLWorker.Configs;
using AliCdnSSLWorker.Models;
using Aliyun.Credentials.Utils;
using Microsoft.Extensions.Options;
using Tea;
using ApiClient = AlibabaCloud.SDK.Cdn20180510.Client;
using CredClient = Aliyun.Credentials.Client;

namespace AliCdnSSLWorker.Services;

public class AliCdnService
{
    private readonly ILogger<AliCdnService> _logger;
    private readonly ApiClient _apiClient;
    private readonly RuntimeOptions _apiRuntimeOptions = new();

    public AliCdnService(ILogger<AliCdnService> logger, IOptions<AliCdnConfig> options)
    {
        var credentialClient = new CredClient(
            new()
            {
                AccessKeyId = options.Value.AccessKeyId,
                AccessKeySecret = options.Value.AccessKeySecret,
                Type = AuthConstant.AccessKey,
            });

        _apiClient = new(new()
        {
            Endpoint = options.Value.Endpoint,
            Credential = credentialClient,
        });

        _logger = logger;
    }

    public bool TryGetRemoteCerts(out IEnumerable<CertInfo> infos)
    {
        var req = new DescribeCdnHttpsDomainListRequest();

        try
        {
            var resp = _apiClient.DescribeCdnHttpsDomainListWithOptions(req, _apiRuntimeOptions);
            
            // Request success
            if (resp.StatusCode == 200)
            {
                infos = resp.Body.CertInfos.CertInfo.Select(c => new CertInfo
                {
                    CertExpireDate = DateTime.Parse(c.CertExpireTime),
                    CertCommonName = DomainInfo.Parse(c.CertCommonName),
                    IdentityName = c.CertName,
                    FullChain = null,
                    PrivateKey = null
                });

                return true;
            }

            // Request failed
            _logger.LogWarning("Api return status {status}, request failed.", resp.StatusCode);
            infos = [];
            return false;
        }
        catch (TeaException error)
        {
            _logger.LogError(error, "Describe cdn domain list failed with msg: {m}, {rec}", error.Message, error.Data["Recommend"]);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured during describe cdn domain list, msg: {m}", e.Message);
        }

        infos = Array.Empty<CertInfo>();
        return false;
    }

    public bool TryUploadCert(string domain, CertInfo certInfo)
    {
        var req = new SetCdnDomainSSLCertificateRequest
        {
            DomainName = domain,
            CertName = domain + DateTime.Now.ToShortDateString(),
            CertType = "upload",
            SSLProtocol = "on",
            SSLPub = certInfo.FullChain,
            SSLPri = certInfo.PrivateKey,
        };

        _logger.LogInformation("Upload cert, cert `{c}`.", certInfo.CertCommonName);

        try
        {
            // 复制代码运行请自行打印 API 的返回值
            _apiClient.SetCdnDomainSSLCertificateWithOptions(req, _apiRuntimeOptions);
            return true;
        }
        catch (TeaException error)
        {
            _logger.LogError(error, "Upload cert failed with msg: {m}, {r}", error.Message, error.Data["Recommend"]);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured during upload cert, msg: {m}", e.Message);
        }

        return false;
    }
}
