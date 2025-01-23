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

        // not wildcard
        if (!domain.IsWildcard() || !IsWildcard())
            return Equals(domain) ? Parts.Count : 0;

        // wildcard
        if (Parts.Count == domain.Parts.Count - 1)
        {
            for (var i = domain.Parts.Count - 1; i >= 1; i--)
            {
                if (Parts[i].Span != domain.Parts[i].Span)
                    return 0;
            }

            return Parts.Count;
        }

        if (domain.Parts.Count == Parts.Count - 1)
        {
            for (var i = Parts.Count - 1; i >= 1; i--)
            {
                if (Parts[i].Span != domain.Parts[i].Span)
                    return 0;
            }

            return domain.Parts.Count;
        }

        // not start with '*', not equal
        return 0;
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
            .Where((t, i) => target.Parts[i].Span != t.Span)
            .Any();
    }

    public override int GetHashCode()
        => Parts.GetHashCode();
}
