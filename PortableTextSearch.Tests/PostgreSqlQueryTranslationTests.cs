using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Functions;
using PortableTextSearch.Query;
using PortableTextSearch.Tests.TestModel;

namespace PortableTextSearch.Tests;

public sealed class PostgreSqlQueryTranslationTests
{
    [Fact]
    public void TextContains_translates_to_ilike_pattern()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "alice"))
            .ToQueryString();

        sql.Should().Contain("ILIKE");
        sql.Should().Contain("%");
        sql.Should().Contain("\"Email\"");
    }

    [Fact]
    public void TextContains_can_be_composed_with_or_conditions()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x =>
                EF.Functions.TextContains(x.Email, "alice") ||
                EF.Functions.TextContains(x.Name, "alice"))
            .ToQueryString();

        sql.Should().Contain("ILIKE");
        sql.Should().Contain(" OR ");
        sql.Should().Contain("\"Email\"");
        sql.Should().Contain("\"Name\"");
    }

    [Fact]
    public void TextContainsAny_translates_to_or_conditions()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContainsAny("alice", x.Email, x.Name))
            .ToQueryString();

        sql.Should().Contain("ILIKE");
        sql.Should().Contain(" OR ");
        sql.Should().Contain("\"Email\"");
        sql.Should().Contain("\"Name\"");
    }

    [Fact]
    public void TextContainsAny_supports_six_fields()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContainsAny("alice", x.Email, x.Name, x.Email, x.Name, x.Email, x.Name))
            .ToQueryString();

        sql.Should().Contain("ILIKE");
        sql.Should().Contain(" OR ");
        sql.Should().Contain("\"Email\"");
        sql.Should().Contain("\"Name\"");
    }

    [Fact]
    public void TextContains_can_be_nested_with_other_predicates()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => x.Type == 1 && EF.Functions.TextContains(x.Email, "alice"))
            .ToQueryString();

        sql.Should().Contain("WHERE");
        sql.Should().Contain("\"Type\" = 1");
        sql.Should().Contain("ILIKE");
    }

    [Fact]
    public void TextContains_preserves_null_safe_sql_shape()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, null))
            .ToQueryString();

        sql.Should().Contain("ILIKE");
        sql.Should().NotContain("client");
    }

    private static PortableTextSearchTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortableTextSearchTestContext>()
            .UseNpgsql("Host=localhost;Database=portable_text_search_tests;Username=test;Password=test")
            .UsePortableTextSearch()
            .Options;

        return new PortableTextSearchTestContext(options);
    }
}
