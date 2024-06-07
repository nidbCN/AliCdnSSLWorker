using Microsoft.Extensions.Options;
using AlibabaCloud.SDK.Cdn20180510.Models;
using AlibabaCloud.TeaUtil.Models;
using AliCdnSSLWorker.Configs;
using Tea;
using AliCdnSSLWorker.Models;

namespace AliCdnSSLWorker.Services;

public class AliCdnService
{
    private readonly ILogger<AliCdnService> _logger;
    private readonly AlibabaCloud.SDK.Cdn20180510.Client _apiClient;
    private readonly Aliyun.Credentials.Client _credentialClient;
    private readonly RuntimeOptions _runtimeOptions = new();

    public AliCdnService(ILogger<AliCdnService> logger, IOptions<AliCdnConfig> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _credentialClient = new(new()
        {
            AccessKeyId = options.Value.AccessKeyId,
            AccessKeySecret = options.Value.AccessKeySecret,
            Type = "access_key",
        });

        _apiClient = new(new()
        {
            Endpoint = options.Value.Endpoint,
            Credential = _credentialClient,
        });

        _logger = logger;
    }

    public bool TryGetHttpsCerts(out IEnumerable<DomainCertInfo> infos)
    {
        var req = new DescribeCdnHttpsDomainListRequest();

        try
        {
            var resp = _apiClient.DescribeCdnHttpsDomainListWithOptions(req, _runtimeOptions);
            if (resp.StatusCode == 200)
            {
                infos = resp.Body.CertInfos.CertInfo.Select(c => new DomainCertInfo()
                {
                    Name = c.DomainName,
                    CertExpireTime = DateTime.Parse(c.CertExpireTime),
                    CertCommonName = c.CertCommonName,
                });

                return true;
            }
        }
        catch (TeaException error)
        {
            _logger.LogError("error msg: {m}", error.Message);
            // 诊断地址
            _logger.LogError("rec: {r}", error.Data["Recommend"]);
            AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
        }
        catch (Exception _error)
        {
            TeaException error = new TeaException(new Dictionary<string, object>
                {
                    { "message", _error.Message }
                });
            // 此处仅做打印展示，请谨慎对待异常处理，在工程项目中切勿直接忽略异常。
            // 错误 message
            Console.WriteLine(error.Message);
            // 诊断地址
            Console.WriteLine(error.Data["Recommend"]);
            AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
        }

        infos = Array.Empty<DomainCertInfo>();
        return false;
    }

    public bool TryUploadCert(string domainName, (string, string) certPair)
    {
        var (cert, privateKey) = certPair;

        var req = new SetCdnDomainSSLCertificateRequest
        {
            DomainName = domainName,
            CertType = "upload",
            SSLProtocol = "on",
            SSLPub = cert,
            SSLPri = privateKey,
        };

        try
        {
            // 复制代码运行请自行打印 API 的返回值
            _apiClient.SetCdnDomainSSLCertificateWithOptions(req, _runtimeOptions);
            return true;
        }
        catch (TeaException error)
        {
            // 此处仅做打印展示，请谨慎对待异常处理，在工程项目中切勿直接忽略异常。
            // 错误 message
            Console.WriteLine(error.Message);
            // 诊断地址
            Console.WriteLine(error.Data["Recommend"]);
            AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
        }
        catch (Exception _error)
        {
            TeaException error = new TeaException(new Dictionary<string, object>
                {
                    { "message", _error.Message }
                });
            // 此处仅做打印展示，请谨慎对待异常处理，在工程项目中切勿直接忽略异常。
            // 错误 message
            Console.WriteLine(error.Message);
            // 诊断地址
            Console.WriteLine(error.Data["Recommend"]);
            AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
        }

        return false;
    }
}
