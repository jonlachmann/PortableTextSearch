using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
}
