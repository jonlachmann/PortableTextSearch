using Microsoft.EntityFrameworkCore.Metadata;

namespace PortableTextSearch.Configuration;

internal static class TextSearchMetadataExtensions
{
    public static IReadOnlyList<string> GetTextSearchProperties(this IReadOnlyEntityType entityType)
        => entityType.FindAnnotation(TextSearchAnnotationNames.SearchableProperties)?.Value as string[] ?? [];

    public static void AddTextSearchProperty(this IMutableEntityType entityType, string propertyName)
    {
        var existing = entityType.GetTextSearchProperties();
        if (existing.Contains(propertyName, StringComparer.Ordinal))
        {
            return;
        }

        var updated = existing.Concat([propertyName]).ToArray();
        entityType.SetAnnotation(TextSearchAnnotationNames.SearchableProperties, updated);
    }
}
