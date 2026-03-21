using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PortableTextSearch.Internal;

internal static class PropertyExpressionValidator
{
    public static IMutableProperty GetMappedStringProperty<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        LambdaExpression propertyExpression)
        where TEntity : class
    {
        if (Unwrap(propertyExpression.Body) is not MemberExpression memberExpression || memberExpression.Expression != propertyExpression.Parameters[0])
        {
            throw new ArgumentException(
                $"Expression '{propertyExpression}' must be a simple property access like 'x => x.Email'.",
                nameof(propertyExpression));
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new ArgumentException(
                $"Expression '{propertyExpression}' must reference a property.",
                nameof(propertyExpression));
        }

        if (propertyInfo.PropertyType != typeof(string))
        {
            throw new ArgumentException(
                $"Property '{propertyInfo.Name}' on entity '{typeof(TEntity).Name}' must be of type string or string?.",
                nameof(propertyExpression));
        }

        var property = builder.Metadata.FindProperty(propertyInfo);
        if (property is null)
        {
            throw new ArgumentException(
                $"Property '{propertyInfo.Name}' on entity '{typeof(TEntity).Name}' must be mapped by EF Core before it can be configured for text search.",
                nameof(propertyExpression));
        }

        return property;
    }

    private static Expression Unwrap(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unaryExpression
            ? Unwrap(unaryExpression.Operand)
            : expression;
}
