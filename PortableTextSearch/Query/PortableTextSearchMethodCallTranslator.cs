using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using PortableTextSearch;
using PortableTextSearch.Functions;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchMethodCallTranslator : IMethodCallTranslator
{
    private static readonly HashSet<MethodInfo> TextContainsMethods = typeof(PortableTextSearchDbFunctionsExtensions)
        .GetRuntimeMethods()
        .Where(method =>
            method.Name == nameof(PortableTextSearchDbFunctionsExtensions.TextContains) &&
            method.ReturnType == typeof(bool) &&
            method.GetParameters() is [var dbFunctions, var field, var value, ..] &&
            dbFunctions.ParameterType == typeof(DbFunctions) &&
            field.ParameterType == typeof(string) &&
            value.ParameterType == typeof(string))
        .ToHashSet();
    private static readonly HashSet<MethodInfo> TextContainsAnyMethods = typeof(PortableTextSearchDbFunctionsExtensions)
        .GetRuntimeMethods()
        .Where(method =>
            method.Name == nameof(PortableTextSearchDbFunctionsExtensions.TextContainsAny) &&
            method.ReturnType == typeof(bool) &&
            method.GetParameters() is [var dbFunctions, var value, ..] &&
            dbFunctions.ParameterType == typeof(DbFunctions) &&
            value.ParameterType == typeof(string))
        .ToHashSet();

    private readonly IDatabaseProvider _databaseProvider;
    private readonly IModel _model;
    private readonly ISqlExpressionFactory _sqlExpressionFactory;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    public PortableTextSearchMethodCallTranslator(
        IDatabaseProvider databaseProvider,
        ICurrentDbContext currentDbContext,
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _databaseProvider = databaseProvider;
        _model = currentDbContext.Context.Model;
        _sqlExpressionFactory = sqlExpressionFactory;
        _typeMappingSource = typeMappingSource;
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (TextContainsMethods.Contains(method))
        {
            if (arguments.Count is not (3 or 4))
            {
                return null;
            }

            var singleMode = arguments.Count == 4 ? arguments[3] : GetDefaultModeExpression();
            return TranslateSingle(arguments[1], arguments[2], singleMode);
        }

        if (!TextContainsAnyMethods.Contains(method) || arguments.Count < 4)
        {
            return null;
        }

        var value = arguments[1];
        var hasMode = method.GetParameters()[^1].ParameterType == typeof(TextSearchMode);
        var anyMode = hasMode ? arguments[^1] : GetDefaultModeExpression();
        var fieldCount = hasMode ? arguments.Count - 3 : arguments.Count - 2;
        var fieldPredicates = arguments.Skip(2).Take(fieldCount)
            .Select(field => TranslateSingle(field, value, anyMode))
            .ToArray();

        return fieldPredicates.Aggregate(_sqlExpressionFactory.OrElse);
    }

    private SqlExpression TranslateSingle(SqlExpression field, SqlExpression value, SqlExpression mode)
        => _databaseProvider.Name switch
        {
            ProviderNames.Npgsql => PostgreSqlTextContainsTranslator.Translate(_sqlExpressionFactory, _typeMappingSource, field, value, mode),
            ProviderNames.Sqlite => SqliteTextContainsTranslator.Translate(_model, _sqlExpressionFactory, _typeMappingSource, field, value, mode),
            _ => null
        } ?? throw new InvalidOperationException("PortableTextSearch does not support the configured database provider.");

    private SqlExpression GetDefaultModeExpression()
        => _sqlExpressionFactory.Constant(TextSearchMode.AnyTerms);
}
