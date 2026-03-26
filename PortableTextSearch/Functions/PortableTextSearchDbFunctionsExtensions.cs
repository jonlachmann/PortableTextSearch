using Microsoft.EntityFrameworkCore;
using PortableTextSearch;

namespace PortableTextSearch.Functions;

/// <summary>
/// Exposes provider-translated text search helpers for use inside LINQ queries.
/// </summary>
public static class PortableTextSearchDbFunctionsExtensions
{
    private const string ServerTranslationOnlyMessage =
        "PortableTextSearchDbFunctionsExtensions methods can only be used inside LINQ queries translated by EF Core.";

    private static bool ThrowNotTranslatable()
        => throw new InvalidOperationException(ServerTranslationOnlyMessage);

    /// <summary>
    /// Tests whether a mapped text column contains the provided search value.
    /// </summary>
    /// <param name="_">The EF Core <see cref="DbFunctions" /> marker.</param>
    /// <param name="field">The mapped text field being searched.</param>
    /// <param name="value">The search text.</param>
    /// <returns>A boolean expression that EF Core translates server-side.</returns>
    /// <exception cref="InvalidOperationException">Thrown when evaluated outside EF Core query translation.</exception>
    /// <remarks>
    /// The <see cref="DbFunctionAttribute" /> is present primarily as a tooling hint. Runtime translation is
    /// provided by PortableTextSearch's provider-specific method translators rather than a database function named
    /// <c>text_contains</c>.
    /// </remarks>
    [DbFunction("text_contains")]
    public static bool TextContains(this DbFunctions _, string? field, string? value)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether a mapped text column contains the provided search value using the specified parsing mode.
    /// </summary>
    /// <param name="_">The EF Core <see cref="DbFunctions" /> marker.</param>
    /// <param name="field">The mapped text field being searched.</param>
    /// <param name="value">The search text.</param>
    /// <param name="mode">The portable parsing mode used for the search text.</param>
    /// <returns>A boolean expression that EF Core translates server-side.</returns>
    /// <exception cref="InvalidOperationException">Thrown when evaluated outside EF Core query translation.</exception>
    [DbFunction("text_contains")]
    public static bool TextContains(this DbFunctions _, string? field, string? value, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of two mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of two mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of three mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of three mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of four mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of four mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of five mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of five mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of six mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of six mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of seven mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of seven mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of eight mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of eight mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of nine mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of nine mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of ten mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of ten mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of eleven mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of eleven mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twelve mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twelve mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirteen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirteen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of fourteen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of fourteen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of fifteen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of fifteen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of sixteen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of sixteen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of seventeen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of seventeen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of eighteen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of eighteen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of nineteen mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of nineteen mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-one mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-one mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-two mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-two mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-three mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-three mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-four mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-four mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-five mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-five mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-six mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-six mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-seven mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-seven mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-eight mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-eight mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-nine mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of twenty-nine mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirty mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, string? field30)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirty mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, string? field30, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirty-one mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, string? field30, string? field31)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirty-one mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, string? field30, string? field31, TextSearchMode mode)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirty-two mapped text fields contains the provided search value.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, string? field30, string? field31, string? field32)
        => ThrowNotTranslatable();

    /// <summary>
    /// Tests whether any of thirty-two mapped text fields contains the provided search value using the specified parsing mode.
    /// </summary>
    [DbFunction("text_contains_any")]
    public static bool TextContainsAny(this DbFunctions _, string? value, string? field1, string? field2, string? field3, string? field4, string? field5, string? field6, string? field7, string? field8, string? field9, string? field10, string? field11, string? field12, string? field13, string? field14, string? field15, string? field16, string? field17, string? field18, string? field19, string? field20, string? field21, string? field22, string? field23, string? field24, string? field25, string? field26, string? field27, string? field28, string? field29, string? field30, string? field31, string? field32, TextSearchMode mode)
        => ThrowNotTranslatable();
}
