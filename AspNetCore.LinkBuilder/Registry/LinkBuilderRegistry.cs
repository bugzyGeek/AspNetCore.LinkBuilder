using AspNetCore.LinkBuilder.Cache;
using AspNetCore.LinkBuilder.Enums;
using AspNetCore.LinkBuilder.Interfaces;
using AspNetCore.LinkBuilder.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.LinkBuilder.Registry
{
    public class LinkBuilderRegistry(IServiceProvider services, ILinkCache? cache = null)
    {
        private readonly IServiceProvider _services = services;
        private readonly ILinkCache? _cache = cache;

        public List<Link> GenerateLinks<T>(
            T resource,
            IUrlHelper urlHelper,
            LinkPolicy policy = LinkPolicy.OnDemand,
            bool enableCaching = false)
            where T : class, IHasLinks
        {
            if (_services.GetService(typeof(ILinkBuilder<T>)) is not ILinkBuilder<T> builder)
                throw new InvalidOperationException($"No ILinkBuilder<{typeof(T).Name}> is registered.");

            if (policy == LinkPolicy.Never)
                return [];

            if (policy == LinkPolicy.OnDemand &&
                !urlHelper.ActionContext.HttpContext.Request.Headers["Accept"].ToString()
                    .Contains("hateoas", StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            if (!enableCaching)
                return builder.BuildLinks(resource, urlHelper);

            if (_cache == null)
                throw new InvalidOperationException("Link caching was requested, but no ILinkCache was registered.");

            var key = $"link:{typeof(T).Name}:{GetCacheKey(resource)}";
            if (_cache.TryGet<T>(key, out var cached)) return cached;

            var links = builder.BuildLinks(resource, urlHelper);
            _cache.Set<T>(key, links);
            return links;
        }

        private static string GetCacheKey<T>(T resource)
        {
            if (resource is ICacheIdentifiable customKey)
                return customKey.GetCacheKey();

            var type = typeof(T);
            var idProp = type.GetProperty("Id") ?? type.GetProperty($"{type.Name}Id");

            if (idProp?.GetValue(resource) is string idStr && !string.IsNullOrWhiteSpace(idStr))
                return idStr;

            throw new InvalidOperationException($"Cannot determine cache key for {type.Name}. Add an Id or implement ICacheIdentifiable.");
        }
    }

}
