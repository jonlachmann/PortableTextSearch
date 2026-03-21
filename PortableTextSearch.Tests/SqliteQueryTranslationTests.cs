using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Functions;
using PortableTextSearch.Query;
using PortableTextSearch.Tests.TestModel;

namespace PortableTextSearch.Tests;

public sealed class SqliteQueryTranslationTests
{
    [Fact]
    public void TextContains_translates_to_fts_key_subquery()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "alice"))
            .ToQueryString();

        sql.Should().Contain(" IN (");
        sql.Should().Contain("SELECT \"__pts_entity_key\", \"Email\", \"Name\" FROM \"MessageRecipients_TextSearch\"");
        sql.Should().Contain("\"Email\" MATCH 'alice'");
    }

    [Fact]
    public void TextContains_can_be_composed_with_and_or_logic()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x =>
                x.Type == 2 &&
                (EF.Functions.TextContains(x.Email, "alice") || EF.Functions.TextContains(x.Name, "alice")))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH 'alice'");
        sql.Should().Contain("\"Name\" MATCH 'alice'");
        sql.Should().Contain(" OR ");
        sql.Should().Contain("\"Type\" = 2");
    }

    [Fact]
    public void TextContainsAny_translates_to_an_or_of_fts_searches()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContainsAny("alice", x.Email, x.Name))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH 'alice'");
        sql.Should().Contain("\"Name\" MATCH 'alice'");
        sql.Should().Contain(" OR ");
    }

    [Fact]
    public void TextContainsAny_supports_six_fields()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContainsAny("alice", x.Email, x.Name, x.Email, x.Name, x.Email, x.Name))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH 'alice'");
        sql.Should().Contain("\"Name\" MATCH 'alice'");
        sql.Should().Contain(" OR ");
    }

    [Fact]
    public void TextContains_remains_server_translatable()
    {
        var action = () =>
        {
            using var context = CreateContext();
            _ = context.MessageRecipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToQueryString();
        };

        action.Should().NotThrow();
    }

    private static PortableTextSearchTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortableTextSearchTestContext>()
            .UseSqlite("Data Source=portable-text-search-tests.db")
            .UsePortableTextSearch()
            .Options;

        return new PortableTextSearchTestContext(options);
    }
}
