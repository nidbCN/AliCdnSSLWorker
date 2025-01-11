﻿namespace AliCdnSSLWorker.Models;

public record RemoteCertInfo
{
    public required DateTime CertExpireDate { get; set; }
    public required DomainInfo CertCommonName { get; init; }
}