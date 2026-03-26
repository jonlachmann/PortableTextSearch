using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch;
using PortableTextSearch.Functions;

namespace PortableTextSearch.Tests;

public sealed class DbFunctionApiTests
{
    [Fact]
    public void TextContains_throws_when_called_outside_of_query_translation()
    {
        var action = () => EF.Functions.TextContains("alice@example.com", "alice");

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*only be used inside LINQ queries*");
    }

    [Fact]
    public void TextContains_supports_explicit_mode_argument()
    {
        var action = () => EF.Functions.TextContains("alice@example.com", "alice bob", TextSearchMode.AllTerms);

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*only be used inside LINQ queries*");
    }

    [Fact]
    public void TextContainsAny_supports_trailing_mode_argument()
    {
        var action = () => EF.Functions.TextContainsAny("alice bob", "alice@example.com", "Alice", TextSearchMode.Phrase);

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*only be used inside LINQ queries*");
    }
}
