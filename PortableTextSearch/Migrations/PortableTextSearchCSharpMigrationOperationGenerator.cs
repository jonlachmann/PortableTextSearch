using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PortableTextSearch.Migrations.Operations;

namespace PortableTextSearch.Migrations;

internal sealed class PortableTextSearchCSharpMigrationOperationGenerator(
    CSharpMigrationOperationGeneratorDependencies dependencies)
    : CSharpMigrationOperationGenerator(dependencies)
{
    protected override void Generate(MigrationOperation operation, IndentedStringBuilder builder)
    {
        switch (operation)
        {
            case EnsurePostgresTrigramExtensionOperation:
                builder.AppendLine(".EnsurePostgresTrigramExtension();");
                return;
            case CreatePostgresTextSearchIndexOperation createPostgres:
                GenerateCreatePostgresTextSearchIndex(createPostgres, builder);
                return;
            case DropPostgresTextSearchIndexOperation dropPostgres:
                GenerateDropPostgresTextSearchIndex(dropPostgres, builder);
                return;
            case CreateSqliteTextSearchIndexOperation createSqlite:
                GenerateCreateSqliteTextSearchIndex(createSqlite, builder);
                return;
            case DropSqliteTextSearchIndexOperation dropSqlite:
                GenerateDropSqliteTextSearchIndex(dropSqlite, builder);
                return;
            default:
                base.Generate(operation, builder);
                return;
        }
    }

    private void GenerateCreatePostgresTextSearchIndex(
        CreatePostgresTextSearchIndexOperation operation,
        IndentedStringBuilder builder)
    {
        builder.Append(".CreatePostgresTextSearchIndex(")
            .Append($"table: {Dependencies.CSharpHelper.Literal(operation.Table)}, ")
            .Append($"column: {Dependencies.CSharpHelper.Literal(operation.Column)}");

        if (operation.Schema is not null)
        {
            builder.Append($", schema: {Dependencies.CSharpHelper.Literal(operation.Schema)}");
        }

        if (operation.IndexName is not null)
        {
            builder.Append($", indexName: {Dependencies.CSharpHelper.Literal(operation.IndexName)}");
        }

        builder.AppendLine(");");
    }

    private void GenerateDropPostgresTextSearchIndex(
        DropPostgresTextSearchIndexOperation operation,
        IndentedStringBuilder builder)
    {
        builder.Append(".DropPostgresTextSearchIndex(")
            .Append($"table: {Dependencies.CSharpHelper.Literal(operation.Table)}, ")
            .Append($"column: {Dependencies.CSharpHelper.Literal(operation.Column)}");

        if (operation.Schema is not null)
        {
            builder.Append($", schema: {Dependencies.CSharpHelper.Literal(operation.Schema)}");
        }

        builder.AppendLine(");");
    }

    private void GenerateCreateSqliteTextSearchIndex(
        CreateSqliteTextSearchIndexOperation operation,
        IndentedStringBuilder builder)
    {
        builder.Append(".CreateSqliteTextSearchIndex(")
            .Append($"table: {Dependencies.CSharpHelper.Literal(operation.Table)}, ")
            .Append($"columns: {BuildStringArrayLiteral(operation.Columns)}");

        if (operation.VirtualTableName is not null)
        {
            builder.Append($", virtualTableName: {Dependencies.CSharpHelper.Literal(operation.VirtualTableName)}");
        }

        builder.Append($", contentRowIdColumn: {Dependencies.CSharpHelper.Literal(operation.ContentKeyColumn)})")
            .AppendLine(";");
    }

    private void GenerateDropSqliteTextSearchIndex(
        DropSqliteTextSearchIndexOperation operation,
        IndentedStringBuilder builder)
    {
        builder.Append(".DropSqliteTextSearchIndex(")
            .Append($"table: {Dependencies.CSharpHelper.Literal(operation.Table)}, ")
            .Append($"columns: {BuildStringArrayLiteral(operation.Columns)}");

        if (operation.VirtualTableName is not null)
        {
            builder.Append($", virtualTableName: {Dependencies.CSharpHelper.Literal(operation.VirtualTableName)}");
        }

        builder.Append($", contentRowIdColumn: {Dependencies.CSharpHelper.Literal(operation.ContentKeyColumn)})")
            .AppendLine(";");
    }

    private string BuildStringArrayLiteral(IReadOnlyList<string> values)
        => $"new[] {{ {string.Join(", ", values.Select(Dependencies.CSharpHelper.Literal))} }}";
}
