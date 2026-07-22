namespace uCodeFirst.Attributes;

/// <summary>
/// Marks an enum as the code-first Umbraco language set. Apply <see cref="LanguageAttribute"/> to
/// individual members to declare the languages that belong to it — members without it are ignored,
/// so the enum can also hold unrelated values. Only one enum across the scanned assemblies may
/// carry this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class LanguagesAttribute : Attribute
{
    /// <param name="DefaultLanguage">
    /// The enum member (of the enum this attribute is applied to) whose language becomes
    /// Umbraco's default language the first time it is created. Must carry <see cref="LanguageAttribute"/>.
    /// </param>
    public LanguagesAttribute(object DefaultLanguage)
    {
        this.DefaultLanguage = DefaultLanguage;
    }

    /// <summary>
    /// The enum member (of the enum this attribute is applied to) whose language becomes
    /// Umbraco's default language the first time it is created. Must carry <see cref="LanguageAttribute"/>.
    /// </summary>
    public object DefaultLanguage { get; }
}
