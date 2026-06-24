namespace Consid.Umbraco.CodeFirst.Attributes;

public sealed class DropdownAttribute : PropertyEditorAttribute
{
    public DropdownAttribute(
        string? Name = null,
        string? Alias = null,
        bool Mandatory = false,
        string? Description = null,
        bool AllowMultiple = false)
        : base(Name, Alias, Mandatory, Description)
    {
        this.AllowMultiple = AllowMultiple;
    }

    public bool AllowMultiple { get; }

    /// <summary>Options/items shown in the dropdown. Set via attribute property syntax: <c>Options = new[] { "a", "b" }</c>.</summary>
    public string[] Options { get; set; } = [];
}
