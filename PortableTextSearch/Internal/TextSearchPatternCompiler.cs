using PortableTextSearch;

namespace PortableTextSearch.Internal;

internal static class TextSearchPatternCompiler
{
    public static TextSearchInput Parse(string? value, TextSearchMode mode)
    {
        if (value is null)
        {
            return TextSearchInput.Null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return TextSearchInput.Empty;
        }

        if (mode == TextSearchMode.Phrase)
        {
            return new TextSearchInput(false, false, [trimmed]);
        }

        var terms = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return terms.Length == 0
            ? TextSearchInput.Empty
            : new TextSearchInput(false, false, terms);
    }

    public static string? CompileSqliteMatch(string? value, TextSearchMode mode)
    {
        var parsed = Parse(value, mode);
        if (parsed.IsNull)
        {
            return null;
        }

        if (parsed.IsEmpty)
        {
            return string.Empty;
        }

        if (mode == TextSearchMode.Phrase)
        {
            return QuoteSqliteToken(parsed.Terms[0]);
        }

        var op = mode == TextSearchMode.AllTerms ? " AND " : " OR ";
        return string.Join(op, parsed.Terms.Select(QuoteSqliteToken));
    }

    private static string QuoteSqliteToken(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    internal readonly record struct TextSearchInput(bool IsNull, bool IsEmpty, IReadOnlyList<string> Terms)
    {
        public static TextSearchInput Null { get; } = new(true, false, Array.Empty<string>());

        public static TextSearchInput Empty { get; } = new(false, true, Array.Empty<string>());
    }
}
