using Microsoft.EntityFrameworkCore.Migrations;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Migrations;

/// <summary>
/// SQLite migration helpers for portable text search.
/// </summary>
public static partial class MigrationBuilderExtensions
{
    /// <summary>
    /// Emits SQL to create an SQLite FTS5 virtual table and synchronization triggers for a base table.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="table">The base table name.</param>
    /// <param name="columns">The text columns included in the FTS index.</param>
    /// <param name="virtualTableName">An optional FTS5 virtual table name override.</param>
    /// <param name="contentRowIdColumn">The primary-key column mapped to the FTS <c>rowid</c>.</param>
    /// <returns>The same migration builder for chaining.</returns>
    public static MigrationBuilder CreateSqliteTextSearchIndex(
        this MigrationBuilder migrationBuilder,
        string table,
        IReadOnlyList<string> columns,
        string? virtualTableName = null,
        string contentRowIdColumn = "Id")
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        var definition = SqliteTextSearchMigrationDefinition.Create(table, columns, virtualTableName, contentRowIdColumn);

        migrationBuilder.Sql(definition.CreateVirtualTableSql);
        migrationBuilder.Sql(definition.SeedExistingRowsSql);
        migrationBuilder.Sql(definition.InsertTriggerSql);
        migrationBuilder.Sql(definition.UpdateTriggerSql);
        migrationBuilder.Sql(definition.DeleteTriggerSql);

        return migrationBuilder;
    }

    /// <summary>
    /// Emits SQL to drop an SQLite FTS5 virtual table and its synchronization triggers.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder.</param>
    /// <param name="table">The base table name.</param>
    /// <param name="columns">The text columns previously included in the FTS index.</param>
    /// <param name="virtualTableName">An optional FTS5 virtual table name override.</param>
    /// <param name="contentRowIdColumn">The primary-key column mapped to the FTS <c>rowid</c>.</param>
    /// <returns>The same migration builder for chaining.</returns>
    public static MigrationBuilder DropSqliteTextSearchIndex(
        this MigrationBuilder migrationBuilder,
        string table,
        IReadOnlyList<string> columns,
        string? virtualTableName = null,
        string contentRowIdColumn = "Id")
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        var definition = SqliteTextSearchMigrationDefinition.Create(table, columns, virtualTableName, contentRowIdColumn);

        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {SqlIdentifier.Quote(definition.InsertTriggerName)};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {SqlIdentifier.Quote(definition.UpdateTriggerName)};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {SqlIdentifier.Quote(definition.DeleteTriggerName)};");
        migrationBuilder.Sql($"DROP TABLE IF EXISTS {SqlIdentifier.Quote(definition.VirtualTableName)};");

        return migrationBuilder;
    }
}
