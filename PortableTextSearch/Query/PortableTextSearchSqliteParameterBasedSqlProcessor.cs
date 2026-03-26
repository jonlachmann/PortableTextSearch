using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using PortableTextSearch;

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
    public override Expression Process(
        Expression queryExpression,
        ParametersCacheDecorator parametersValues)
    {
        var visitor = new SqliteMatchCompilingVisitor(parametersValues);
        return base.Process(visitor.Visit(queryExpression), parametersValues);
    }
#else
    public override Expression Optimize(
        Expression queryExpression,
        IReadOnlyDictionary<string, object?> parameterValues,
        out bool canCache)
    {
        var visitor = new SqliteMatchCompilingVisitor(parameterValues);
        var optimized = base.Optimize(visitor.Visit(queryExpression), parameterValues, out canCache);
        if (visitor.UsedParameterValues)
        {
            canCache = false;
        }

        return optimized;
    }
#endif

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
            return new SqliteMatchExpression(match, pattern, matchExpression.Mode, matchExpression.TypeMapping!);
        }
    }

    private sealed class SqliteMatchCompilingVisitor : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<string, object?>? _parameterValues;
#if PORTABLETEXTSEARCH_EF10
        private readonly ParametersCacheDecorator? _parametersCache;
#endif

        public SqliteMatchCompilingVisitor(IReadOnlyDictionary<string, object?> parameterValues)
        {
            _parameterValues = parameterValues;
        }

#if PORTABLETEXTSEARCH_EF10
        public SqliteMatchCompilingVisitor(ParametersCacheDecorator parametersCache)
        {
            _parametersCache = parametersCache;
        }
#endif

        public bool UsedParameterValues { get; private set; }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is not SqliteMatchExpression matchExpression)
            {
                return base.VisitExtension(node);
            }

            var rewrittenMatch = (SqlExpression)Visit(matchExpression.Match);
            var parsed = ResolvePattern(matchExpression.Pattern, matchExpression.Mode);
            if (!parsed.IsResolved)
            {
                return new SqliteMatchExpression(rewrittenMatch, matchExpression.Pattern, matchExpression.Mode, matchExpression.TypeMapping!);
            }

            if (parsed.IsEmpty)
            {
                return CreateConstant(false, matchExpression.TypeMapping);
            }

            var rewrittenPattern = CreateConstant(parsed.CompiledValue, matchExpression.Pattern.TypeMapping);

            return new SqliteMatchExpression(rewrittenMatch, rewrittenPattern, matchExpression.Mode, matchExpression.TypeMapping!);
        }

        private CompiledPattern ResolvePattern(SqlExpression pattern, TextSearchMode mode)
            => pattern switch
            {
                SqlConstantExpression constant => CreateCompiledPattern((string?)constant.Value, mode, usedParameterValue: false),
                SqlParameterExpression parameter when TryGetParameterValue(parameter.Name, out var value) => CreateCompiledPattern(value as string, mode, usedParameterValue: true),
                _ => CompiledPattern.Unresolved
            };

        private CompiledPattern CreateCompiledPattern(string? value, TextSearchMode mode, bool usedParameterValue)
        {
            if (usedParameterValue)
            {
                UsedParameterValues = true;
            }

            var parsed = Internal.TextSearchPatternCompiler.Parse(value, mode);
            return new CompiledPattern(
                true,
                parsed.IsEmpty,
                parsed.IsEmpty ? null : Internal.TextSearchPatternCompiler.CompileSqliteMatch(value, mode));
        }

        private readonly record struct CompiledPattern(bool IsResolved, bool IsEmpty, string? CompiledValue)
        {
            public static CompiledPattern Unresolved { get; } = new(false, false, null);
        }

        private static SqlConstantExpression CreateConstant(object? value, RelationalTypeMapping? typeMapping)
#if PORTABLETEXTSEARCH_EF8
            => new(Expression.Constant(value, value?.GetType() ?? typeof(object)), typeMapping);
#else
            => new(value, value?.GetType() ?? typeof(object), typeMapping);
#endif

        private bool TryGetParameterValue(string name, out object? value)
        {
            var parameterValues = _parameterValues;
#if PORTABLETEXTSEARCH_EF10
            parameterValues ??= _parametersCache?.GetAndDisableCaching();
#endif
            if (parameterValues is not null && parameterValues.TryGetValue(name, out value))
            {
                return true;
            }

            value = null;
            return false;
        }
    }
}
