using AspNetCore.LinkBuilder.Filters;
using AspNetCore.LinkBuilder.Registry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.LinkBuilder;

public static class AddHateoasExtention
{
    /// <summary>  
    /// Adds HATEOAS support to the ASP.NET Core application.  
    /// </summary>  
    /// <param name="builder">The application builder.</param>  
    /// <returns>The updated application builder.</returns>  
    public static IServiceCollection AddHateoas(this IServiceCollection services)
    {
        // Register the necessary services for HATEOAS  
        services.AddScoped<LinkBuilderRegistry>();
        services.AddScoped<HypermediaAttribute>();
        return services;
    }
}
