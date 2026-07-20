namespace uCodeFirst.Attributes;

/// <summary>
/// Marks an enum member as a code-first Umbraco language. The declaring enum must carry
/// <see cref="LanguagesAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class LanguageAttribute : Attribute
{
    public LanguageAttribute(string IsoCode)
    {
        this.IsoCode = IsoCode;
    }

    /// <summary>ISO code of the language, e.g. "en-US".</summary>
    public string IsoCode { get; }

    /// <summary>
    /// The enum member (of the same enum) this language falls back to. Must itself carry
    /// <see cref="LanguageAttribute"/>. Leave unset for no fallback.
    /// </summary>
    public object? Fallback { get; set; }

    /// <summary>
    /// Whether a multi-lingual document must be published in this language before it can be
    /// published at all. Defaults to <see langword="false"/>.
    /// </summary>
    public bool IsMandatory { get; set; }
}
