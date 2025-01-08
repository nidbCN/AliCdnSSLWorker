﻿namespace AliCdnSSLWorker.Configs;

public record CertConfig
{
    public uint CacheTimeoutMin { get; init; } = 30;

    public HashSet<string> DomainList { get; init; } = [];
}
