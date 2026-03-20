using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace PortableTextSearch.Query;

internal static class PostgreSqlTextContainsTranslator
{
    public static SqlExpression? Translate(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource,
        SqlExpression field,
        SqlExpression value)
    {
        if (sqlExpressionFactory is not NpgsqlSqlExpressionFactory npgsqlSqlExpressionFactory)
        {
            return null;
        }

        var stringMapping = field.TypeMapping ?? value.TypeMapping ?? typeMappingSource.FindMapping(typeof(string));
        var percent = sqlExpressionFactory.Constant("%", stringMapping);
        var pattern = sqlExpressionFactory.Add(
            sqlExpressionFactory.Add(percent, sqlExpressionFactory.ApplyTypeMapping(value, stringMapping), stringMapping),
            percent,
            stringMapping);

        return npgsqlSqlExpressionFactory.ILike(
            sqlExpressionFactory.ApplyTypeMapping(field, stringMapping),
            pattern);
    }
}
