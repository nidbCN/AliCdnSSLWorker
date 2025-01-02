namespace AliCdnSSLWorker.Models;

public class DomainInfo
{
    public required string OriginString { get; init; }

    public IList<ReadOnlyMemory<char>> Parts { get; } = new List<ReadOnlyMemory<char>>();

    public bool Contains(DomainInfo domain)
    {
        if (domain.Parts[0].Span is not "*")
            return Equals(domain);

        // compare all element except '*' in target(sub) domain.
        for (var i = domain.Parts.Count - 1; i >= 1; i--)
        {
            if (Parts[i].Span != domain.Parts[i].Span)
                return false;
        }
        return true;
    }

    public static DomainInfo Parse(string domain)
    {
        var result = new DomainInfo
        {
            OriginString = domain;
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

    public override string ToString()
        => OriginString;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;

        if (obj is not DomainInfo target) return false;

        if (target.Parts.Count > Parts.Count || target.Parts.Count != Parts.Count)
            return false;

        return !Parts
            .Where((t, i) => target.Parts[i].Span != t.Span)
            .Any();
    }

    public override int GetHashCode()
        => Parts.GetHashCode();

    protected bool Equals(DomainInfo other)
        => Parts.Equals(other.Parts);
}
