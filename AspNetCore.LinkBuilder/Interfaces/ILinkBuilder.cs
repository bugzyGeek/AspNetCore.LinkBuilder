using AspNetCore.LinkBuilder.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.LinkBuilder.Interfaces
{
    public interface ILinkBuilder<in T>
    {
        List<Link> BuildLinks(T resource, IUrlHelper urlHelper);
    }

}
