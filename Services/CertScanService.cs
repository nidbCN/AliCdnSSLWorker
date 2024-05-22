using AliCdnSSLWorker.Configs;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AliCdnSSLWorker.Services;
internal class CertScanService
{
    private readonly ILogger<CertScanService> _logger;
    private readonly IOptions<ApiConfig> _options;

    public CertScanService(ILogger<CertScanService> logger, IOptions<ApiConfig> options)
    {
        _logger = logger;
        _options = options;
    }
}
