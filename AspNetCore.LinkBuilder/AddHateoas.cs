using AspNetCore.LinkBuilder.Filters;
using AspNetCore.LinkBuilder.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.LinkBuilder;

public static class AddHateoasExtensions
{
    /// <summary>
    /// Adds HATEOAS support to the ASP.NET Core application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection for fluent chaining.</returns>
    public static IServiceCollection AddHateoas(this IServiceCollection services)
    {
        // Register the necessary services for HATEOAS
        services.AddScoped<LinkBuilderRegistry>();
        services.AddScoped<HypermediaAttribute>();
        return services;
    }

    /// <summary>
    /// Adds a Swagger schema filter for HATEOAS links.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection for fluent chaining.</returns>
    public static IServiceCollection AddListSwaggerSchema(this IServiceCollection services)
    {
        // Register the schema filter for HATEOAS links
        services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<HypermediaSchemaFilter>();
        });
        return services;
    }
}
