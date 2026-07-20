namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MediaTypeAttribute : Attribute
{
    public MediaTypeAttribute(
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Color = null,
        string? Description = null,
        bool AllowedAtRoot = false,
        string? Folder = null,
        string[]? Compositions = null,
        bool External = false)
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
    }

    /// <summary>Stable GUID for this media type. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    public System.Guid Key => System.Guid.Parse(Guid);
    public string Name { get; }
    public string? Alias { get; }
    /// <summary>Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</summary>
    public string? Icon { get; }
    /// <summary>Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</summary>
    public string? Color { get; }
    public string? Description { get; }
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
}
