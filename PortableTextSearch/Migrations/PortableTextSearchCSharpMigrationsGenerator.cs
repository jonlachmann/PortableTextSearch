using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PortableTextSearch.Migrations.Operations;

namespace PortableTextSearch.Migrations;

internal sealed class PortableTextSearchCSharpMigrationsGenerator(
    MigrationsCodeGeneratorDependencies dependencies,
    CSharpMigrationsGeneratorDependencies csharpDependencies)
    : CSharpMigrationsGenerator(dependencies, csharpDependencies)
{
    protected override IEnumerable<string> GetNamespaces(IEnumerable<MigrationOperation> operations)
    {
        var namespaces = base.GetNamespaces(operations).ToHashSet(StringComparer.Ordinal);

        if (operations.Any(IsPortableTextSearchOperation))
        {
            namespaces.Add("PortableTextSearch.Migrations");
        }

        return namespaces;
    }

    private static bool IsPortableTextSearchOperation(MigrationOperation operation)
        => operation is EnsurePostgresTrigramExtensionOperation
           or CreatePostgresTextSearchIndexOperation
           or DropPostgresTextSearchIndexOperation
           or CreateSqliteTextSearchIndexOperation
           or DropSqliteTextSearchIndexOperation;
}
