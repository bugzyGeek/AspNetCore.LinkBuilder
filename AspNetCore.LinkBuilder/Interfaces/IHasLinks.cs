using AspNetCore.LinkBuilder.Models;

namespace AspNetCore.LinkBuilder.Interfaces;

public interface IHasLinks
{
    List<Link> Links { get; set; }
}
