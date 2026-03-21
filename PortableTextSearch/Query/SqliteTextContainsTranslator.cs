using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using PortableTextSearch.Configuration;
using PortableTextSearch.Internal;

namespace PortableTextSearch.Query;

internal static class SqliteTextContainsTranslator
{
#if !PORTABLETEXTSEARCH_EF8
    private static readonly ConstructorInfo TableExpressionConstructor =
        typeof(TableExpression).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(ITableBase)],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate internal TableExpression(string, ITableBase) constructor.");
#endif

#if PORTABLETEXTSEARCH_EF8
    private static readonly ConstructorInfo SelectExpressionProjectionConstructor =
        typeof(SelectExpression).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(TableExpressionBase), typeof(string), typeof(Type), typeof(RelationalTypeMapping), typeof(bool?), typeof(string), typeof(Type), typeof(RelationalTypeMapping)],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate internal SelectExpression(TableExpressionBase, ...) constructor.");
#else
    private static readonly ConstructorInfo SelectExpressionConstructor =
        typeof(SelectExpression).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [
                typeof(string),
                typeof(List<TableExpressionBase>),
                typeof(SqlExpression),
                typeof(List<SqlExpression>),
                typeof(SqlExpression),
                typeof(List<ProjectionExpression>),
                typeof(bool),
                typeof(List<OrderingExpression>),
                typeof(SqlExpression),
                typeof(SqlExpression),
                typeof(ISet<string>),
                typeof(IReadOnlyDictionary<string, IAnnotation>),
                typeof(SqlAliasManager),
                typeof(bool)
            ],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate internal SelectExpression(alias, tables, ...) constructor.");

    private static readonly PropertyInfo IsMutableProperty =
        typeof(SelectExpression).GetProperty(
            "IsMutable",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Unable to locate SelectExpression.IsMutable.");
#endif

#if PORTABLETEXTSEARCH_EF8
    private static readonly MethodInfo CreateColumnExpressionMethod =
        typeof(SelectExpression).GetMethod(
            nameof(SelectExpression.CreateColumnExpression),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            [typeof(TableExpressionBase), typeof(string), typeof(Type), typeof(RelationalTypeMapping), typeof(bool?)],
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate SelectExpression.CreateColumnExpression.");
#endif

    public static SqlExpression Translate(
        IModel model,
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource,
        SqlExpression field,
        SqlExpression value)
    {
        if (field is not ColumnExpression fieldColumn)
        {
            throw new InvalidOperationException(
                "SQLite text search requires a direct mapped string column access configured with HasTextSearch(...).");
        }

        var searchInfo = ResolveFtsSearch(model, fieldColumn);
        var ftsAlias = GetFtsAlias(searchInfo, fieldColumn);
        var keyTypeMapping = typeMappingSource.FindMapping(searchInfo.KeyProperty)!;
        var keyColumn = CreateKeyColumn(searchInfo, fieldColumn, keyTypeMapping);

        var projectedColumns = string.Join(
            ", ",
            new[] { SqlIdentifier.Quote(SqliteTextSearchMigrationDefinition.EntityKeyColumnName) }
                .Concat(searchInfo.SearchColumnNames.Select(SqlIdentifier.Quote)));
        var fromSql = new FromSqlExpression(
            ftsAlias,
            $"SELECT {projectedColumns} FROM {SqlIdentifier.Quote(searchInfo.VirtualTableName)}",
            Expression.Constant(Array.Empty<object>()));
#if PORTABLETEXTSEARCH_EF8
        var subquery = (SelectExpression)SelectExpressionProjectionConstructor.Invoke(
            [
                fromSql,
                SqliteTextSearchMigrationDefinition.EntityKeyColumnName,
                keyColumn.Type,
                keyColumn.TypeMapping!,
                false,
                SqliteTextSearchMigrationDefinition.EntityKeyColumnName,
                keyColumn.Type,
                keyColumn.TypeMapping!
            ]);
        var subqueryKeyColumn = (ColumnExpression)CreateColumnExpressionMethod.Invoke(
            subquery,
            [
                fromSql,
                SqliteTextSearchMigrationDefinition.EntityKeyColumnName,
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
#else
        var subquery = (SelectExpression)SelectExpressionConstructor.Invoke(
            [
                null!,
                new List<TableExpressionBase> { fromSql },
                null!,
                new List<SqlExpression>(),
                null!,
                new List<ProjectionExpression>(),
                false,
                new List<OrderingExpression>(),
                null!,
                null!,
                new HashSet<string>(),
                null!,
                null!,
                true
            ]);
        var subqueryKeyColumn = new ColumnExpression(
            SqliteTextSearchMigrationDefinition.EntityKeyColumnName,
            ftsAlias,
            keyColumn.Type,
            keyColumn.TypeMapping!,
            nullable: false);
        var subqueryFieldColumn = new ColumnExpression(
            fieldColumn.Name,
            ftsAlias,
            fieldColumn.Type,
            fieldColumn.TypeMapping!,
            fieldColumn.IsNullable);
#endif
        var stringTypeMapping = fieldColumn.TypeMapping ?? value.TypeMapping ?? typeMappingSource.FindMapping(typeof(string));
        subquery.AddToProjection(subqueryKeyColumn);
        subquery.ApplyPredicate(new SqliteMatchExpression(
            subqueryFieldColumn.ApplyTypeMapping(stringTypeMapping!),
            sqlExpressionFactory.ApplyTypeMapping(value, stringTypeMapping),
            typeMappingSource.FindMapping(typeof(bool))!));
#if !PORTABLETEXTSEARCH_EF8
        subquery.ApplyProjection();
        IsMutableProperty.SetValue(subquery, false);
#endif

        return sqlExpressionFactory.In(keyColumn, subquery);
    }

    private static SqliteFtsSearchInfo ResolveFtsSearch(IModel model, ColumnExpression fieldColumn)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();
            if (tableName is null || !string.IsNullOrEmpty(schema))
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

    private static string GetFtsAlias(SqliteFtsSearchInfo searchInfo, ColumnExpression fieldColumn)
    {
        var index = searchInfo.SearchColumnNames
            .Select((columnName, i) => new { columnName, i })
            .FirstOrDefault(entry => string.Equals(entry.columnName, fieldColumn.Name, StringComparison.Ordinal))
            ?.i ?? 0;

        return $"t{index}";
    }

    private static ColumnExpression CreateKeyColumn(
        SqliteFtsSearchInfo searchInfo,
        ColumnExpression fieldColumn,
        RelationalTypeMapping keyTypeMapping)
    {
#if PORTABLETEXTSEARCH_EF8
        if (fieldColumn.Table is not TableExpression tableExpression)
        {
            throw new InvalidOperationException(
                "SQLite text search requires a direct mapped string column access configured with HasTextSearch(...).");
        }

        var outerSelect = (SelectExpression)SelectExpressionProjectionConstructor.Invoke(
            [
                tableExpression,
                searchInfo.KeyColumnName,
                searchInfo.KeyProperty.ClrType,
                keyTypeMapping,
                searchInfo.KeyProperty.IsNullable,
                searchInfo.KeyColumnName,
                searchInfo.KeyProperty.ClrType,
                keyTypeMapping
            ]);

        return (ColumnExpression)CreateColumnExpressionMethod.Invoke(
            outerSelect,
            [
                tableExpression,
                searchInfo.KeyColumnName,
                searchInfo.KeyProperty.ClrType,
                keyTypeMapping,
                searchInfo.KeyProperty.IsNullable
            ])!;
#else
        return new ColumnExpression(
            searchInfo.KeyColumnName,
            fieldColumn.TableAlias,
            searchInfo.KeyProperty.ClrType,
            keyTypeMapping,
            searchInfo.KeyProperty.IsNullable);
#endif
    }
}
