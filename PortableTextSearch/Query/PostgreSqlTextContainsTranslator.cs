using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using PortableTextSearch;

namespace PortableTextSearch.Query;

internal static class PostgreSqlTextContainsTranslator
{
    public static SqlExpression? Translate(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource,
        SqlExpression field,
        SqlExpression value,
        SqlExpression mode)
    {
        if (mode is not SqlConstantExpression { Value: TextSearchMode textSearchMode })
        {
            return null;
        }

        return value is SqlConstantExpression constant
            ? BuildPredicate(sqlExpressionFactory, typeMappingSource, field, constant.Value as string, textSearchMode)
            : BuildILike(sqlExpressionFactory, typeMappingSource, field, value);
    }

    public static SqlExpression? BuildPredicate(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource,
        SqlExpression field,
        string? value,
        TextSearchMode mode)
    {
        var parsed = Internal.TextSearchPatternCompiler.Parse(value, mode);
        var stringMapping = field.TypeMapping ?? typeMappingSource.FindMapping(typeof(string));
        var boolMapping = typeMappingSource.FindMapping(typeof(bool))!;

        if (parsed.IsEmpty)
        {
            return sqlExpressionFactory.Constant(false, boolMapping);
        }

        if (parsed.IsNull)
        {
            return BuildILike(
                sqlExpressionFactory,
                typeMappingSource,
                field,
                sqlExpressionFactory.Constant(null, typeof(string), stringMapping));
        }

        return parsed.Terms
            .Select(term => BuildILike(
                sqlExpressionFactory,
                typeMappingSource,
                field,
                sqlExpressionFactory.Constant(term, stringMapping)))
            .Aggregate((left, right) => mode == TextSearchMode.AllTerms
                ? sqlExpressionFactory.AndAlso(left!, right!)
                : sqlExpressionFactory.OrElse(left!, right!));
    }

    private static SqlExpression? BuildILike(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource,
        SqlExpression field,
        SqlExpression value)
    {
        if (sqlExpressionFactory is not Npgsql.EntityFrameworkCore.PostgreSQL.Query.NpgsqlSqlExpressionFactory npgsqlSqlExpressionFactory)
        {
            return null;
        }

        var stringMapping = field.TypeMapping ?? value.TypeMapping ?? typeMappingSource.FindMapping(typeof(string));

        // Use format() to build the pattern so PostgreSQL sees a single parameter
        // and can use GIN trigram indexes. SQL concatenation ('% '|| @p || '%')
        // prevents the query planner from choosing the index.
        var pattern = sqlExpressionFactory.Function(
            "format",
            [
                sqlExpressionFactory.Constant("%%%s%%", stringMapping),
                sqlExpressionFactory.ApplyTypeMapping(value, stringMapping)
            ],
            nullable: true,
            argumentsPropagateNullability: [false, true],
            typeof(string),
            stringMapping);

        return npgsqlSqlExpressionFactory.ILike(
            sqlExpressionFactory.ApplyTypeMapping(field, stringMapping),
            pattern);
    }
}
