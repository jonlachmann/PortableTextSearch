using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(256)]
        public string? Email { get; set; }
    }
}
