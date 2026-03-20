using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace PortableTextSearch.Query;

internal sealed class SqliteMatchExpression : SqlExpression
{
    public SqliteMatchExpression(SqlExpression match, SqlExpression pattern, RelationalTypeMapping typeMapping)
        : base(typeof(bool), typeMapping)
    {
        Match = match;
        Pattern = pattern;
    }

    public SqlExpression Match { get; }

    public SqlExpression Pattern { get; }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var match = (SqlExpression)visitor.Visit(Match)!;
        var pattern = (SqlExpression)visitor.Visit(Pattern)!;

        return match != Match || pattern != Pattern
            ? new SqliteMatchExpression(match, pattern, TypeMapping!)
            : this;
    }

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Visit(Match);
        expressionPrinter.Append(" MATCH ");
        expressionPrinter.Visit(Pattern);
    }
}
