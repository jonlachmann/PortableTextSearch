namespace PortableTextSearch.Internal;

internal static class SqlStringLiteral
{
    public static string Quote(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }
}
