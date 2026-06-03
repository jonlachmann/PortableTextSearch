#pragma warning disable EF1001

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
using PortableTextSearch.Internal;
using PortableTextSearch.Migrations.Operations;

namespace PortableTextSearch.Migrations;

internal sealed class PortableTextSearchNpgsqlMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlSingletonOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
{
    protected override void Generate(
        MigrationOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        switch (operation)
        {
            case EnsurePostgresTrigramExtensionOperation:
                builder.AppendLine("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
                builder.EndCommand();
                break;
            case CreatePostgresTextSearchIndexOperation create:
                GenerateCreateTextSearchIndex(create, builder);
                break;
            case DropPostgresTextSearchIndexOperation drop:
                GenerateDropTextSearchIndex(drop, builder);
                break;
            default:
                base.Generate(operation, model, builder);
                break;
        }
    }

    private static void GenerateCreateTextSearchIndex(
        CreatePostgresTextSearchIndexOperation operation,
        MigrationCommandListBuilder builder)
    {
        var indexName = operation.IndexName ?? $"IX_{operation.Table}_{operation.Column}_TextSearch";
        var qualifiedTable = operation.Schema is null
            ? SqlIdentifier.Quote(operation.Table)
            : $"{SqlIdentifier.Quote(operation.Schema)}.{SqlIdentifier.Quote(operation.Table)}";

        builder.AppendLine(
            $"CREATE INDEX {SqlIdentifier.Quote(indexName)} ON {qualifiedTable} USING GIN ({SqlIdentifier.Quote(operation.Column)} gin_trgm_ops);");
        builder.EndCommand();
    }

    private static void GenerateDropTextSearchIndex(
        DropPostgresTextSearchIndexOperation operation,
        MigrationCommandListBuilder builder)
    {
        var indexName = $"IX_{operation.Table}_{operation.Column}_TextSearch";
        var qualifiedIndexName = operation.Schema is null
            ? SqlIdentifier.Quote(indexName)
            : $"{SqlIdentifier.Quote(operation.Schema)}.{SqlIdentifier.Quote(indexName)}";

        builder.AppendLine($"DROP INDEX IF EXISTS {qualifiedIndexName};");
        builder.EndCommand();
    }
}

#pragma warning restore EF1001
