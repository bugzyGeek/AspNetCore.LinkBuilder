using System.Collections.Concurrent;
using System.Reflection;
using AspNetCore.LinkBuilder.Cache;
using AspNetCore.LinkBuilder.Enums;
using AspNetCore.LinkBuilder.Interfaces;
using AspNetCore.LinkBuilder.Models;
using AspNetCore.LinkBuilder.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspNetCore.LinkBuilder.Registry;

public class LinkBuilderRegistry(IServiceProvider services, ILinkCache? cache = null, ILogger<LinkBuilderRegistry>? logger = null)
{
    private readonly IServiceProvider _services = services;
    private readonly ILinkCache? _cache = cache;
    private readonly ILogger<LinkBuilderRegistry> _logger = logger ?? NullLogger<LinkBuilderRegistry>.Instance;

    // Cache for the ID property info per type to avoid repeated reflection.
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdPropertyCache = new();

    public List<Link> GenerateLinks<T>(
        T resource,
        IUrlHelper urlHelper,
        LinkPolicy policy = LinkPolicy.OnDemand,
        bool enableCaching = false)
    {
        _logger.LogDebug("Generating links for type {Type}", typeof(T).Name);

        if (_services.GetService(typeof(ILinkBuilder<T>)) is not ILinkBuilder<T> builder)
        {
            _logger.LogWarning("No ILinkBuilder<{Type}> is registered.", typeof(T).Name);
            throw new InvalidOperationException($"No ILinkBuilder<{typeof(T).Name}> is registered.");
        }

        if (policy == LinkPolicy.Never)
            return [];

        // Improved Accept header handling (again, as a safeguard here)
        string acceptHeader = urlHelper.ActionContext.HttpContext.Request.Headers.Accept.ToString();
        bool isHateoasRequested = MediaTypeHelper.AcceptsHateoas(acceptHeader);
        if ((policy == LinkPolicy.OnDemand && !isHateoasRequested) || LinkPolicy.Always == policy)
        {
            return [];
        }

        if (!enableCaching)
            return builder.BuildLinks(resource, urlHelper);

        if (_cache == null)
            throw new InvalidOperationException("Link caching was requested, but no ILinkCache was registered.");

        var key = $"link:{typeof(T).Name}:{GetCacheKey(resource, urlHelper)}";
        _logger.LogDebug("Cache key generated: {Key}", key);
        if (_cache.TryGet<T>(key, out var cached))
        {
            _logger.LogInformation("Returning cached links for key: {Key}", key);
            return cached;
        }

        List<Link> links = builder.BuildLinks(resource, urlHelper);
        _logger.LogInformation("Storing links in cache for key: {Key}", key);
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
            Type type = typeof(T);
            // Cache the property info lookup for "Id" or "{TypeName}Id"
            PropertyInfo? idProp = IdPropertyCache.GetOrAdd(type, t =>
                t.GetProperty("Id") ?? t.GetProperty($"{t.Name}Id")
            ) ?? throw new InvalidOperationException($"Cannot determine cache key for {type.Name}. Add an Id property or implement ICacheIdentifiable.");

            object idValue = idProp.GetValue(resource) ?? throw new InvalidOperationException($"The ID for {type.Name} is null. Ensure it has a valid value.");

            idPart = idValue switch
            {
                string s when !string.IsNullOrWhiteSpace(s) => s,
                Guid g => g.ToString(),
                int i => i.ToString(),
                _ => idValue.ToString() ?? throw new InvalidOperationException($"Cannot convert the ID for {type.Name} to a string.")
            };
        }

        // Include route/controller to enhance uniqueness
        string? controller = urlHelper.ActionContext.RouteData.Values["controller"]?.ToString();
        string? action = urlHelper.ActionContext.RouteData.Values["action"]?.ToString();
        return $"{controller}:{action}:{typeof(T).Name}:{idPart}";
    }
}
