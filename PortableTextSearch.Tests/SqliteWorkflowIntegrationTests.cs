using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PortableTextSearch.Configuration;
using PortableTextSearch;
using PortableTextSearch.Functions;
using PortableTextSearch.Migrations;
using PortableTextSearch.Query;

namespace PortableTextSearch.Tests;

public sealed class SqliteWorkflowIntegrationTests
{
    [Fact]
    public async Task Migration_and_query_workflow_updates_fts_and_linq_queries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SqliteWorkflowContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(SqliteWorkflowContext).Assembly.FullName))
            .UsePortableTextSearch()
            .Options;

        await using (var setupContext = new SqliteWorkflowContext(options))
        {
            await setupContext.Database.MigrateAsync();

            setupContext.Recipients.Add(new WorkflowRecipient
            {
                Id = 1,
                MessageId = "message-1",
                Type = 7,
                Email = "alice@example.com",
                Name = "Alice Johnson"
            });
            setupContext.Recipients.Add(new WorkflowRecipient
            {
                Id = 2,
                MessageId = "message-2",
                Type = 7,
                Email = "bob@example.com",
                Name = "Alice Cooper"
            });

            await setupContext.SaveChangesAsync();
        }

        await using (var queryContext = new SqliteWorkflowContext(options))
        {
            var querySql = queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToQueryString();

            querySql.Should().Contain("MessageRecipients_TextSearch");
            querySql.Should().Contain("\"Email\" MATCH '\"alice\"'");

            var matches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToListAsync();

            matches.Should().ContainSingle(x => x.Email == "alice@example.com");

            var nameMatches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, "alice"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            nameMatches.Should().HaveCount(2);
            nameMatches.Select(x => x.Id).Should().Equal(1, 2);
        }

        await using (var updateContext = new SqliteWorkflowContext(options))
        {
            var recipient = (await updateContext.Recipients
                .Where(x => x.Id == 1)
                .ToListAsync())
                .Single();
            recipient.Email = "bob@example.com";
            recipient.Name = "Bob Stone";
            await updateContext.SaveChangesAsync();
        }

        await using (var updatedQueryContext = new SqliteWorkflowContext(options))
        {
            var emailAliceMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToListAsync();

            emailAliceMatches.Should().BeEmpty();

            var emailBobMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "bob"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            emailBobMatches.Should().HaveCount(2);
            emailBobMatches.Select(x => x.Id).Should().Equal(1, 2);

            var nameAliceMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, "alice"))
                .ToListAsync();

            nameAliceMatches.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }

        await using (var deleteContext = new SqliteWorkflowContext(options))
        {
            var recipient = (await deleteContext.Recipients
                .Where(x => x.Id == 1)
                .ToListAsync())
                .Single();
            deleteContext.Remove(recipient);
            await deleteContext.SaveChangesAsync();
        }

        await using (var deletedQueryContext = new SqliteWorkflowContext(options))
        {
            var emailBobMatches = await deletedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "bob"))
                .ToListAsync();

            emailBobMatches.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }
    }

    [Fact]
    public async Task Query_modes_compile_user_input_without_leaking_raw_sqlite_match_syntax()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SqliteWorkflowContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(SqliteWorkflowContext).Assembly.FullName))
            .UsePortableTextSearch()
            .Options;

        const string input = "draft query-1774300743237-8a756b7e";
        const string quotedInput = "say \"hello\"";

        await using (var setupContext = new SqliteWorkflowContext(options))
        {
            await setupContext.Database.MigrateAsync();
            setupContext.Recipients.Add(new WorkflowRecipient
            {
                Id = 1,
                MessageId = "message-1",
                Type = 7,
                Email = input,
                Name = quotedInput
            });

            await setupContext.SaveChangesAsync();
        }

        await using (var queryContext = new SqliteWorkflowContext(options))
        {
            var anyTerms = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, input))
                .ToListAsync();

            anyTerms.Should().ContainSingle().Which.Id.Should().Be(1);

            var allTerms = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, input, TextSearchMode.AllTerms))
                .ToListAsync();

            allTerms.Should().ContainSingle().Which.Id.Should().Be(1);

            var phrase = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, input, TextSearchMode.Phrase))
                .ToListAsync();

            phrase.Should().ContainSingle().Which.Id.Should().Be(1);

            var quotedPhrase = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, quotedInput, TextSearchMode.Phrase))
                .ToListAsync();

            quotedPhrase.Should().ContainSingle().Which.Id.Should().Be(1);

            var blank = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "   "))
                .ToListAsync();

            blank.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Migration_and_query_workflow_supports_guid_primary_keys()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SqliteGuidWorkflowContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(SqliteGuidWorkflowContext).Assembly.FullName))
            .UsePortableTextSearch()
            .Options;

        var recipientId = Guid.NewGuid();

        await using (var setupContext = new SqliteGuidWorkflowContext(options))
        {
            await setupContext.Database.MigrateAsync();

            setupContext.Recipients.Add(new GuidWorkflowRecipient
            {
                Id = recipientId,
                MessageId = "message-guid-1",
                Type = 4,
                Email = "guid.alice@example.com",
                Name = "Guid Alice"
            });

            await setupContext.SaveChangesAsync();
        }

        await using (var queryContext = new SqliteGuidWorkflowContext(options))
        {
            var querySql = queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "guid"))
                .ToQueryString();

            querySql.Should().Contain("GuidRecipients_TextSearch");
            querySql.Should().Contain("\"__pts_entity_key\"");
            querySql.Should().Contain("\"Email\" MATCH '\"guid\"'");

            var matches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "guid"))
                .ToListAsync();

            matches.Should().ContainSingle();
            matches[0].Id.Should().Be(recipientId);
        }
    }

    public sealed class SqliteWorkflowContext(DbContextOptions<SqliteWorkflowContext> options) : DbContext(options)
    {
        public DbSet<WorkflowRecipient> Recipients => Set<WorkflowRecipient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowRecipient>(builder =>
            {
                builder.ToTable("MessageRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.Property(x => x.Name).HasMaxLength(256);
                builder.HasTextSearch(x => x.Email)
                    .HasTextSearch(x => x.Name);
            });
        }
    }

    public sealed class SqliteGuidWorkflowContext(DbContextOptions<SqliteGuidWorkflowContext> options) : DbContext(options)
    {
        public DbSet<GuidWorkflowRecipient> Recipients => Set<GuidWorkflowRecipient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidWorkflowRecipient>(builder =>
            {
                builder.ToTable("GuidRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.Property(x => x.Name).HasMaxLength(256);
                builder.HasTextSearch(x => x.Email)
                    .HasTextSearch(x => x.Name);
            });
        }
    }

    public sealed class WorkflowRecipient
    {
        public int Id { get; init; }

        [MaxLength(128)]
        public string MessageId { get; init; } = null!;

        public int Type { get; init; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }
    }

    public sealed class GuidWorkflowRecipient
    {
        public Guid Id { get; init; }

        [MaxLength(128)]
        public string MessageId { get; init; } = null!;

        public int Type { get; init; }

        [MaxLength(256)]
        public string? Email { get; init; }

        [MaxLength(256)]
        public string? Name { get; init; }
    }

    [DbContext(typeof(SqliteWorkflowContext))]
    [Migration("202603200101_CreateWorkflowSchema")]
    public sealed class CreateWorkflowSchemaMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", false),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_MessageRecipients", x => x.Id));

            migrationBuilder.CreateSqliteTextSearchIndex(
                table: "MessageRecipients",
                columns: ["Email", "Name"],
                contentRowIdColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSqliteTextSearchIndex(
                table: "MessageRecipients",
                columns: ["Email", "Name"],
                contentRowIdColumn: "Id");

            migrationBuilder.DropTable("MessageRecipients");
        }
    }

    [DbContext(typeof(SqliteGuidWorkflowContext))]
    [Migration("202603210001_CreateGuidWorkflowSchema")]
    public sealed class CreateGuidWorkflowSchemaMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuidRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_GuidRecipients", x => x.Id));

            migrationBuilder.CreateSqliteTextSearchIndex(
                table: "GuidRecipients",
                columns: ["Email", "Name"],
                contentRowIdColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSqliteTextSearchIndex(
                table: "GuidRecipients",
                columns: ["Email", "Name"],
                contentRowIdColumn: "Id");

            migrationBuilder.DropTable("GuidRecipients");
        }
    }
}
