using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PortableTextSearch.Migrations;

namespace PortableTextSearch.Tests;

public sealed class MigrationHelperTests
{
    [Fact]
    public void EnsurePostgresTrigramExtension_emits_expected_sql()
    {
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migrationBuilder.EnsurePostgresTrigramExtension();

        GetSqlOperations(migrationBuilder)
            .Single()
            .Sql
            .Should()
            .Be("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
    }

    [Fact]
    public void CreatePostgresTextSearchIndex_emits_expected_sql_with_default_name()
    {
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migrationBuilder.CreatePostgresTextSearchIndex("MessageRecipients", "Email");

        GetSqlOperations(migrationBuilder)
            .Single()
            .Sql
            .Should()
            .Be("CREATE INDEX \"IX_MessageRecipients_Email_TextSearch\" ON \"MessageRecipients\" USING GIN (\"Email\" gin_trgm_ops);");
    }

    [Fact]
    public void CreatePostgresTextSearchIndex_supports_schema_and_custom_name()
    {
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migrationBuilder.CreatePostgresTextSearchIndex(
            table: "MessageRecipients",
            column: "Email",
            schema: "app",
            indexName: "IX_Custom_Search");

        GetSqlOperations(migrationBuilder)
            .Single()
            .Sql
            .Should()
            .Be("CREATE INDEX \"IX_Custom_Search\" ON \"app\".\"MessageRecipients\" USING GIN (\"Email\" gin_trgm_ops);");
    }

    [Fact]
    public void CreateSqliteTextSearchIndex_is_an_explicit_stub()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");

        migrationBuilder.CreateSqliteTextSearchIndex("MessageRecipients", ["Email", "Name"]);

        var sql = GetSqlOperations(migrationBuilder).Select(x => x.Sql).ToArray();

        sql.Should().HaveCount(5);
        sql[0].Should().Contain("CREATE VIRTUAL TABLE \"MessageRecipients_TextSearch\" USING fts5");
        sql[0].Should().Contain("content='MessageRecipients'");
        sql[1].Should().Contain("INSERT INTO \"MessageRecipients_TextSearch\"");
        sql[2].Should().Contain("CREATE TRIGGER \"trg_MessageRecipients_MessageRecipients_TextSearch_ai\"");
        sql[3].Should().Contain("CREATE TRIGGER \"trg_MessageRecipients_MessageRecipients_TextSearch_au\"");
        sql[4].Should().Contain("CREATE TRIGGER \"trg_MessageRecipients_MessageRecipients_TextSearch_ad\"");
    }

    [Fact]
    public void DropSqliteTextSearchIndex_emits_drop_statements()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");

        migrationBuilder.DropSqliteTextSearchIndex("MessageRecipients", ["Email", "Name"]);

        GetSqlOperations(migrationBuilder)
            .Select(x => x.Sql)
            .Should()
            .Equal(
            [
                "DROP TRIGGER IF EXISTS \"trg_MessageRecipients_MessageRecipients_TextSearch_ai\";",
                "DROP TRIGGER IF EXISTS \"trg_MessageRecipients_MessageRecipients_TextSearch_au\";",
                "DROP TRIGGER IF EXISTS \"trg_MessageRecipients_MessageRecipients_TextSearch_ad\";",
                "DROP TABLE IF EXISTS \"MessageRecipients_TextSearch\";"
            ]);
    }

    private static IReadOnlyList<SqlOperation> GetSqlOperations(MigrationBuilder migrationBuilder)
        => migrationBuilder.Operations.OfType<SqlOperation>().ToArray();
}
