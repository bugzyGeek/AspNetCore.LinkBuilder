using System.Collections.Concurrent;
using System.Reflection;
using AspNetCore.LinkBuilder.Cache;
using AspNetCore.LinkBuilder.Enums;
using AspNetCore.LinkBuilder.Interfaces;
using AspNetCore.LinkBuilder.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.LinkBuilder.Registry;

public class LinkBuilderRegistry(IServiceProvider services, ILinkCache? cache = null)
{
    private readonly IServiceProvider _services = services;
    private readonly ILinkCache? _cache = cache;

    // Cache for the ID property info per type to avoid repeated reflection.
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdPropertyCache = new();

    public List<Link> GenerateLinks<T>(
        T resource,
        IUrlHelper urlHelper,
        LinkPolicy policy = LinkPolicy.OnDemand,
        bool enableCaching = false)
    {
        if (_services.GetService(typeof(ILinkBuilder<T>)) is not ILinkBuilder<T> builder)
            throw new InvalidOperationException($"No ILinkBuilder<{typeof(T).Name}> is registered.");

        if (policy == LinkPolicy.Never)
            return [];

        // Improved Accept header handling (again, as a safeguard here)
        var acceptHeader = urlHelper.ActionContext.HttpContext.Request.Headers["Accept"].ToString();
        var mediaTypes = acceptHeader.Split(',')
            .Select(mt => mt.Split(';')[0].Trim());
        if (policy == LinkPolicy.OnDemand && !mediaTypes.Any(mt => mt.Contains("hateoas", StringComparison.OrdinalIgnoreCase)))
        {
            return [];
        }

        if (!enableCaching)
            return builder.BuildLinks(resource, urlHelper);

        if (_cache == null)
            throw new InvalidOperationException("Link caching was requested, but no ILinkCache was registered.");

        var key = $"link:{typeof(T).Name}:{GetCacheKey(resource, urlHelper)}";
        if (_cache.TryGet<T>(key, out var cached)) return cached;

        var links = builder.BuildLinks(resource, urlHelper);
        _cache.Set<T>(key, links);
        return links;
    }

    private static string GetCacheKey<T>(T resource, IUrlHelper urlHelper)
    {
        string idPart;

        if (resource is ICacheIdentifiable customKey)
        {
            idPart = customKey.GetCacheKey();
        }
        else
        {
            var type = typeof(T);
            // Cache the property info lookup for "Id" or "{TypeName}Id"
            PropertyInfo? idProp = IdPropertyCache.GetOrAdd(type, t =>
                t.GetProperty("Id") ?? t.GetProperty($"{t.Name}Id")
            ) ?? throw new InvalidOperationException($"Cannot determine cache key for {type.Name}. Add an Id property or implement ICacheIdentifiable.");

            var idValue = idProp.GetValue(resource) ?? throw new InvalidOperationException($"The ID for {type.Name} is null. Ensure it has a valid value.");

            idPart = idValue switch
            {
                string s when !string.IsNullOrWhiteSpace(s) => s,
                Guid g => g.ToString(),
                int i => i.ToString(),
                _ => idValue.ToString() ?? throw new InvalidOperationException($"Cannot convert the ID for {type.Name} to a string.")
            };
        }

        // Include route/controller to enhance uniqueness
        var controller = urlHelper.ActionContext.RouteData.Values["controller"]?.ToString();
        var action = urlHelper.ActionContext.RouteData.Values["action"]?.ToString();
        return $"{controller}:{action}:{typeof(T).Name}:{idPart}";
    }
}
