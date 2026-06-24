namespace Consid.Umbraco.CodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public abstract class PropertyEditorAttribute : Attribute
{
    protected PropertyEditorAttribute(
        string? Name = null,
        string? Alias = null,
        bool Mandatory = false,
        string? Description = null)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Mandatory = Mandatory;
        this.Description = Description;
    }

    public string? Name { get; }
    public string? Alias { get; }
    public bool Mandatory { get; }
    public string? Description { get; }
}
