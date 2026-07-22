namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a class as a code-first Umbraco media type. Sync creates or updates a matching media type
/// in Umbraco, keyed by <see cref="Guid"/> — unless <see cref="External"/> is set, in which case the
/// class is treated as a stub for a media type Umbraco already ships (see <c>uCodeFirst.BuiltIn</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MediaTypeAttribute : Attribute
{
    /// <param name="Name">Display name shown in the backoffice.</param>
    /// <param name="Alias">Content type alias. Derived from <paramref name="Name"/> when left unset.</param>
    /// <param name="Icon">Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</param>
    /// <param name="Color">Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</param>
    /// <param name="Description">Backoffice description shown when creating media of this type.</param>
    /// <param name="AllowedAtRoot">Whether media of this type may be created at the media tree root.</param>
    /// <param name="Folder">Backoffice folder path, e.g. "Media" or "Media/Files".</param>
    /// <param name="Compositions">GUIDs of existing Umbraco media types to add as compositions, e.g. the built-in Image type.</param>
    /// <param name="External">
    /// Marks this as a stub for a media type that already exists in Umbraco (e.g. the built-in Image type) —
    /// it is never created or updated by sync.
    /// </param>
    /// <param name="IsContainer">When true, this media type's children are shown in the backoffice as a sortable/filterable list view instead of a tree.</param>
    public MediaTypeAttribute(
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Color = null,
        string? Description = null,
        bool AllowedAtRoot = false,
        string? Folder = null,
        string[]? Compositions = null,
        bool External = false,
        bool IsContainer = false)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Color = Color;
        this.Description = Description;
        this.AllowedAtRoot = AllowedAtRoot;
        this.Folder = Folder;
        this.Compositions = Compositions ?? [];
        this.External = External;
        this.IsContainer = IsContainer;
    }

    /// <summary>Stable GUID for this media type. Leave unset — the code fixer will generate one.</summary>
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
    /// <summary>Backoffice description shown when creating media of this type.</summary>
    public string? Description { get; }
    /// <summary>Whether media of this type may be created at the media tree root.</summary>
    public bool AllowedAtRoot { get; }
    /// <summary>Backoffice folder path, e.g. "Media" or "Media/Files".</summary>
    public string? Folder { get; }
    /// <summary>GUIDs of existing Umbraco media types to add as compositions, e.g. the built-in Image type.</summary>
    public string[] Compositions { get; }
    /// <summary>
    /// Marks this as a stub for a media type that already exists in Umbraco (e.g. the built-in Image type) —
    /// it is never created or updated by sync. Other media type classes may inherit from an External-marked
    /// class to become a true child of that type in the Media Types tree.
    /// </summary>
    public bool External { get; }
    /// <summary>When true, this media type's children are shown in the backoffice as a sortable/filterable list view instead of a tree.</summary>
    public bool IsContainer { get; }
}
