# AspNetCore.LinkBuilder

`AspNetCore.LinkBuilder` is a lightweight and extensible library that enables **HATEOAS** (Hypermedia as the Engine of Application State) in ASP.NET Core APIs. It helps generate navigable links for resources, with full support for custom link builders, attribute-based configuration, caching, and dynamic client-driven control through `Accept` headers.

---

## 🚀 Installation

Install from NuGet:

```bash
dotnet add package AspNetCore.LinkBuilder
```

---

## 🧩 Features

* ✅ Per-resource HATEOAS support with `ILinkBuilder<T>`
* ✅ Global or per-action HATEOAS policy via `HypermediaAttribute`
* ✅ Optional caching of generated links with pluggable `ILinkCache`
* ✅ Fail-fast diagnostics for missing configurations
* ✅ Swagger-compatible via OpenAPI schema filter (optional)
* ✅ Media-type based toggling using the `Accept` header (`hateoas` keyword)

---

## 🛠️ Setup

### 1. Register services in `Program.cs` or `Startup.cs`

```csharp
builder.Services.AddHateoas();
```

To enable Swagger schema for `_links`:

```csharp
builder.Services.AddListSwaggerSchema();
```

To add HATEOAS globally to all controllers:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<HypermediaAttribute>();
});
```

If you plan to use caching, register an implementation of `ILinkCache`:

```csharp
builder.Services.AddSingleton<ILinkCache, MemoryLinkCache>();
```

---

## 🎛️ Usage

### 1. Create your resource model

```csharp
public class Product : IHasLinks, ICacheIdentifiable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Link> Links { get; set; } = new();

    public string GetCacheKey() => Id.ToString();
}
```

### 2. Implement a link builder

```csharp
public class ProductLinkBuilder : ILinkBuilder<Product>
{
    public List<Link> BuildLinks(Product resource, IUrlHelper urlHelper)
    {
        return new List<Link>
        {
            new(urlHelper.Link("GetProduct", new { id = resource.Id })!, "self", "GET"),
            new(urlHelper.Link("DeleteProduct", new { id = resource.Id })!, "delete", "DELETE")
        };
    }
}
```

Register in DI:

```csharp
builder.Services.AddScoped<ILinkBuilder<Product>, ProductLinkBuilder>();
```

---

### 3. Apply `[Hypermedia]` Attribute

#### Option A: On Controller (Global for all actions)

```csharp
[ApiController]
[Route("api/[controller]")]
[Hypermedia] // Defaults to OnDemand
public class ProductsController : ControllerBase
```

#### Option B: On Specific Action

```csharp
[HttpGet("{id}", Name = "GetProduct")]
[Hypermedia(LinkPolicy.Always, enableCaching: true)]
public IActionResult Get(int id) => Ok(_repo.Find(id));
```

---

## 🧪 Policy Modes

| Policy     | Behavior                                                                              |
| ---------- | ------------------------------------------------------------------------------------- |
| `Always`   | Always generates HATEOAS links, regardless of `Accept` header                         |
| `OnDemand` | (Default) Generates links **only** when `Accept: application/hateoas+json` is present |
| `Never`    | Suppresses HATEOAS link generation, even if requested                                 |

**Example Accept Header for OnDemand mode:**

```
Accept: application/json, application/hateoas+json
```

---

## 🔁 Caching Support

Link caching is **opt-in** via the attribute:

```csharp
[Hypermedia(LinkPolicy.Always, enableCaching: true)]
```

Requirements:

* You **must** implement and register `ILinkCache`.
* Your resource should either:

  * Implement `ICacheIdentifiable` and provide `GetCacheKey()`, **or**
  * Have a public `Id` or `{TypeName}Id` property.

If not, a detailed exception is thrown to guide you.

### Sample `ILinkCache` Implementation

```csharp
public class MemoryLinkCache : ILinkCache
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public bool TryGet<T>(string key, out List<Link> links)
    {
        return _cache.TryGetValue(key, out links!);
    }

    public void Set<T>(string key, List<Link> links, TimeSpan? ttl = null)
    {
        _cache.Set(key, links, ttl ?? TimeSpan.FromMinutes(10));
    }
}
```

---

## 💣 Exceptions & Safety

* If a controller/action returns an object implementing `IHasLinks`, and no corresponding `ILinkBuilder<T>` is registered, an `InvalidOperationException` is thrown.
* If caching is requested but no `ILinkCache` is registered, an exception is also thrown.

These fail-fast validations help prevent silent misconfigurations.

---

## 🧼 Swagger/OpenAPI Support (Optional)

To expose `_links` in Swagger:

1. Add the schema filter:

```csharp
builder.Services.AddListSwaggerSchema();
```

---


## 🔧 Sample Link Class

```csharp
public class Link
{
    public string Href { get; }
    public string Rel { get; }
    public string Method { get; }

    public Link(string href, string rel, string method) =>
        (Href, Rel, Method) = (href, rel, method);
}
```

---

## 📦 Release Notes

### v1.0.0

* Initial release
* HATEOAS filter with policy control
* Caching and key resolution with `ICacheIdentifiable` or ID reflection
* Fallback-safe logging
* OpenAPI schema support (optional)

---

## 📄 License

MIT

---
