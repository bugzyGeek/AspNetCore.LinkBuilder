namespace AspNetCore.LinkBuilder.Cache;

public interface ICacheIdentifiable
{
    string GetCacheKey();
}
