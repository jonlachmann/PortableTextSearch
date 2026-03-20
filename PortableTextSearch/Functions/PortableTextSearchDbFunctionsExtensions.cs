using Microsoft.EntityFrameworkCore;

namespace PortableTextSearch.Functions;

/// <summary>
/// Exposes provider-translated text search helpers for use inside LINQ queries.
/// </summary>
public static class PortableTextSearchDbFunctionsExtensions
{
    /// <summary>
    /// Tests whether a mapped text column contains the provided search value.
    /// </summary>
    /// <param name="_">The EF Core <see cref="DbFunctions" /> marker.</param>
    /// <param name="field">The mapped text field being searched.</param>
    /// <param name="value">The search text.</param>
    /// <returns>A boolean expression that EF Core translates server-side.</returns>
    /// <exception cref="InvalidOperationException">Thrown when evaluated outside EF Core query translation.</exception>
    public static bool TextContains(this DbFunctions _, string? field, string? value)
        => throw new InvalidOperationException(
            "PortableTextSearchDbFunctionsExtensions.TextContains can only be used inside LINQ queries translated by EF Core.");
}
