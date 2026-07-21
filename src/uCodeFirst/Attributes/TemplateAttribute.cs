namespace uCodeFirst.Attributes;

/// <summary>
/// Marks an enum member as a code-first Umbraco template, declaring its position in Umbraco's
/// master/parent template hierarchy. Unlike <see cref="LanguagesAttribute"/>, there's no
/// single-enum or default-member requirement — any number of enums may carry
/// <see cref="TemplateAttribute"/>-decorated members, and the enum can also hold unrelated values.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class TemplateAttribute : Attribute
{
    public TemplateAttribute(string Alias)
    {
        this.Alias = Alias;
    }

    /// <summary>Alias of the template. Matched against a document/element type's DefaultTemplate.</summary>
    public string Alias { get; }

    /// <summary>
    /// The enum member (of the same enum) that is this template's master/parent template. Must
    /// itself carry <see cref="TemplateAttribute"/>. Leave unset for a top-level template.
    /// </summary>
    public object? Master { get; set; }
}
