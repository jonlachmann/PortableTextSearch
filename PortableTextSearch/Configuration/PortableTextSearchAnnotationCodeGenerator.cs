using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace PortableTextSearch.Configuration;

internal sealed class PortableTextSearchAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies)
    : AnnotationCodeGenerator(dependencies)
{
    public override IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IEntityType entityType,
        IDictionary<string, IAnnotation> annotations)
    {
        var fragments = new List<MethodCallCodeFragment>(base.GenerateFluentApiCalls(entityType, annotations));

        if (!annotations.TryGetValue(TextSearchAnnotationNames.SearchableProperties, out var annotation))
        {
            return fragments;
        }

        annotations.Remove(TextSearchAnnotationNames.SearchableProperties);

        if (annotation.Value is string serializedProperties && !string.IsNullOrWhiteSpace(serializedProperties))
        {
            fragments.Add(new MethodCallCodeFragment(
                "HasAnnotation",
                TextSearchAnnotationNames.SearchableProperties,
                serializedProperties));
        }

        return fragments;
    }
}
