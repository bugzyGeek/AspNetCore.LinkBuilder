namespace AspNetCore.LinkBuilder.Models;

public class Link
{
    public string Href { get; }
    public string Rel { get; }
    public string Method { get; }

    public Link(string href, string rel, string method)
        => (Href, Rel, Method) = (href, rel, method);
}
