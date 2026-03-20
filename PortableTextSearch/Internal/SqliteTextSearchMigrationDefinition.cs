namespace PortableTextSearch.Internal;

internal sealed class SqliteTextSearchMigrationDefinition
{
    private SqliteTextSearchMigrationDefinition(
        string table,
        IReadOnlyList<string> columns,
        string virtualTableName,
        string contentRowIdColumn)
    {
        Table = table;
        Columns = columns;
        VirtualTableName = virtualTableName;
        ContentRowIdColumn = contentRowIdColumn;
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

    public string ContentRowIdColumn { get; }

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
        string contentRowIdColumn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRowIdColumn);

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
            contentRowIdColumn);
    }

    private string BuildCreateVirtualTableSql()
    {
        var columnList = string.Join(", ", Columns.Select(SqlIdentifier.Quote));
        return
            $"CREATE VIRTUAL TABLE {SqlIdentifier.Quote(VirtualTableName)} USING fts5({columnList}, content={SqlStringLiteral.Quote(Table)}, content_rowid={SqlStringLiteral.Quote(ContentRowIdColumn)});";
    }

    private string BuildSeedExistingRowsSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote));
        var selectedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote));
        return
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} (rowid, {quotedColumns}) SELECT {SqlIdentifier.Quote(ContentRowIdColumn)}, {selectedColumns} FROM {SqlIdentifier.Quote(Table)};";
    }

    private string BuildInsertTriggerSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote));
        var insertedValues = string.Join(", ", Columns.Select(column => $"new.{SqlIdentifier.Quote(column)}"));

        return
            $"CREATE TRIGGER {SqlIdentifier.Quote(InsertTriggerName)} AFTER INSERT ON {SqlIdentifier.Quote(Table)} BEGIN " +
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} (rowid, {quotedColumns}) VALUES (new.{SqlIdentifier.Quote(ContentRowIdColumn)}, {insertedValues}); " +
            "END;";
    }

    private string BuildUpdateTriggerSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote));
        var deletedValues = string.Join(", ", Columns.Select(column => $"old.{SqlIdentifier.Quote(column)}"));
        var insertedValues = string.Join(", ", Columns.Select(column => $"new.{SqlIdentifier.Quote(column)}"));

        return
            $"CREATE TRIGGER {SqlIdentifier.Quote(UpdateTriggerName)} AFTER UPDATE ON {SqlIdentifier.Quote(Table)} BEGIN " +
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} ({SqlIdentifier.Quote(VirtualTableName)}, rowid, {quotedColumns}) VALUES ({SqlStringLiteral.Quote("delete")}, old.{SqlIdentifier.Quote(ContentRowIdColumn)}, {deletedValues}); " +
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} (rowid, {quotedColumns}) VALUES (new.{SqlIdentifier.Quote(ContentRowIdColumn)}, {insertedValues}); " +
            "END;";
    }

    private string BuildDeleteTriggerSql()
    {
        var quotedColumns = string.Join(", ", Columns.Select(SqlIdentifier.Quote));
        var deletedValues = string.Join(", ", Columns.Select(column => $"old.{SqlIdentifier.Quote(column)}"));

        return
            $"CREATE TRIGGER {SqlIdentifier.Quote(DeleteTriggerName)} AFTER DELETE ON {SqlIdentifier.Quote(Table)} BEGIN " +
            $"INSERT INTO {SqlIdentifier.Quote(VirtualTableName)} ({SqlIdentifier.Quote(VirtualTableName)}, rowid, {quotedColumns}) VALUES ({SqlStringLiteral.Quote("delete")}, old.{SqlIdentifier.Quote(ContentRowIdColumn)}, {deletedValues}); " +
            "END;";
    }
}
