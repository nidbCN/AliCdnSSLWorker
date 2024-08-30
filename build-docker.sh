ASSEMBLY_VERSION=$(sed -n 's:.*<AssemblyVersion>\(.*\)</AssemblyVersion>.*:\1:p' AliCdnSSLWorker.csproj)
docker build -t gaein/alicdn-ssl-worker:v$ASSEMBLY_VERSION .
