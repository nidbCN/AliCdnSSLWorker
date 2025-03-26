# AliCdnSSLWorker

![image](https://github.com/nidbCN/AliCdnSSLWorker/assets/36162655/cb36b8b9-063e-44a8-bd6c-02d312f1e5e9)

这玩意做了什么？

每隔一段时间读取一下你的证书，然后看看阿里云 CDN 上面的证书快过期没，过期的话就把它换了。如果阿里云上面这个域名没开 SSL，但是你列表里面写了，也有证书那就给开了。

## 发行

开发版： `registry.cn-beijing.aliyuncs.com/nidb-cr/alicdn-ssl-worker:git`

稳定版： `registry.cn-beijing.aliyuncs.com/nidb-cr/alicdn-ssl-worker:v8.1.1.0`

## 使用

### 部署

~~教程见：[blog.gaein.cn](https://blog.gaein.cn/passages/auto-deploy-cert-to-alicdn/) ~~

### 配置

```json
  "CertConfig": {
    "CacheTimeoutMin": "30", // 在程序内的缓存时间
    "DomainWhiteList": [ // 域名白名单
      "www.example.com",
      "cdn.example.com",
      "*.test.cn" // 如通配符功能有误还请附上 Debug 的日志提 issue
    ]
  },
```

```json
  "AliCdnConfig": {
    "AccessKeyId": "************************", // 阿里云 Access key
    "AccessKeySecret": "******************************", // 阿里云 Access key secret
    "Endpoint": "cdn.aliyuncs.com"
  }
```

#### Cert Provider

证书内容由 Provider 提供，在配置文件中填写某个 Provider 相关配置即为启用该 Provider。

##### LocalCertProvider

基于本地文件扫描的证书提供者。

```json
  "LocalCertProviderConfig": {
    "CertFileName": "fullchain.pem", // 证书文件名
    "PrivateKeyFileName": "privkey.pem", // 私钥文件名
    "SearchPath": "/mnt/ssl/certs", // 路径
    "RecursionSearch": true
  }
```

##### KubernetesCertProvider

基于 certificates.k8s.io 的证书提供者。

TODO

#### Monitor

触发证书更新的行为由 Monitor 提供，在配置文件中填写某个 Monitor 相关配置即为启用该 Monitor。

##### TimerMonitor

定时更新。

```json
  "TimerMonitorConfig": {
    "Enable": true, // 定时更新是否启用
    "RefreshIntervalMinute": 60 // 检查间隔时间（分钟）
  }
```

##### ForceMonitor

由用户控制的强制更新，请求 `http://<Ip>:<Port>/force_refresh/` 。

```json
  "ForceMonitorConfig": {
    "Enable": true, // API 监听是否启用
    "Ip": "0.0.0.0", // 监听地址
    "Port": 5057 // 监听端口
  }
```

为方便用户发送请求，可以使用命令行参数 `-r` 或 `--refresh` 来向配置文件中指定的 IP 和端口发送更新请求，如：

```bash
sudo docker exec alicdn-ssl dotnet AliCdnSSLWorker.dll -r
```
