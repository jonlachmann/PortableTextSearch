using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PortableTextSearch.Configuration;
using PortableTextSearch.Migrations;

namespace PortableTextSearch.Tests;

internal static class PostgreSqlWorkflowFixture
{
    public const string SchemaName = "pts_pgsql_workflow";
}

public sealed class PostgreSqlWorkflowContext(DbContextOptions<PostgreSqlWorkflowContext> options) : DbContext(options)
{
    public DbSet<PostgreSqlWorkflowRecipient> Recipients => Set<PostgreSqlWorkflowRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgreSqlWorkflowFixture.SchemaName);

        modelBuilder.Entity<PostgreSqlWorkflowRecipient>(builder =>
        {
            builder.ToTable("MessageRecipients");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.UnindexedEmail).HasMaxLength(256);
            builder.Property(x => x.Name).HasMaxLength(256);
            builder.HasTextSearch(x => x.Email)
                .HasTextSearch(x => x.Name);
        });
    }
}

public sealed class PostgreSqlWorkflowRecipient
{
    public int Id { get; init; }

    [MaxLength(128)]
    public string MessageId { get; init; } = null!;

    public int Type { get; init; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? UnindexedEmail { get; set; }

    [MaxLength(256)]
    public string? Name { get; set; }
}

[DbContext(typeof(PostgreSqlWorkflowContext))]
[Migration("202603210101_CreatePostgreSqlWorkflowSchema")]
public sealed class CreatePostgreSqlWorkflowSchemaMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsurePostgresTrigramExtension();

        migrationBuilder.CreateTable(
            name: "MessageRecipients",
            schema: PostgreSqlWorkflowFixture.SchemaName,
            columns: table => new
            {
                Id = table.Column<int>(nullable: false),
                MessageId = table.Column<string>(maxLength: 128, nullable: false),
                Type = table.Column<int>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: true),
                UnindexedEmail = table.Column<string>(maxLength: 256, nullable: true),
                Name = table.Column<string>(maxLength: 256, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_MessageRecipients", x => x.Id));

        migrationBuilder.CreatePostgresTextSearchIndex(
            table: "MessageRecipients",
            column: "Email",
            schema: PostgreSqlWorkflowFixture.SchemaName);

        migrationBuilder.CreatePostgresTextSearchIndex(
            table: "MessageRecipients",
            column: "Name",
            schema: PostgreSqlWorkflowFixture.SchemaName);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""DROP TABLE IF EXISTS "{PostgreSqlWorkflowFixture.SchemaName}"."MessageRecipients";""");
    }
}
