using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Configuration;

/// <summary>
/// Adds provider-neutral text-search metadata to EF Core entity mappings.
/// </summary>
public static class PortableTextSearchModelBuilderExtensions
{
    /// <summary>
    /// Marks a mapped string property as participating in portable text search.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpression">A simple mapped string property access such as <c>x => x.Email</c>.</param>
    /// <returns>The same entity type builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="propertyExpression"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the expression does not reference a mapped string property.</exception>
    public static EntityTypeBuilder<TEntity> HasTextSearch<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, string?>> propertyExpression)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        return builder.HasTextSearch((LambdaExpression)propertyExpression);
    }

    /// <summary>
    /// Marks a mapped property as participating in portable text search and validates that it is a string property.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpression">A simple mapped property access.</param>
    /// <returns>The same entity type builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="propertyExpression"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the expression does not reference a mapped string property.</exception>
    public static EntityTypeBuilder<TEntity> HasTextSearch<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> propertyExpression)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        return builder.HasTextSearch((LambdaExpression)propertyExpression);
    }

    /// <summary>
    /// Marks multiple mapped properties as participating in portable text search and validates that each is a string property.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpressions">Simple mapped property accesses such as <c>x => x.Email</c> and <c>x => x.Name</c>.</param>
    /// <returns>The same entity type builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="propertyExpressions"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when any expression does not reference a mapped string property.</exception>
    public static EntityTypeBuilder<TEntity> HasTextSearch<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        params Expression<Func<TEntity, object?>>[] propertyExpressions)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpressions);

        foreach (var propertyExpression in propertyExpressions)
        {
            ArgumentNullException.ThrowIfNull(propertyExpression);
            builder.HasTextSearch((LambdaExpression)propertyExpression);
        }

        return builder;
    }

    private static EntityTypeBuilder<TEntity> HasTextSearch<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        LambdaExpression propertyExpression)
        where TEntity : class
    {
        var property = PropertyExpressionValidator.GetMappedStringProperty(builder, propertyExpression);
        builder.Metadata.AddTextSearchProperty(property.Name);
        return builder;
    }
}
