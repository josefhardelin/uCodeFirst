namespace Consid.Umbraco.CodeFirst.Attributes;

public sealed class NumericAttribute : PropertyEditorAttribute
{
    public NumericAttribute(
        string? Name = null,
        string? Alias = null,
        bool Mandatory = false,
        string? Description = null)
        : base(Name, Alias, Mandatory, Description)
    {
    }
}
