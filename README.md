# AliCdnSSLWorker

这玩意做了什么？
每隔一段时间读取一下你的证书，然后看看阿里云CDN上面的证书快过期没，过期的话就把它换了。如果阿里云上面这个域名没开SSL，但是你列表里面写了，也有证书那就给开了。

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",   // 日志等级
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "CertConfig": {
    "CertSerchPath": "/opt/letsencrypt/live", // 证书目录
    "IntervalHour": "12", // 检测时间
    "CacheTimeoutMin": "30", // 内部缓存过期时间
    "DomainList": [ // 域名列表
      "cdn1.gaein.cn",
      "cdn2.gaein.cn"
      // ...
    ]
  },
  "ApiConfig": {
    "AccessKeyId": "LT**********************", // 阿里云 AK
    "AccessKeySecret": "6L***********************", // 阿里云 AK
    "Endpoint": "cdn.aliyuncs.com" // 参考https://api.aliyun.com/product/Cdn
  }
}
```