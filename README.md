# AliCdnSSLWorker

![image](https://github.com/nidbCN/AliCdnSSLWorker/assets/36162655/cb36b8b9-063e-44a8-bd6c-02d312f1e5e9)

这玩意做了什么？

每隔一段时间读取一下你的证书，然后看看阿里云 CDN 上面的证书快过期没，过期的话就把它换了。如果阿里云上面这个域名没开 SSL，但是你列表里面写了，也有证书那就给开了。

## 发行

开发版： `registry.cn-beijing.aliyuncs.com/nidb-cr/alicdn-ssl-worker:git`

稳定版： `registry.cn-beijing.aliyuncs.com/nidb-cr/alicdn-ssl-worker:v8.1.1.0`

## 使用

### 部署

教程见：[blog.gaein.cn](https://blog.gaein.cn/passages/auto-deploy-cert-to-alicdn/)

### 配置

```json
"CertConfig": {
    "CacheTimeoutMin": 30,
    "DomainWhiteList": [],
    "DomainBlackList": []
}
```

```json
"AliCdnConfig": {
    "AccessKeyId": "",
    "AccessKeySecret": "",
    "Endpoint": ""
}
```

### WebAPI

为了方便控制，该项目启动了一个 HTTP 服务器并暴露了 WebAPI

#### 强制上传所有证书

```
HTTP GET <IpAddress>:5057/force_refresh/
```

或使用命令行参数 `--refresh` 或 `-r` 启动二进制发送更新指令，如：

```bash
sudo docker exec alicdn-ssl dotnet AliCdnSSLWorker.dll -r
```
