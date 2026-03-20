using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchSqliteQuerySqlGeneratorFactory(
    QuerySqlGeneratorDependencies dependencies) : IQuerySqlGeneratorFactory
{
    public QuerySqlGenerator Create() => new PortableTextSearchSqliteQuerySqlGenerator(dependencies);
}

internal sealed class PortableTextSearchSqliteQuerySqlGenerator(
    QuerySqlGeneratorDependencies dependencies) : QuerySqlGenerator(dependencies)
{
    protected override void GenerateLimitOffset(SelectExpression selectExpression)
    {
        if (selectExpression.Limit is null && selectExpression.Offset is null)
        {
            return;
        }

        Sql.AppendLine()
            .Append("LIMIT ");

        if (selectExpression.Limit is not null)
        {
            Visit(selectExpression.Limit);
        }
        else
        {
            Sql.Append("-1");
        }

        if (selectExpression.Offset is not null)
        {
            Sql.Append(" OFFSET ");
            Visit(selectExpression.Offset);
        }
    }

    protected override Expression VisitExtension(Expression extensionExpression)
    {
        if (extensionExpression is not SqliteMatchExpression matchExpression)
        {
            return base.VisitExtension(extensionExpression);
        }

        Visit(matchExpression.Match);
        Sql.Append(" MATCH ");
        Visit(matchExpression.Pattern);
        return matchExpression;
    }
}
