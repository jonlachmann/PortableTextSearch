using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PortableTextSearch.Configuration;
using PortableTextSearch.Migrations.Operations;
using PortableTextSearch.Query;

namespace PortableTextSearch.Tests;

public sealed class MigrationSqlGeneratorTests
{
    [Fact]
    public void Npgsql_sql_generator_handles_EnsurePostgresTrigramExtensionOperation()
    {
        using var context = CreatePostgresContext();
        var generator = context.GetService<IMigrationsSqlGenerator>();

        var commands = generator.Generate(
            [new EnsurePostgresTrigramExtensionOperation()],
            context.Model);

        commands.Should().ContainSingle();
        commands[0].CommandText.Should().Contain("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
    }

    [Fact]
    public void Npgsql_sql_generator_handles_CreatePostgresTextSearchIndexOperation()
    {
        using var context = CreatePostgresContext();
        var generator = context.GetService<IMigrationsSqlGenerator>();

        var commands = generator.Generate(
            [new CreatePostgresTextSearchIndexOperation { Table = "Recipients", Column = "Email" }],
            context.Model);

        commands.Should().ContainSingle();
        commands[0].CommandText.Should().Contain("CREATE INDEX");
        commands[0].CommandText.Should().Contain("USING GIN");
        commands[0].CommandText.Should().Contain("gin_trgm_ops");
    }

    [Fact]
    public void Npgsql_sql_generator_handles_DropPostgresTextSearchIndexOperation()
    {
        using var context = CreatePostgresContext();
        var generator = context.GetService<IMigrationsSqlGenerator>();

        var commands = generator.Generate(
            [new DropPostgresTextSearchIndexOperation { Table = "Recipients", Column = "Email" }],
            context.Model);

        commands.Should().ContainSingle();
        commands[0].CommandText.Should().Contain("DROP INDEX IF EXISTS");
    }

    [Fact]
    public void Sqlite_sql_generator_handles_CreateSqliteTextSearchIndexOperation()
    {
        using var context = CreateSqliteContext();
        var generator = context.GetService<IMigrationsSqlGenerator>();

        var commands = generator.Generate(
            [new CreateSqliteTextSearchIndexOperation
            {
                Table = "Recipients",
                Columns = new[] { "Email", "Name" },
                ContentKeyColumn = "Id"
            }],
            context.Model);

        commands.Should().HaveCountGreaterThan(1);
        commands[0].CommandText.Should().Contain("CREATE VIRTUAL TABLE");
        commands[0].CommandText.Should().Contain("fts5");
    }

    [Fact]
    public void Sqlite_sql_generator_handles_DropSqliteTextSearchIndexOperation()
    {
        using var context = CreateSqliteContext();
        var generator = context.GetService<IMigrationsSqlGenerator>();

        var commands = generator.Generate(
            [new DropSqliteTextSearchIndexOperation
            {
                Table = "Recipients",
                Columns = new[] { "Email", "Name" },
                ContentKeyColumn = "Id"
            }],
            context.Model);

        commands.Should().HaveCountGreaterThan(1);
        commands.Select(c => c.CommandText)
            .Should().Contain(s => s.Contains("DROP TRIGGER IF EXISTS"))
            .And.Contain(s => s.Contains("DROP TABLE IF EXISTS"));
    }

    [Fact]
    public void Npgsql_sql_generator_handles_mixed_operations()
    {
        using var context = CreatePostgresContext();
        var generator = context.GetService<IMigrationsSqlGenerator>();

        var operations = new MigrationOperation[]
        {
            new EnsurePostgresTrigramExtensionOperation(),
            new CreatePostgresTextSearchIndexOperation { Table = "Recipients", Column = "Email" },
            new CreatePostgresTextSearchIndexOperation { Table = "Recipients", Column = "Name" },
        };

        var commands = generator.Generate(operations, context.Model);

        commands.Should().HaveCount(3);
        commands.Select(c => c.CommandText)
            .Should().Contain(s => s.Contains("CREATE EXTENSION IF NOT EXISTS pg_trgm"))
            .And.Contain(s => s.Contains("\"Email\" gin_trgm_ops"))
            .And.Contain(s => s.Contains("\"Name\" gin_trgm_ops"));
    }

    private static TestContext CreatePostgresContext(bool configureTextSearch = true)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .UsePortableTextSearch()
            .Options;
        return new TestContext(options, configureTextSearch);
    }

    private static TestContext CreateSqliteContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite("Data Source=migration-sql-gen-test.db")
            .UsePortableTextSearch()
            .Options;
        return new TestContext(options, configureTextSearch: true);
    }

    private sealed class TestContext : DbContext
    {
        private readonly bool _configureTextSearch;

        public TestContext(DbContextOptions<TestContext> options, bool configureTextSearch)
            : base(options)
        {
            _configureTextSearch = configureTextSearch;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recipient>(builder =>
            {
                builder.ToTable("Recipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.Property(x => x.Name).HasMaxLength(256);
                if (_configureTextSearch)
                {
                    builder.HasTextSearch(x => x.Email)
                        .HasTextSearch(x => x.Name);
                }
            });
        }
    }

    private sealed class Recipient
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }
}
