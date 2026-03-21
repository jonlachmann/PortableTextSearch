namespace PortableTextSearch.Internal;

internal sealed class SqliteTextSearchMigrationDefinition
{
    public const string EntityKeyColumnName = "__pts_entity_key";

    private SqliteTextSearchMigrationDefinition(
        string table,
        IReadOnlyList<string> columns,
        string virtualTableName,
        string keyColumnName)
    {
        Table = table;
        Columns = columns;
        VirtualTableName = virtualTableName;
        KeyColumnName = keyColumnName;
        InsertTriggerName = $"trg_{table}_{virtualTableName}_ai";
        UpdateTriggerName = $"trg_{table}_{virtualTableName}_au";
        DeleteTriggerName = $"trg_{table}_{virtualTableName}_ad";
        CreateVirtualTableSql = BuildCreateVirtualTableSql();
        SeedExistingRowsSql = BuildSeedExistingRowsSql();
        InsertTriggerSql = BuildInsertTriggerSql();
        UpdateTriggerSql = BuildUpdateTriggerSql();
        DeleteTriggerSql = BuildDeleteTriggerSql();
    }

    public string Table { get; }

    public IReadOnlyList<string> Columns { get; }

    public string VirtualTableName { get; }

    public string KeyColumnName { get; }

    public string InsertTriggerName { get; }

    public string UpdateTriggerName { get; }

    public string DeleteTriggerName { get; }

    public string CreateVirtualTableSql { get; }

    public string SeedExistingRowsSql { get; }

    public string InsertTriggerSql { get; }

    public string UpdateTriggerSql { get; }

    public string DeleteTriggerSql { get; }

    public static SqliteTextSearchMigrationDefinition Create(
        string table,
        IReadOnlyList<string> columns,
        string? virtualTableName,
        string keyColumnName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumnName);

        var normalizedColumns = columns
            .Select(column =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(column);
                return column;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedColumns.Length == 0)
        {
            throw new ArgumentException("At least one text column must be configured for SQLite FTS5 search.", nameof(columns));
        }

        return new SqliteTextSearchMigrationDefinition(
            table,
            normalizedColumns,
            virtualTableName ?? SqliteTextSearchNaming.GetDefaultVirtualTableName(table),
            keyColumnName);
    }

    private string BuildCreateVirtualTableSql()
    {
        var columnList = string.Join(", ", Columns.Select(SqlIdentifier.Quote).Prepend($"{SqlIdentifier.Quote(EntityKeyColumnName)} UNINDEXED"));
        return
            $"CREATE VIRTUAL TABLE {SqlIdentifier.Quote(VirtualTableName)} USING fts5({columnList});";
    }

    private string BuildSeedExistingRowsSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote).Prepend(SqlIdentifier.Quote(EntityKeyColumnName)));
        var selectedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote).Prepend(SqlIdentifier.Quote(KeyColumnName)));
        return
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} ({quotedColumns}) SELECT {selectedColumns} FROM {SqlIdentifier.Quote(Table)};";
    }

    private string BuildInsertTriggerSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote).Prepend(SqlIdentifier.Quote(EntityKeyColumnName)));
        var insertedValues = string.Join(", ", Columns.Select(column => $"new.{SqlIdentifier.Quote(column)}").Prepend($"new.{SqlIdentifier.Quote(KeyColumnName)}"));

        return
            $"CREATE TRIGGER {SqlIdentifier.Quote(InsertTriggerName)} AFTER INSERT ON {SqlIdentifier.Quote(Table)} BEGIN " +
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} ({quotedColumns}) VALUES ({insertedValues}); " +
            "END;";
    }

    private string BuildUpdateTriggerSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote).Prepend(SqlIdentifier.Quote(EntityKeyColumnName)));
        var deletedValues = string.Join(", ", Columns.Select(column => $"old.{SqlIdentifier.Quote(column)}").Prepend($"old.{SqlIdentifier.Quote(KeyColumnName)}"));
        var insertedValues = string.Join(", ", Columns.Select(column => $"new.{SqlIdentifier.Quote(column)}").Prepend($"new.{SqlIdentifier.Quote(KeyColumnName)}"));

        return
            $"CREATE TRIGGER {SqlIdentifier.Quote(UpdateTriggerName)} AFTER UPDATE ON {SqlIdentifier.Quote(Table)} BEGIN " +
            $"DELETE FROM {SqlIdentifier.Quote(VirtualTableName)} WHERE {SqlIdentifier.Quote(EntityKeyColumnName)} = old.{SqlIdentifier.Quote(KeyColumnName)}; " +
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} ({quotedColumns}) VALUES ({insertedValues}); " +
            "END;";
    }

    private string BuildDeleteTriggerSql()
    {
        return
            $"CREATE TRIGGER {SqlIdentifier.Quote(DeleteTriggerName)} AFTER DELETE ON {SqlIdentifier.Quote(Table)} BEGIN " +
            $"DELETE FROM {SqlIdentifier.Quote(VirtualTableName)} WHERE {SqlIdentifier.Quote(EntityKeyColumnName)} = old.{SqlIdentifier.Quote(KeyColumnName)}; " +
            "END;";
    }
}
