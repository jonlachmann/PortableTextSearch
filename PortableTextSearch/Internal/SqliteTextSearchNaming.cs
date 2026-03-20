namespace PortableTextSearch.Internal;

internal static class SqliteTextSearchNaming
{
    public static string GetDefaultVirtualTableName(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        return $"{tableName}_TextSearch";
    }
}
