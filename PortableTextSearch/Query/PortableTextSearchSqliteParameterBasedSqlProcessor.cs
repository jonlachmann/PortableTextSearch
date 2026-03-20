using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchSqliteParameterBasedSqlProcessorFactory(
    RelationalParameterBasedSqlProcessorDependencies dependencies) : IRelationalParameterBasedSqlProcessorFactory
{
    public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
        => new PortableTextSearchSqliteParameterBasedSqlProcessor(dependencies, useRelationalNulls);
}

internal sealed class PortableTextSearchSqliteParameterBasedSqlProcessor(
    RelationalParameterBasedSqlProcessorDependencies dependencies,
    bool useRelationalNulls) : RelationalParameterBasedSqlProcessor(dependencies, useRelationalNulls)
{
    protected override Expression ProcessSqlNullability(
        Expression queryExpression,
        IReadOnlyDictionary<string, object?> parameterValues,
        out bool canCache)
        => new PortableTextSearchSqliteSqlNullabilityProcessor(Dependencies, UseRelationalNulls)
            .Process(queryExpression, parameterValues, out canCache);

    private sealed class PortableTextSearchSqliteSqlNullabilityProcessor(
        RelationalParameterBasedSqlProcessorDependencies dependencies,
        bool useRelationalNulls) : SqlNullabilityProcessor(dependencies, useRelationalNulls)
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
