using AspNetCore.LinkBuilder.Enums;
using AspNetCore.LinkBuilder.Interfaces;
using AspNetCore.LinkBuilder.Registry;
using AspNetCore.LinkBuilder.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.LinkBuilder.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class HypermediaAttribute(LinkPolicy policy = LinkPolicy.OnDemand, bool enableCaching = false) : Attribute, IAsyncResultFilter
{
    public LinkPolicy Policy { get; } = policy;
    public bool EnableCaching { get; } = enableCaching;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        FilterDescriptor? currentDescriptor = context.Filters
                                        .OfType<FilterDescriptor>()
                                        .FirstOrDefault(fd => fd.Filter == this);

        if (currentDescriptor == null)
        {
            await next();
            return;
        }

        bool hasMoreSpecific = context.Filters
            .OfType<FilterDescriptor>()
            .Where(fd => fd.Filter is FilterDescriptor && fd.Filter != this)
            .Any(fd => IsMoreSpecific(fd.Scope, currentDescriptor.Scope));

        if (hasMoreSpecific)
        {
            await next();
            return;
        }

        HttpRequest request = context.HttpContext.Request;
        string acceptHeader = request.Headers.Accept.ToString();

        // Improved Accept header handling: split by commas and handle media type parameters.
        bool isHateoasRequested = MediaTypeHelper.AcceptsHateoas(acceptHeader);

        if ((Policy == LinkPolicy.OnDemand && !isHateoasRequested) || Policy == LinkPolicy.Never)
        {
            await next();
            return;
        }

        if (context.Result is ObjectResult result && result.Value is IHasLinks entity)
        {
            LinkBuilderRegistry registry = context.HttpContext.RequestServices.GetRequiredService<LinkBuilderRegistry>();
            IUrlHelperFactory urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(context);

            entity.Links = registry.GenerateLinks(entity, urlHelper, Policy, EnableCaching);
        }

        await next();
    }

    private static bool IsMoreSpecific(int candidate, int current)
    {
        return candidate switch
        {
            var action when action == FilterScope.Action => current != FilterScope.Action,
            var controller when controller == FilterScope.Controller => current == FilterScope.Global,
            _ => false
        };
    }
}
