using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace PortableTextSearch.Query;

internal sealed class SqliteMatchExpression : SqlExpression
{
    private static readonly ConstructorInfo QuoteConstructor =
        typeof(SqliteMatchExpression).GetConstructor(
            [typeof(SqlExpression), typeof(SqlExpression), typeof(RelationalTypeMapping)])
        ?? throw new InvalidOperationException("Unable to locate SqliteMatchExpression quoting constructor.");

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

#if !PORTABLETEXTSEARCH_EF8
    public override Expression Quote()
        => Expression.New(
            QuoteConstructor,
            Expression.Constant(Match, typeof(SqlExpression)),
            Expression.Constant(Pattern, typeof(SqlExpression)),
            Expression.Constant(TypeMapping, typeof(RelationalTypeMapping)));
#endif
}
