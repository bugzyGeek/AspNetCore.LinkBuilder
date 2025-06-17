using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using AspNetCore.LinkBuilder.Interfaces;

namespace AspNetCore.LinkBuilder.Filters;

public class HypermediaSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (typeof(IHasLinks).IsAssignableFrom(context.Type))
        {
            schema.Properties.Add("_links", new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema { Type = "object" },
                Description = "Hypermedia links"
            });
        }
    }
}