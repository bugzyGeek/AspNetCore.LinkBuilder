namespace AspNetCore.LinkBuilder.Utilities;

public static class MediaTypeHelper
{
    public static bool AcceptsHateoas(string acceptHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptHeader))
            return false;

        return acceptHeader
            .Split(',')
            .Select(mt => mt.Split(';')[0].Trim())
            .Any(mt => mt.Contains("hateoas", StringComparison.OrdinalIgnoreCase));
    }
}

