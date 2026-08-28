namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a <c>const string</c> field as a code-first Umbraco template. The field's own literal
/// value is the template's alias — matched verbatim against a document/element type's
/// DefaultTemplate, so there's no separate alias to keep in sync. Any number of classes may carry
/// <see cref="TemplateAttribute"/>-decorated fields, and a class can also hold unrelated consts.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class TemplateAttribute : Attribute
{
    /// <summary>
    /// The sibling <c>const string</c> field (declared in the same class) that is this template's
    /// master/parent template — e.g. <c>Master: Layout</c>. Must itself carry
    /// <see cref="TemplateAttribute"/>. Leave unset for a top-level template.
    /// </summary>
    public string? Master { get; set; }
}
