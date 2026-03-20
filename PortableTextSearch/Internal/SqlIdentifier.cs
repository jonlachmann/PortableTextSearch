namespace PortableTextSearch.Internal;

internal static class SqlIdentifier
{
    public static string Quote(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
