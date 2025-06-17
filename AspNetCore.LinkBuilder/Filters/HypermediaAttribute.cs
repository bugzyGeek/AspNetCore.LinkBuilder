using AspNetCore.LinkBuilder.Enums;
using AspNetCore.LinkBuilder.Interfaces;
using AspNetCore.LinkBuilder.Registry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.LinkBuilder.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HypermediaAttribute(LinkPolicy policy = LinkPolicy.OnDemand, bool enableCaching = false) : Attribute, IAsyncResultFilter
    {
        public LinkPolicy Policy { get; } = policy;
        public bool EnableCaching { get; } = enableCaching;

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var acceptHeader = request.Headers["Accept"].ToString();

            // Evaluate OnDemand trigger
            if ((Policy == LinkPolicy.OnDemand && !acceptHeader.Contains("hateoas", StringComparison.OrdinalIgnoreCase)) || Policy == LinkPolicy.Never)
            {
                await next();
                return;
            }

            if (context.Result is ObjectResult result && result.Value is IHasLinks entity)
            {
                var registry = context.HttpContext.RequestServices.GetRequiredService<LinkBuilderRegistry>();
                var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
                var urlHelper = urlHelperFactory.GetUrlHelper(context);

                entity.Links = registry.GenerateLinks(entity, urlHelper, Policy, EnableCaching);
            }

            await next();
        }
    }


}
