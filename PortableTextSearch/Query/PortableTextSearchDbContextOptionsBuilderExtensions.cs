using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PortableTextSearch.Query;

/// <summary>
/// Registers PortableTextSearch query translation services on a DbContext options builder.
/// </summary>
public static class PortableTextSearchDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables PortableTextSearch query translation services for the current context.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>The same options builder for chaining.</returns>
    public static DbContextOptionsBuilder UsePortableTextSearch(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        var extension = optionsBuilder.Options.FindExtension<PortableTextSearchOptionsExtension>()
            ?? new PortableTextSearchOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }

    /// <summary>
    /// Enables PortableTextSearch query translation services for the current context.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>The same options builder for chaining.</returns>
    public static DbContextOptionsBuilder<TContext> UsePortableTextSearch<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        UsePortableTextSearch((DbContextOptionsBuilder)optionsBuilder);
        return optionsBuilder;
    }
}
