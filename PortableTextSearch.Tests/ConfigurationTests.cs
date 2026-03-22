using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PortableTextSearch.Configuration;
using PortableTextSearch.Query;
using PortableTextSearch.Tests.TestModel;

namespace PortableTextSearch.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void HasTextSearch_stores_metadata_for_a_single_field()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(MessageRecipient));
        var values = entityType!.GetTextSearchProperties();

        values.Should().Contain(["Email", "Name"]);
    }

    [Fact]
    public void HasTextSearch_supports_multiple_fields_without_duplicates()
    {
        var options = new DbContextOptionsBuilder<DuplicateFieldContext>()
            .UseSqlite("Data Source=duplicate-fields.db")
            .UsePortableTextSearch()
            .Options;

        using var context = new DuplicateFieldContext(options);
        var entityType = context.Model.FindEntityType(typeof(MessageRecipient));

        entityType!.GetTextSearchProperties().Should().Equal("Email");
    }

    [Fact]
    public void HasTextSearch_supports_multiple_fields_in_a_single_call()
    {
        var options = new DbContextOptionsBuilder<MultiFieldContext>()
            .UseSqlite("Data Source=multi-fields.db")
            .UsePortableTextSearch()
            .Options;

        using var context = new MultiFieldContext(options);
        var entityType = context.Model.FindEntityType(typeof(MessageRecipient));

        entityType!.GetTextSearchProperties().Should().Equal("Email", "Name");
    }

    [Fact]
    public void HasTextSearch_stores_deterministic_annotation_values()
    {
        using var firstContext = CreateSqliteContext();
        using var secondContext = CreateSqliteContext();

        var differ = firstContext.GetService<IMigrationsModelDiffer>();
        var differences = differ.GetDifferences(
            firstContext.GetService<IDesignTimeModel>().Model.GetRelationalModel(),
            secondContext.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        differences.Should().BeEmpty();
    }

    [Fact]
    public void HasTextSearch_rejects_complex_expressions()
    {
        var action = () => BuildModel<InvalidExpressionContext>(
            builder => builder.UseSqlite("Data Source=invalid-expression.db"));

        action.Should()
            .Throw<ArgumentException>()
            .WithMessage("*simple property access*");
    }

    [Fact]
    public void HasTextSearch_rejects_non_string_properties()
    {
        var action = () => BuildModel<NonStringPropertyContext>(
            builder => builder.UseSqlite("Data Source=non-string.db"));

        action.Should()
            .Throw<ArgumentException>()
            .WithMessage("*must be of type string*");
    }

    [Fact]
    public void HasTextSearch_rejects_invalid_expressions_in_multi_field_calls()
    {
        var action = () => BuildModel<InvalidMultiFieldContext>(
            builder => builder.UseSqlite("Data Source=invalid-multi-field.db"));

        action.Should()
            .Throw<ArgumentException>()
            .WithMessage("*simple property access*");
    }

    private static PortableTextSearchTestContext CreateSqliteContext()
        => BuildContext<PortableTextSearchTestContext>(builder => builder.UseSqlite("Data Source=metadata.db"));

    private static void BuildModel<TContext>(Action<DbContextOptionsBuilder<TContext>> configure)
        where TContext : DbContext
    {
        using var context = BuildContext(configure);
        _ = context.Model;
    }

    private static TContext BuildContext<TContext>(
        Action<DbContextOptionsBuilder<TContext>> configure)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        configure(builder);
        builder.UsePortableTextSearch();
        return (TContext)Activator.CreateInstance(typeof(TContext), builder.Options)!;
    }

    private sealed class DuplicateFieldContext(DbContextOptions<DuplicateFieldContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasTextSearch(x => x.Email)
                    .HasTextSearch(x => x.Email);
            });
        }
    }

    private sealed class MultiFieldContext(DbContextOptions<MultiFieldContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasTextSearch(x => x.Email, x => x.Name);
            });
        }
    }

    private sealed class InvalidExpressionContext(DbContextOptions<InvalidExpressionContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasTextSearch(x => x.Email == null ? null : x.Email.Trim());
            });
        }
    }

    private sealed class NonStringPropertyContext(DbContextOptions<NonStringPropertyContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasTextSearch(x => x.Type);
            });
        }
    }

    private sealed class InvalidMultiFieldContext(DbContextOptions<InvalidMultiFieldContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageRecipient>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasTextSearch(x => x.Email, x => x.Name == null ? null : x.Name.Trim());
            });
        }
    }
}
