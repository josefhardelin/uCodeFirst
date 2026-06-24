namespace Consid.Umbraco.CodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocumentTypeAttribute : Attribute
{
    public DocumentTypeAttribute(
        string Guid,
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Description = null,
        bool AllowedAtRoot = false)
    {
        Key = System.Guid.Parse(Guid);
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Description = Description;
        this.AllowedAtRoot = AllowedAtRoot;
    }

    public System.Guid Key { get; }
    public string Name { get; }
    public string? Alias { get; }
    public string? Icon { get; }
    public string? Description { get; }
    public bool AllowedAtRoot { get; }
}
