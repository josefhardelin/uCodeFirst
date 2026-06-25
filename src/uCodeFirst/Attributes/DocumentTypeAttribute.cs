namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocumentTypeAttribute : Attribute
{
    public DocumentTypeAttribute(
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Color = null,
        string? Description = null,
        bool AllowedAtRoot = false,
        string? Folder = null,
        string? DefaultTemplate = null)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Color = Color;
        this.Description = Description;
        this.AllowedAtRoot = AllowedAtRoot;
        this.Folder = Folder;
        this.DefaultTemplate = DefaultTemplate;
    }

    /// <summary>Stable GUID for this document type. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    public System.Guid Key => System.Guid.Parse(Guid);
    public string Name { get; }
    public string? Alias { get; }
    public string? Icon { get; }
    public string? Color { get; }
    public string? Description { get; }
    public bool AllowedAtRoot { get; }
    /// <summary>Backoffice folder path, e.g. "Pages" or "Pages/Articles".</summary>
    public string? Folder { get; }
    /// <summary>Template alias to link as the default template, e.g. "startPage". Null means no template.</summary>
    public string? DefaultTemplate { get; }
}
