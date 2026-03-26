namespace PortableTextSearch;

/// <summary>
/// Controls how input text is parsed into portable search terms.
/// </summary>
public enum TextSearchMode
{
    /// <summary>
    /// Split on whitespace and match if any term is present.
    /// </summary>
    AnyTerms = 0,

    /// <summary>
    /// Split on whitespace and require every term to be present.
    /// </summary>
    AllTerms = 1,

    /// <summary>
    /// Treat the trimmed input as a single phrase.
    /// </summary>
    Phrase = 2
}
