ASSEMBLY_VERSION=$(sed -n 's:.*<AssemblyVersion>\(.*\)</AssemblyVersion>.*:\1:p' AliCdnSSLWorker.csproj)
docker build -t registry.cn-beijing.aliyuncs.com/nidb-cr/alicdn_ssl_worker:v$ASSEMBLY_VERSION .
