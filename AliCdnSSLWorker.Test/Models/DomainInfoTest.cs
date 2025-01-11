using AliCdnSSLWorker.Models;

namespace AliCdnSSLWorker.Test.Models;

public class DomainInfoTest
{
    [Theory]
    [InlineData("*.example.com", true)]
    [InlineData("www.example.com", false)]
    public void IsWildcardTest(string domain, bool expected) =>
        Assert.Equal(expected, DomainInfo.Parse(domain).IsWildcard());

    [Theory]
    [InlineData("*.example.com", "www.example.com", 2)]
    [InlineData("*.example.com", "a.b.example.com", 2)]
    [InlineData("*.audio.cdn.example.com", "live.cn.bjs.audio.cdn.example.com", 4)]
    [InlineData("*.bjs.audio.cdn.example.com", "live.cn.bjs.audio.cdn.example.com", 5)]
    [InlineData("audio.cdn.example.com", "bjs.audio.cdn.example.com", 0)]
    [InlineData("*.audio.cdn.example.com", "video.cdn.example.com", 0)]
    [InlineData("www.example.com", "*.example.com", 2)]
    [InlineData("a.b.example.com", "*.example.com", 2)]
    [InlineData("live.cn.bjs.audio.cdn.example.com", "*.audio.cdn.example.com", 4)]
    [InlineData("live.cn.bjs.audio.cdn.example.com", "*.bjs.audio.cdn.example.com", 5)]
    [InlineData("bjs.audio.cdn.example.com", "audio.cdn.example.com", 0)]
    [InlineData("video.cdn.example.com", "*.audio.cdn.example.com", 0)]
    public void MatchedCountTest(string self, string other, int expected) =>
        Assert.Equal(expected, DomainInfo.Parse(self).MatchedCount(DomainInfo.Parse(other)));
}
