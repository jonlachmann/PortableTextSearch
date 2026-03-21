using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PortableTextSearch.Configuration;
using PortableTextSearch.Functions;
using PortableTextSearch.Migrations;
using PortableTextSearch.Query;

namespace PortableTextSearch.Tests;

public sealed class PostgreSqlWorkflowIntegrationTests
{
    private const string SchemaName = "pts_pgsql_workflow";

    [RequiresPostgresFact]
    public async Task Migration_and_query_workflow_runs_against_postgresql()
    {
        var configuration = PostgreSqlTestConfiguration.TryLoad()
            ?? throw new InvalidOperationException("PostgreSQL test configuration is required.");
        await using var databaseScope = await PostgreSqlTestDatabaseScope.CreateAsync(configuration);
        var connectionString = databaseScope.DatabaseConnectionString;

        var options = new DbContextOptionsBuilder<PostgreSqlWorkflowContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .MigrationsAssembly(typeof(PostgreSqlWorkflowContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", SchemaName))
            .UsePortableTextSearch()
            .Options;

        await using (var setupContext = new PostgreSqlWorkflowContext(options))
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

        await using (var queryContext = new PostgreSqlWorkflowContext(options))
        {
            var querySql = queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToQueryString();

            querySql.Should().Contain("ILIKE");
            querySql.Should().Contain("\"Email\"");

            var emailMatches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToListAsync();

            emailMatches.Should().ContainSingle()
                .Which.Id.Should().Be(1);

            var nameMatches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, "alice"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            nameMatches.Select(x => x.Id).Should().Equal(1, 2);
        }

        await using (var updateContext = new PostgreSqlWorkflowContext(options))
        {
            var recipient = (await updateContext.Recipients
                .Where(x => x.Id == 1)
                .ToListAsync())
                .Single();
            recipient.Email = "carol@example.com";
            recipient.Name = "Carol Stone";
            await updateContext.SaveChangesAsync();
        }

        await using (var updatedQueryContext = new PostgreSqlWorkflowContext(options))
        {
            var emailAliceMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToListAsync();

            emailAliceMatches.Should().BeEmpty();

            var emailCarolMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "carol"))
                .ToListAsync();

            emailCarolMatches.Should().ContainSingle()
                .Which.Id.Should().Be(1);

            var nameAliceMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, "alice"))
                .ToListAsync();

            nameAliceMatches.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }

        await using (var deleteContext = new PostgreSqlWorkflowContext(options))
        {
            var recipient = (await deleteContext.Recipients
                .Where(x => x.Id == 1)
                .ToListAsync())
                .Single();
            deleteContext.Remove(recipient);
            await deleteContext.SaveChangesAsync();
        }

        await using (var deletedQueryContext = new PostgreSqlWorkflowContext(options))
        {
            var emailCarolMatches = await deletedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "carol"))
                .ToListAsync();

            emailCarolMatches.Should().BeEmpty();

            var remainingMatches = await deletedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "bob"))
                .ToListAsync();

            remainingMatches.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }
    }

    public sealed class PostgreSqlWorkflowContext(DbContextOptions<PostgreSqlWorkflowContext> options) : DbContext(options)
    {
        public DbSet<WorkflowRecipient> Recipients => Set<WorkflowRecipient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);

            modelBuilder.Entity<WorkflowRecipient>(builder =>
            {
                builder.ToTable("MessageRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.MessageId).IsRequired();
                builder.HasTextSearch(x => x.Email)
                    .HasTextSearch(x => x.Name);
            });
        }
    }

    public sealed class WorkflowRecipient
    {
        public int Id { get; set; }

        public string MessageId { get; set; } = null!;

        public int Type { get; set; }

        public string? Email { get; set; }

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
                schema: SchemaName,
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    MessageId = table.Column<string>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_MessageRecipients", x => x.Id));

            migrationBuilder.CreatePostgresTextSearchIndex(
                table: "MessageRecipients",
                column: "Email",
                schema: SchemaName);

            migrationBuilder.CreatePostgresTextSearchIndex(
                table: "MessageRecipients",
                column: "Name",
                schema: SchemaName);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""DROP TABLE IF EXISTS "{SchemaName}"."MessageRecipients";""");
        }
    }
}
