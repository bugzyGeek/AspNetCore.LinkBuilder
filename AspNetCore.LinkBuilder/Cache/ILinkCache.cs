using AspNetCore.LinkBuilder.Models;

namespace AspNetCore.LinkBuilder.Cache;

public interface ILinkCache
{
    bool TryGet<T>(string key, out List<Link> links);
    void Set<T>(string key, List<Link> links, TimeSpan? ttl = null);
}
