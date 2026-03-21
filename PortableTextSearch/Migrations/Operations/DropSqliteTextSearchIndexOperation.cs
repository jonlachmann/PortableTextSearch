using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace PortableTextSearch.Migrations.Operations;

internal sealed class DropSqliteTextSearchIndexOperation : MigrationOperation
{
    public required string Table { get; init; }

    public required IReadOnlyList<string> Columns { get; init; }

    public required string ContentKeyColumn { get; init; }

    public string? VirtualTableName { get; init; }
}
