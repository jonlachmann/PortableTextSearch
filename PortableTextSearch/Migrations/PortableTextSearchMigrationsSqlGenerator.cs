#pragma warning disable EF1001

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Sqlite.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Update.Internal;
using PortableTextSearch.Internal;
using PortableTextSearch.Migrations.Operations;

namespace PortableTextSearch.Migrations;

/// <summary>
/// Extends the SQLite migration SQL generator to handle PortableTextSearch custom operations.
/// </summary>
internal sealed class PortableTextSearchMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    IRelationalAnnotationProvider relationalAnnotationProvider)
    : SqliteMigrationsSqlGenerator(dependencies, relationalAnnotationProvider)
{
    protected override void Generate(
        MigrationOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        switch (operation)
        {
            case CreateSqliteTextSearchIndexOperation create:
                GenerateCreateSqliteTextSearchIndex(create, builder);
                break;
            case DropSqliteTextSearchIndexOperation drop:
                GenerateDropSqliteTextSearchIndex(drop, builder);
                break;
            default:
                base.Generate(operation, model, builder);
                break;
        }
    }

    private static void GenerateCreateSqliteTextSearchIndex(
        CreateSqliteTextSearchIndexOperation operation,
        MigrationCommandListBuilder builder)
    {
        var definition = SqliteTextSearchMigrationDefinition.Create(
            operation.Table,
            operation.Columns,
            operation.VirtualTableName,
            operation.ContentKeyColumn);

        builder.AppendLine(definition.CreateVirtualTableSql);
        builder.EndCommand();
        builder.AppendLine(definition.SeedExistingRowsSql);
        builder.EndCommand();
        builder.AppendLine(definition.InsertTriggerSql);
        builder.EndCommand();
        builder.AppendLine(definition.UpdateTriggerSql);
        builder.EndCommand();
        builder.AppendLine(definition.DeleteTriggerSql);
        builder.EndCommand();
    }

    private static void GenerateDropSqliteTextSearchIndex(
        DropSqliteTextSearchIndexOperation operation,
        MigrationCommandListBuilder builder)
    {
        var definition = SqliteTextSearchMigrationDefinition.Create(
            operation.Table,
            operation.Columns,
            operation.VirtualTableName,
            operation.ContentKeyColumn);

        builder.AppendLine($"DROP TRIGGER IF EXISTS {SqlIdentifier.Quote(definition.InsertTriggerName)};");
        builder.EndCommand();
        builder.AppendLine($"DROP TRIGGER IF EXISTS {SqlIdentifier.Quote(definition.UpdateTriggerName)};");
        builder.EndCommand();
        builder.AppendLine($"DROP TRIGGER IF EXISTS {SqlIdentifier.Quote(definition.DeleteTriggerName)};");
        builder.EndCommand();
        builder.AppendLine($"DROP TABLE IF EXISTS {SqlIdentifier.Quote(definition.VirtualTableName)};");
        builder.EndCommand();
    }
}

#pragma warning restore EF1001
