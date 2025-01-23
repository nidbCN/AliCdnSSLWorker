namespace AliCdnSSLWorker.Models;

public class DomainInfo
{
    public required string OriginString { get; init; }

    public IList<ReadOnlyMemory<char>> Parts { get; } = [];

    public bool IsWildcard()
    {
        if (Parts[0].Span.Length != 1)
            return false;

        return Parts[0].Span[0] == '*';
    }

    /// <summary>
    /// 比较两个域名
    /// </summary>
    /// <param name="domain"></param>
    /// <returns>相似的部分数。</returns>
    public int MatchedCount(DomainInfo domain)
    {
        ArgumentNullException.ThrowIfNull(domain, nameof(domain));
        if (Parts.Count == 0 || domain.Parts.Count == 0)
            return 0;

        if (domain.IsWildcard())
        {
            // input domain is wildcard
            if (Parts.Count < domain.Parts.Count)
                return 0;

            for (var i = 1; i < domain.Parts.Count; i++)
            {
                if (domain.Parts[^i].Span.ToString() != Parts[^i].Span.ToString())
                    return 0;
            }

            return domain.Parts.Count - 1;
        }

        if (IsWildcard())
        {
            // this domain is wildcard
            if (domain.Parts.Count < Parts.Count)
                return 0;

            for (var i = 1; i < Parts.Count; i++)
            {
                if (Parts[^i].Span.ToString() != domain.Parts[^i].Span.ToString())
                    return 0;
            }

            return Parts.Count - 1;
        }

        // not wildcard
        return Equals(domain) ? Parts.Count : 0;
    }

    public static DomainInfo Parse(string domain)
    {
        var result = new DomainInfo
        {
            OriginString = domain,
        };
        var mem = domain.AsMemory();

        var i = mem.Span.IndexOf('.');
        if (i == -1)
            throw new ArgumentException("Invalid domain format.", nameof(domain));

        do
        {
            result.Parts.Add(mem[..i]);
            mem = mem[(i + 1)..];
        } while ((i = mem.Span.IndexOf('.')) != -1);

        result.Parts.Add(mem);
        return result;
    }

    public static bool TryParse(string domain, out DomainInfo result)
    {
        result = new()
        {
            OriginString = domain,
        };

        var mem = domain.AsMemory();

        var i = mem.Span.IndexOf('.');
        if (i == -1)
            return false;

        do
        {
            result.Parts.Add(mem[..i]);
            mem = mem[(i + 1)..];
        } while ((i = mem.Span.IndexOf('.')) != -1);

        result.Parts.Add(mem);
        return true;
    }

    public override string ToString()
        => OriginString;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is string domain)
            return OriginString == domain;

        if (obj is not DomainInfo target)
            return false;

        if (target.Parts.Count > Parts.Count || target.Parts.Count != Parts.Count)
            return false;

        return !Parts
            .Where((t, i) => target.Parts[i].Span.ToString() != t.Span.ToString())
            .Any();
    }

    public override int GetHashCode()
        => Parts.GetHashCode();
}
