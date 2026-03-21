using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace PortableTextSearch.Migrations.Operations;

internal sealed class DropPostgresTextSearchIndexOperation : MigrationOperation
{
    public required string Table { get; init; }

    public required string Column { get; init; }

    public string? Schema { get; init; }
}
