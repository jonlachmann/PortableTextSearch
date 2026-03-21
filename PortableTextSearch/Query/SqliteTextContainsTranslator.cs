using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using PortableTextSearch.Configuration;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Query;

internal static class SqliteTextContainsTranslator
{
    private static readonly ConstructorInfo SelectExpressionEntityTableConstructor =
        typeof(SelectExpression).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(IEntityType), typeof(TableExpressionBase)],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate internal SelectExpression(IEntityType, TableExpressionBase) constructor.");

    private static readonly ConstructorInfo SelectExpressionProjectionConstructor =
        typeof(SelectExpression).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(TableExpressionBase), typeof(string), typeof(Type), typeof(RelationalTypeMapping), typeof(bool?), typeof(string), typeof(Type), typeof(RelationalTypeMapping)],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate internal SelectExpression(TableExpressionBase, ...) constructor.");

    private static readonly MethodInfo CreateColumnExpressionMethod =
        typeof(SelectExpression).GetMethod(
            nameof(SelectExpression.CreateColumnExpression),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            [typeof(TableExpressionBase), typeof(string), typeof(Type), typeof(RelationalTypeMapping), typeof(bool?)],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate SelectExpression.CreateColumnExpression.");

    public static SqlExpression Translate(
        IModel model,
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource,
        SqlExpression field,
        SqlExpression value)
    {
        if (field is not ColumnExpression { Table: TableExpression tableExpression } fieldColumn)
        {
            throw new InvalidOperationException(
                "SQLite text search requires a direct mapped string column access configured with HasTextSearch(...).");
        }

        var searchInfo = ResolveFtsSearch(model, fieldColumn);
        var keyTypeMapping = typeMappingSource.FindMapping(searchInfo.KeyProperty)!;
        var outerSelect = (SelectExpression)SelectExpressionEntityTableConstructor.Invoke([searchInfo.EntityType, tableExpression]);
        var keyColumn = (ColumnExpression)CreateColumnExpressionMethod.Invoke(
            outerSelect,
            [
                tableExpression,
                searchInfo.KeyColumnName,
                searchInfo.KeyProperty.ClrType,
                keyTypeMapping,
                searchInfo.KeyProperty.IsNullable
            ])!;

        var projectedColumns = string.Join(
            ", ",
            new[] { "rowid" }.Concat(searchInfo.SearchColumnNames.Select(SqlIdentifier.Quote)));
        var fromSql = new FromSqlExpression(
            "pts_fts",
            $"SELECT {projectedColumns} FROM {SqlIdentifier.Quote(searchInfo.VirtualTableName)}",
            Expression.Constant(Array.Empty<object>()));
        var subquery = (SelectExpression)SelectExpressionProjectionConstructor.Invoke(
            [
                fromSql,
                "rowid",
                keyColumn.Type,
                keyColumn.TypeMapping!,
                false,
                "rowid",
                keyColumn.Type,
                keyColumn.TypeMapping!
            ]);
        var subqueryRowIdColumn = (ColumnExpression)CreateColumnExpressionMethod.Invoke(
            subquery,
            [
                fromSql,
                "rowid",
                keyColumn.Type,
                keyColumn.TypeMapping!,
                false
            ])!;
        var subqueryFieldColumn = (ColumnExpression)CreateColumnExpressionMethod.Invoke(
            subquery,
            [
                fromSql,
                fieldColumn.Name,
                fieldColumn.Type,
                fieldColumn.TypeMapping!,
                fieldColumn.IsNullable
            ])!;
        var stringTypeMapping = fieldColumn.TypeMapping ?? value.TypeMapping ?? typeMappingSource.FindMapping(typeof(string));
        subquery.AddToProjection(subqueryRowIdColumn);
        subquery.ApplyPredicate(new SqliteMatchExpression(
            subqueryFieldColumn.ApplyTypeMapping(stringTypeMapping!),
            sqlExpressionFactory.ApplyTypeMapping(value, stringTypeMapping),
            typeMappingSource.FindMapping(typeof(bool))!));

        return sqlExpressionFactory.In(keyColumn, subquery);
    }

    private static SqliteFtsSearchInfo ResolveFtsSearch(IModel model, ColumnExpression fieldColumn)
    {
        if (fieldColumn.Table is not TableExpression tableExpression)
        {
            throw new InvalidOperationException(
                "SQLite text search requires a direct mapped string column access configured with HasTextSearch(...).");
        }

        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();
            if (!string.Equals(tableName, tableExpression.Name, StringComparison.Ordinal)
                || !string.IsNullOrEmpty(schema))
            {
                continue;
            }

            var storeObject = StoreObjectIdentifier.Table(tableName!, schema);
            var property = entityType.GetProperties()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.GetColumnName(storeObject), fieldColumn.Name, StringComparison.Ordinal));

            if (property is null)
            {
                continue;
            }

            if (!entityType.GetTextSearchProperties().Contains(property.Name, StringComparer.Ordinal))
            {
                continue;
            }

            var keyProperty = entityType.FindPrimaryKey()?.Properties.SingleOrDefault();
            if (keyProperty is null)
            {
                continue;
            }

            var keyColumnName = keyProperty.GetColumnName(storeObject);
            if (string.IsNullOrWhiteSpace(keyColumnName))
            {
                continue;
            }

            return new SqliteFtsSearchInfo(
                entityType,
                SqliteTextSearchNaming.GetDefaultVirtualTableName(tableName!),
                entityType.GetTextSearchProperties()
                    .Select(propertyName => entityType.FindProperty(propertyName)!)
                    .Select(prop => prop.GetColumnName(storeObject)!)
                    .ToArray(),
                keyProperty,
                keyColumnName);
        }

        throw new InvalidOperationException(
            $"Column '{fieldColumn.Name}' on SQLite must be configured with HasTextSearch(...) before EF.Functions.TextContains can translate it.");
    }

    private readonly record struct SqliteFtsSearchInfo(
        IEntityType EntityType,
        string VirtualTableName,
        IReadOnlyList<string> SearchColumnNames,
        IProperty KeyProperty,
        string KeyColumnName);
}
