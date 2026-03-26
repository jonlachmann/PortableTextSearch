using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch;
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
        sql.Should().Contain("\"Email\" MATCH '\"alice\"'");
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

        sql.Should().Contain("\"Email\" MATCH '\"alice\"'");
        sql.Should().Contain("\"Name\" MATCH '\"alice\"'");
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

        sql.Should().Contain("\"Email\" MATCH '\"alice\"'");
        sql.Should().Contain("\"Name\" MATCH '\"alice\"'");
        sql.Should().Contain(" OR ");
    }

    [Fact]
    public void TextContainsAny_supports_thirty_two_fields()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContainsAny(
                "alice",
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name,
                x.Email,
                x.Name))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH '\"alice\"'");
        sql.Should().Contain("\"Name\" MATCH '\"alice\"'");
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

    [Fact]
    public void TextContains_defaults_to_any_terms_with_whitespace_only_splitting()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "draft query-1774300743237-8a756b7e"))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH '\"draft\" OR \"query-1774300743237-8a756b7e\"'");
    }

    [Fact]
    public void TextContains_supports_all_terms_mode()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "draft query-1774300743237-8a756b7e", TextSearchMode.AllTerms))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH '\"draft\" AND \"query-1774300743237-8a756b7e\"'");
    }

    [Fact]
    public void TextContains_supports_phrase_mode()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "draft query-1774300743237-8a756b7e", TextSearchMode.Phrase))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH '\"draft query-1774300743237-8a756b7e\"'");
    }

    [Fact]
    public void TextContains_escapes_embedded_quotes_for_sqlite_fts()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "say \"hello\"", TextSearchMode.Phrase))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH '\"say \"\"hello\"\"\"'");
    }

    [Fact]
    public void TextContains_treats_punctuation_inside_terms_as_data()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "alpha, beta"))
            .ToQueryString();

        sql.Should().Contain("\"Email\" MATCH '\"alpha,\" OR \"beta\"'");
    }

    [Fact]
    public void TextContains_whitespace_only_input_short_circuits_without_match()
    {
        using var context = CreateContext();

        var sql = context.MessageRecipients
            .Where(x => EF.Functions.TextContains(x.Email, "   "))
            .ToQueryString();

        sql.Should().NotContain(" MATCH ");
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
