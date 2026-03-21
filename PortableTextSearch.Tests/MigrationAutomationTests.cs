using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using PortableTextSearch.Configuration;
using PortableTextSearch.Design;
using PortableTextSearch.Functions;
using PortableTextSearch.Migrations.Operations;
using PortableTextSearch.Query;
using PortableTextSearch.Tests.TestModel;

namespace PortableTextSearch.Tests;

public sealed class MigrationAutomationTests
{
    [Fact]
    public void Sqlite_model_diff_creates_text_search_sql_operations_from_HasTextSearch()
    {
        using var sourceContext = CreateSqliteContext<SqliteWithoutTextSearchContext>();
        using var targetContext = CreateSqliteContext<SqliteWithTextSearchContext>();

        var operations = GetOperations(targetContext, sourceContext, targetContext);
        var createOperation = operations.OfType<CreateSqliteTextSearchIndexOperation>().Single();

        createOperation.Table.Should().Be("MessageRecipients");
        createOperation.Columns.Should().Equal("Email", "Name");
        createOperation.ContentKeyColumn.Should().Be("Id");
    }

    [Fact]
    public void Sqlite_model_diff_drops_text_search_sql_operations_when_HasTextSearch_is_removed()
    {
        using var sourceContext = CreateSqliteContext<SqliteWithTextSearchContext>();
        using var targetContext = CreateSqliteContext<SqliteWithoutTextSearchContext>();

        var operations = GetOperations(targetContext, sourceContext, targetContext);
        var dropOperation = operations.OfType<DropSqliteTextSearchIndexOperation>().Single();

        dropOperation.Table.Should().Be("MessageRecipients");
        dropOperation.Columns.Should().Equal("Email", "Name");
        dropOperation.ContentKeyColumn.Should().Be("Id");
    }

    [Fact]
    public void Postgres_model_diff_creates_text_search_sql_operations_from_HasTextSearch()
    {
        using var sourceContext = CreatePostgresContext<PostgresWithoutTextSearchContext>();
        using var targetContext = CreatePostgresContext<PostgresWithTextSearchContext>();

        var operations = GetOperations(targetContext, sourceContext, targetContext);
        operations.OfType<EnsurePostgresTrigramExtensionOperation>().Should().ContainSingle();
        operations.OfType<CreatePostgresTextSearchIndexOperation>()
            .Select(operation => operation.Column)
            .Should()
            .Equal("Email", "Name");
    }

    [Fact]
    public void Postgres_model_diff_drops_text_search_sql_operations_when_HasTextSearch_is_removed()
    {
        using var sourceContext = CreatePostgresContext<PostgresWithTextSearchContext>();
        using var targetContext = CreatePostgresContext<PostgresWithoutTextSearchContext>();

        var operations = GetOperations(targetContext, sourceContext, targetContext);
        operations.OfType<DropPostgresTextSearchIndexOperation>()
            .Select(operation => operation.Column)
            .Should()
            .Equal("Email", "Name");
    }

    [Fact]
    public void Scaffolded_sqlite_migration_snapshot_preserves_text_search_annotation()
    {
        using var context = CreateSqliteContext<SqliteWithTextSearchContext>();
        var serviceProvider = BuildDesignTimeServiceProvider(context);
        var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
        var scaffoldedMigration = scaffolder.ScaffoldMigration("AddPortableTextSearch", "PortableTextSearch.Tests", subNamespace: null, language: "C#");

        scaffoldedMigration.MigrationCode.Should().Contain("migrationBuilder.CreateSqliteTextSearchIndex(");
        scaffoldedMigration.SnapshotCode.Should().Contain("PortableTextSearch:SearchableProperties");
        scaffoldedMigration.SnapshotCode.Should().Contain("Email");
        scaffoldedMigration.SnapshotCode.Should().Contain("Name");
    }

    [Fact]
    public void Scaffolded_sqlite_migration_can_be_compiled_and_applied_to_a_real_database()
    {
        using var designTimeContext = CreateSqliteContext<SqliteWithTextSearchContext>();
        var serviceProvider = BuildDesignTimeServiceProvider(designTimeContext);
        var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
        var scaffoldedMigration = scaffolder.ScaffoldMigration("AddPortableTextSearch", "PortableTextSearch.Tests", subNamespace: null, language: "C#");
        var migrationAssembly = CompileMigrationAssembly(scaffoldedMigration.MigrationCode);
        var migration = migrationAssembly.GetTypes()
            .Single(type => typeof(Migration).IsAssignableFrom(type) && !type.IsAbstract);
        var migrationInstance = (Migration)Activator.CreateInstance(migration)!;
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");

        migration.GetMethod("Up", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(migrationInstance, [migrationBuilder]);

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SqliteWithTextSearchContext>()
            .UseSqlite(connection)
            .UsePortableTextSearch()
            .Options;

        using var runtimeContext = new SqliteWithTextSearchContext(options);
        ExecuteMigrationOperations(runtimeContext, migrationBuilder.Operations);

        runtimeContext.Add(new MessageRecipient
        {
            MessageId = "m-1",
            Type = 1,
            Email = "alice@example.com",
            Name = "Alice"
        });
        runtimeContext.SaveChanges();

        runtimeContext.Set<MessageRecipient>()
            .Where(x => EF.Functions.TextContains(x.Email, "alice"))
            .Select(x => x.Id)
            .ToArray()
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void Sqlite_model_diff_supports_guid_primary_keys()
    {
        using var sourceContext = CreateSqliteContext<SqliteWithoutTextSearchGuidKeyContext>();
        using var targetContext = CreateSqliteContext<SqliteWithTextSearchGuidKeyContext>();

        var operations = GetOperations(targetContext, sourceContext, targetContext);
        var createOperation = operations.OfType<CreateSqliteTextSearchIndexOperation>().Single();

        createOperation.Table.Should().Be("GuidKeyRecipients");
        createOperation.ContentKeyColumn.Should().Be("Id");
    }

    private static IReadOnlyList<MigrationOperation> GetOperations(
        DbContext differContext,
        DbContext sourceContext,
        DbContext targetContext)
    {
        var differ = differContext.GetService<IMigrationsModelDiffer>();
        var sourceModel = sourceContext.GetService<IDesignTimeModel>().Model;
        var targetModel = targetContext.GetService<IDesignTimeModel>().Model;
        return differ.GetDifferences(sourceModel.GetRelationalModel(), targetModel.GetRelationalModel());
    }

    private static ServiceProvider BuildDesignTimeServiceProvider(DbContext context)
    {
        var operationReportHandlerType = Type.GetType(
            "Microsoft.EntityFrameworkCore.Design.OperationReportHandler, Microsoft.EntityFrameworkCore.Design",
            throwOnError: true)!;
        var reportHandler = Activator.CreateInstance(
            operationReportHandlerType,
            (Action<string>)(_ => { }),
            (Action<string>)(_ => { }),
            (Action<string>)(_ => { }),
            (Action<string>)(_ => { }))!;
        var operationReporterInterface = Type.GetType(
            "Microsoft.EntityFrameworkCore.Design.Internal.IOperationReporter, Microsoft.EntityFrameworkCore.Design",
            throwOnError: true)!;
        var reporterType = Type.GetType(
            "Microsoft.EntityFrameworkCore.Design.Internal.OperationReporter, Microsoft.EntityFrameworkCore.Design",
            throwOnError: true)!;
        var reporter = Activator.CreateInstance(reporterType, reportHandler)!;
        var designTimeServicesBuilderType = Type.GetType(
            "Microsoft.EntityFrameworkCore.Design.Internal.DesignTimeServicesBuilder, Microsoft.EntityFrameworkCore.Design",
            throwOnError: true)!;
        var constructor = designTimeServicesBuilderType.GetConstructor(
            [typeof(System.Reflection.Assembly), typeof(System.Reflection.Assembly), operationReporterInterface, typeof(string[])])
            ?? throw new InvalidOperationException("Unable to locate DesignTimeServicesBuilder constructor.");
        var builder = constructor.Invoke(
            [typeof(MigrationAutomationTests).Assembly, context.GetType().Assembly, reporter, Array.Empty<string>()]);
        var createServiceCollectionMethod = designTimeServicesBuilderType.GetMethod("CreateServiceCollection", [typeof(DbContext)])
            ?? throw new InvalidOperationException("Unable to locate DesignTimeServicesBuilder.CreateServiceCollection(DbContext).");
        var services = (IServiceCollection)createServiceCollectionMethod.Invoke(builder, [context])!;

        new PortableTextSearchDesignTimeServices().ConfigureDesignTimeServices(services);

        return services.BuildServiceProvider();
    }

    private static System.Reflection.Assembly CompileMigrationAssembly(string migrationCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(migrationCode);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: $"PortableTextSearch.ScaffoldedMigration.{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, emitResult.Diagnostics);
            throw new InvalidOperationException($"Failed to compile scaffolded migration:{Environment.NewLine}{diagnostics}");
        }

        stream.Position = 0;
        return System.Reflection.Assembly.Load(stream.ToArray());
    }

    private static void ExecuteMigrationOperations(DbContext context, IReadOnlyList<MigrationOperation> operations)
    {
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        var commands = sqlGenerator.Generate(operations, context.Model);
        var connection = context.Database.GetDbConnection();

        foreach (var migrationCommand in commands)
        {
            using var command = connection.CreateCommand();
            command.CommandText = migrationCommand.CommandText;
            command.ExecuteNonQuery();
        }
    }

    private static TContext CreateSqliteContext<TContext>()
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlite("Data Source=migration-automation-tests.db")
            .UsePortableTextSearch()
            .Options;

        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    private static TContext CreatePostgresContext<TContext>()
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql("Host=localhost;Database=portable_text_search;Username=test;Password=test")
            .UsePortableTextSearch()
            .Options;

        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    private sealed class SqliteWithoutTextSearchContext(DbContextOptions<SqliteWithoutTextSearchContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.ToTable("MessageRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.Property(x => x.Name).HasMaxLength(256);
            });
        }
    }

    private sealed class SqliteWithTextSearchContext(DbContextOptions<SqliteWithTextSearchContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
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

    private sealed class PostgresWithoutTextSearchContext(DbContextOptions<PostgresWithoutTextSearchContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.ToTable("MessageRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.Property(x => x.Name).HasMaxLength(256);
            });
        }
    }

    private sealed class PostgresWithTextSearchContext(DbContextOptions<PostgresWithTextSearchContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
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

    private sealed class SqliteWithoutTextSearchGuidKeyContext(DbContextOptions<SqliteWithoutTextSearchGuidKeyContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidKeyMessageRecipient>(builder =>
            {
                builder.ToTable("GuidKeyRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).HasMaxLength(256);
            });
        }
    }

    private sealed class SqliteWithTextSearchGuidKeyContext(DbContextOptions<SqliteWithTextSearchGuidKeyContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidKeyMessageRecipient>(builder =>
            {
                builder.ToTable("GuidKeyRecipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.HasTextSearch(x => x.Email);
            });
        }
    }

    private sealed class GuidKeyMessageRecipient
    {
        public Guid Id { get; init; }

        public string? Email { get; init; }
    }
}
