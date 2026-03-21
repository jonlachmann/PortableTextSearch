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
        var visitedMatch = visitor.Visit(Match);
        var visitedPattern = visitor.Visit(Pattern);
        ArgumentNullException.ThrowIfNull(visitedMatch);
        ArgumentNullException.ThrowIfNull(visitedPattern);
        var match = (SqlExpression)visitedMatch;
        var pattern = (SqlExpression)visitedPattern;

        return !ReferenceEquals(match, Match) || !ReferenceEquals(pattern, Pattern)
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
