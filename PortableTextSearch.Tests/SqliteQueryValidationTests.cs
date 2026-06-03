using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Configuration;
using PortableTextSearch.Functions;
using PortableTextSearch.Query;

namespace PortableTextSearch.Tests;

public sealed class SqliteQueryValidationTests
{
    [Fact]
    public void TextContains_throws_for_unconfigured_sqlite_fields()
    {
        var action = () =>
        {
            var options = new DbContextOptionsBuilder<UnconfiguredSqliteContext>()
                .UseSqlite("Data Source=unconfigured-sqlite.db")
                .UsePortableTextSearch()
                .Options;

            using var context = new UnconfiguredSqliteContext(options);
            _ = context.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToQueryString();
        };

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*HasTextSearch*");
    }

    [Fact]
    public void TextContains_throws_clear_error_for_composite_primary_keys_on_sqlite()
    {
        var action = () =>
        {
            var options = new DbContextOptionsBuilder<CompositeKeySqliteContext>()
                .UseSqlite("Data Source=composite-key-sqlite.db")
                .UsePortableTextSearch()
                .Options;

            using var context = new CompositeKeySqliteContext(options);
            _ = context.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToQueryString();
        };

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*single-column primary key*");
    }

    private sealed class UnconfiguredSqliteContext(DbContextOptions<UnconfiguredSqliteContext> options) : DbContext(options)
    {
        public DbSet<UnconfiguredRecipient> Recipients => Set<UnconfiguredRecipient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnconfiguredRecipient>(builder =>
            {
                builder.ToTable("Recipients");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).HasMaxLength(256);
            });
        }
    }

    private sealed class UnconfiguredRecipient
    {
        public int Id { get; init; }

        [System.ComponentModel.DataAnnotations.MaxLength(256)]
        public string? Email { get; init; }
    }

    private sealed class CompositeKeySqliteContext(DbContextOptions<CompositeKeySqliteContext> options) : DbContext(options)
    {
        public DbSet<CompositeKeyRecipient> Recipients => Set<CompositeKeyRecipient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompositeKeyRecipient>(builder =>
            {
                builder.ToTable("CompositeRecipients");
                builder.HasKey(x => new { x.Id, x.Partition });
                builder.Property(x => x.Partition).HasMaxLength(64);
                builder.Property(x => x.Email).HasMaxLength(256);
                builder.HasTextSearch(x => x.Email);
            });
        }
    }

    private sealed class CompositeKeyRecipient
    {
        public Guid Id { get; init; }

        [System.ComponentModel.DataAnnotations.MaxLength(64)]
        public string Partition { get; init; } = null!;

        [System.ComponentModel.DataAnnotations.MaxLength(256)]
        public string? Email { get; init; }
    }
}
