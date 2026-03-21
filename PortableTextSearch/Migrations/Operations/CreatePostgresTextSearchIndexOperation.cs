using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace PortableTextSearch.Migrations.Operations;

internal sealed class CreatePostgresTextSearchIndexOperation : MigrationOperation
{
    public required string Table { get; init; }

    public required string Column { get; init; }

    public string? Schema { get; init; }

    public string? IndexName { get; init; }
}
