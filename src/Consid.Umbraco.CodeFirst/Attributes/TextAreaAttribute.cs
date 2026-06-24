namespace Consid.Umbraco.CodeFirst.Attributes;

public sealed class TextAreaAttribute : PropertyEditorAttribute
{
    public TextAreaAttribute(
        string? Name = null,
        string? Alias = null,
        bool Mandatory = false,
        string? Description = null,
        int MaxLength = 0)
        : base(Name, Alias, Mandatory, Description)
    {
        this.MaxLength = MaxLength;
    }

    public int MaxLength { get; }
}
