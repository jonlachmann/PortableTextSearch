using Microsoft.EntityFrameworkCore.Metadata;

namespace PortableTextSearch.Configuration;

internal static class TextSearchMetadataExtensions
{
    private const char Separator = '\u001F';

    public static IReadOnlyList<string> GetTextSearchProperties(this IReadOnlyEntityType entityType)
        => entityType.FindAnnotation(TextSearchAnnotationNames.SearchableProperties)?.Value is string serialized
            ? Deserialize(serialized)
            : [];

    public static void AddTextSearchProperty(this IMutableEntityType entityType, string propertyName)
    {
        var existing = entityType.GetTextSearchProperties();
        if (existing.Contains(propertyName, StringComparer.Ordinal))
        {
            return;
        }

        var updated = existing
            .Concat([propertyName])
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        entityType.SetAnnotation(TextSearchAnnotationNames.SearchableProperties, Serialize(updated));
    }

    private static string Serialize(IReadOnlyList<string> propertyNames)
        => string.Join(Separator, propertyNames);

    private static IReadOnlyList<string> Deserialize(string serialized)
        => string.IsNullOrEmpty(serialized)
            ? []
            : serialized.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
}
