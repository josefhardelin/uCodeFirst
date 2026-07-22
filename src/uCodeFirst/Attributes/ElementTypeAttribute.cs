namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a class as a code-first Umbraco element type. Sync creates or updates a matching content
/// type in Umbraco, keyed by <see cref="Guid"/>. Element types are used as Block List/Block Grid
/// item content, not as standalone tree nodes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ElementTypeAttribute : Attribute
{
    /// <param name="Name">Display name shown in the backoffice.</param>
    /// <param name="Alias">Content type alias. Derived from <paramref name="Name"/> when left unset.</param>
    /// <param name="Icon">Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</param>
    /// <param name="Color">Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</param>
    /// <param name="Description">Backoffice description shown when picking this element type in a Block List/Grid.</param>
    /// <param name="Folder">Backoffice folder path, e.g. "Blocks".</param>
    /// <param name="VariesByCulture">Whether this element type varies by culture.</param>
    /// <param name="IsContainer">Present for API consistency with <see cref="DocumentTypeAttribute"/> and <see cref="MediaTypeAttribute"/>, but has no effect on element types.</param>
    public ElementTypeAttribute(
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Color = null,
        string? Description = null,
        string? Folder = null,
        bool VariesByCulture = false,
        bool IsContainer = false)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Color = Color;
        this.Description = Description;
        this.Folder = Folder;
        this.VariesByCulture = VariesByCulture;
        this.IsContainer = IsContainer;
    }

    /// <summary>Stable GUID for this element type. Leave unset — the code fixer will generate one.</summary>
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
    /// <summary>Backoffice description shown when picking this element type in a Block List/Grid.</summary>
    public string? Description { get; }
    /// <summary>Backoffice folder path, e.g. "Blocks".</summary>
    public string? Folder { get; }
    /// <summary>Whether this element type varies by culture. When true, properties may opt in to per-culture values via their own VariesByCulture flag.</summary>
    public bool VariesByCulture { get; }
    /// <summary>
    /// Present for API consistency with <see cref="DocumentTypeAttribute"/> and <see cref="MediaTypeAttribute"/>, but has
    /// no effect: element types are used as Block List/Grid items, not as tree nodes with children, so sync never applies
    /// a list view to them.
    /// </summary>
    public bool IsContainer { get; }
}
