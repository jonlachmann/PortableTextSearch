using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchSqliteParameterBasedSqlProcessorFactory(
    RelationalParameterBasedSqlProcessorDependencies dependencies) : IRelationalParameterBasedSqlProcessorFactory
{
#if PORTABLETEXTSEARCH_EF8
    public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
        => new PortableTextSearchSqliteParameterBasedSqlProcessor(dependencies, useRelationalNulls);
#else
    public RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
        => new PortableTextSearchSqliteParameterBasedSqlProcessor(dependencies, parameters);
#endif
}

internal sealed class PortableTextSearchSqliteParameterBasedSqlProcessor(
    RelationalParameterBasedSqlProcessorDependencies dependencies,
#if PORTABLETEXTSEARCH_EF8
    bool useRelationalNulls) : RelationalParameterBasedSqlProcessor(dependencies, useRelationalNulls)
#else
    RelationalParameterBasedSqlProcessorParameters parameters) : RelationalParameterBasedSqlProcessor(dependencies, parameters)
#endif
{
#if PORTABLETEXTSEARCH_EF10
    protected override Expression ProcessSqlNullability(
        Expression queryExpression,
        ParametersCacheDecorator parametersValues)
        => new PortableTextSearchSqliteSqlNullabilityProcessor(Dependencies, Parameters)
            .Process(queryExpression, parametersValues);
#else
    protected override Expression ProcessSqlNullability(
        Expression queryExpression,
        IReadOnlyDictionary<string, object?> parameterValues,
        out bool canCache)
        => new PortableTextSearchSqliteSqlNullabilityProcessor(
                Dependencies,
#if PORTABLETEXTSEARCH_EF8
                UseRelationalNulls
#else
                Parameters
#endif
            )
            .Process(queryExpression, parameterValues, out canCache);
#endif

    private sealed class PortableTextSearchSqliteSqlNullabilityProcessor(
        RelationalParameterBasedSqlProcessorDependencies dependencies,
#if PORTABLETEXTSEARCH_EF8
        bool useRelationalNulls) : SqlNullabilityProcessor(dependencies, useRelationalNulls)
#else
        RelationalParameterBasedSqlProcessorParameters parameters) : SqlNullabilityProcessor(dependencies, parameters)
#endif
    {
        protected override SqlExpression VisitCustomSqlExpression(
            SqlExpression sqlExpression,
            bool allowOptimizedExpansion,
            out bool nullable)
        {
            if (sqlExpression is not SqliteMatchExpression matchExpression)
            {
                return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
            }

            var match = Visit(matchExpression.Match, allowOptimizedExpansion, out var matchNullable);
            var pattern = Visit(matchExpression.Pattern, allowOptimizedExpansion, out var patternNullable);

            nullable = matchNullable || patternNullable;
            return new SqliteMatchExpression(match, pattern, matchExpression.TypeMapping!);
        }
    }
}
