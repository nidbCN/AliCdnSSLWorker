namespace AliCdnSSLWorker.Services;

public class CertHandlerService
{
    private uint _handler = 0;
    public uint Acquire() => _handler++;
}
