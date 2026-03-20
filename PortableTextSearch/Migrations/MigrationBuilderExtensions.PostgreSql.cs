using Microsoft.EntityFrameworkCore.Migrations;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Migrations;

/// <summary>
/// PostgreSQL migration helpers for portable text search.
/// </summary>
public static partial class MigrationBuilderExtensions
{
    /// <summary>
    /// Emits SQL to enable the PostgreSQL <c>pg_trgm</c> extension if it is not already installed.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <returns>The same migration builder for chaining.</returns>
    public static MigrationBuilder EnsurePostgresTrigramExtension(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        return migrationBuilder;
    }

    /// <summary>
    /// Emits SQL to create a GIN trigram index for a text column.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="table">The table name.</param>
    /// <param name="column">The text column name.</param>
    /// <param name="schema">An optional schema.</param>
    /// <param name="indexName">An optional index name override.</param>
    /// <returns>The same migration builder for chaining.</returns>
    public static MigrationBuilder CreatePostgresTextSearchIndex(
        this MigrationBuilder migrationBuilder,
        string table,
        string column,
        string? schema = null,
        string? indexName = null)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(column);

        var resolvedIndexName = indexName ?? $"IX_{table}_{column}_TextSearch";
        var qualifiedTable = schema is null
            ? SqlIdentifier.Quote(table)
            : $"{SqlIdentifier.Quote(schema)}.{SqlIdentifier.Quote(table)}";

        var sql =
            $"CREATE INDEX {SqlIdentifier.Quote(resolvedIndexName)} ON {qualifiedTable} USING GIN ({SqlIdentifier.Quote(column)} gin_trgm_ops);";

        migrationBuilder.Sql(sql);
        return migrationBuilder;
    }
}
