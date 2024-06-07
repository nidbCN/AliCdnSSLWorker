# AliCdnSSLWorker

![image](https://github.com/nidbCN/AliCdnSSLWorker/assets/36162655/cb36b8b9-063e-44a8-bd6c-02d312f1e5e9)


这玩意做了什么？
每隔一段时间读取一下你的证书，然后看看阿里云CDN上面的证书快过期没，过期的话就把它换了。如果阿里云上面这个域名没开SSL，但是你列表里面写了，也有证书那就给开了。

## 安装

配置文件如下图所示：

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

如果你希望使用环境变量来传递配置，请直接按照配置文件中进行设置，在表示嵌套的对象时候使用 `__`，比如使用 `CERTCONFIG__CERTSERCHPATH` 来配置证书目录。

> 注意：环境变量的优先级低于配置文件，如果在配置文件中设置了证书目录则会覆盖掉你在环境变量中的设置。

## 使用

### docker & docker compose 部署

在项目源代码目录使用指令打包 docker 镜像：

```bash
docker build -t gaein/alicdn-ssl-worker:<your_version> .
```

如果出现类似提示，请在命令前加上 `sudo`

```bash
permission denied while trying to connect to the Docker daemon socket at unix:///var/run/docker.sock: Get "http://%2Fvar%2Frun%2Fdocker.sock/v1.45/containers/json": dial unix /var/run/docker.sock: connect: permission denied
```

打包完成后使用 `docker images` 查看是否存在镜像

新建目录，使用 `docker-compose` 来部署，示例的文件如下：

```yaml
volumes:
  ssl_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: './nginx/letsencrypt'

configs:
  alicdn-ssl.conf:
    file: './alicdn-ssl/appsettings.json'

services:
  alicdn-ssl:
    image: 'gaein/alicdn-ssl-worker:v1.4'
    container_name: 'alicdn-ssl'
    environment:
      TZ: 'Asia/Shanghai'
    ports:
      - 5057:5057
    depends_on:
      - nginx
    restart: unless-stopped
    volumes:
      - ssl_data:/data/ssl
    configs:
      - source: alicdn-ssl.conf
        target: '/app/appsettings.json'
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
