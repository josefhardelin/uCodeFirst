namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a class as a code-first Umbraco document type. Sync creates or updates a matching content
/// type in Umbraco, keyed by <see cref="Guid"/>. Public properties on the class (optionally decorated
/// with a data-type attribute such as <c>[TextString]</c>) become the document type's properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocumentTypeAttribute : Attribute
{
    /// <param name="Name">Display name shown in the backoffice.</param>
    /// <param name="Alias">Content type alias. Derived from <paramref name="Name"/> when left unset.</param>
    /// <param name="Icon">Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</param>
    /// <param name="Color">Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</param>
    /// <param name="Description">Backoffice description shown when creating content of this type.</param>
    /// <param name="AllowedAtRoot">Whether content of this type may be created at the content tree root.</param>
    /// <param name="Folder">Backoffice folder path, e.g. "Pages" or "Pages/Articles".</param>
    /// <param name="DefaultTemplate">Template alias to link as the default template, e.g. "startPage". Null means no template.</param>
    /// <param name="VariesByCulture">Whether this content type varies by culture.</param>
    /// <param name="IsContainer">When true, this content type's children are shown in the backoffice as a sortable/filterable list view instead of a tree.</param>
    public DocumentTypeAttribute(
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Color = null,
        string? Description = null,
        bool AllowedAtRoot = false,
        string? Folder = null,
        string? DefaultTemplate = null,
        bool VariesByCulture = false,
        bool IsContainer = false)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Color = Color;
        this.Description = Description;
        this.AllowedAtRoot = AllowedAtRoot;
        this.Folder = Folder;
        this.DefaultTemplate = DefaultTemplate;
        this.VariesByCulture = VariesByCulture;
        this.IsContainer = IsContainer;
    }

    /// <summary>Stable GUID for this document type. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    /// <summary>The parsed <see cref="System.Guid"/> value of <see cref="Guid"/>.</summary>
    public System.Guid Key => System.Guid.Parse(Guid);
    /// <summary>Display name shown in the backoffice.</summary>
    public string Name { get; }
    /// <summary>Content type alias. Derived from <see cref="Name"/> when left unset.</summary>
    public string? Alias { get; }
    /// <summary>Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</summary>
    public string? Icon { get; }
    /// <summary>Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</summary>
    public string? Color { get; }
    /// <summary>Backoffice description shown when creating content of this type.</summary>
    public string? Description { get; }
    /// <summary>Whether content of this type may be created at the content tree root.</summary>
    public bool AllowedAtRoot { get; }
    /// <summary>Backoffice folder path, e.g. "Pages" or "Pages/Articles".</summary>
    public string? Folder { get; }
    /// <summary>Template alias to link as the default template, e.g. "startPage". Null means no template.</summary>
    public string? DefaultTemplate { get; }
    /// <summary>Whether this content type varies by culture. When true, properties may opt in to per-culture values via their own VariesByCulture flag.</summary>
    public bool VariesByCulture { get; }
    /// <summary>When true, this content type's children are shown in the backoffice as a sortable/filterable list view instead of a tree.</summary>
    public bool IsContainer { get; }
}
