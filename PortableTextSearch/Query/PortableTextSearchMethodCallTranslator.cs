using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using PortableTextSearch.Functions;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchMethodCallTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo TextContainsMethod = typeof(PortableTextSearchDbFunctionsExtensions)
        .GetRuntimeMethod(
            nameof(PortableTextSearchDbFunctionsExtensions.TextContains),
            [typeof(DbFunctions), typeof(string), typeof(string)])!;

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
        if (!Equals(method, TextContainsMethod))
        {
            return null;
        }

        if (arguments.Count != 3)
        {
            return null;
        }

        var field = arguments[1];
        var value = arguments[2];

        return _databaseProvider.Name switch
        {
            ProviderNames.Npgsql => PostgreSqlTextContainsTranslator.Translate(_sqlExpressionFactory, _typeMappingSource, field, value),
            ProviderNames.Sqlite => SqliteTextContainsTranslator.Translate(_model, _sqlExpressionFactory, _typeMappingSource, field, value),
            _ => null
        };
    }
}
