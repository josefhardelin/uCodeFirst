namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class CompositionTypeAttribute : Attribute
{
    public CompositionTypeAttribute(
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Color = null,
        string? Description = null,
        string? Folder = null)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Color = Color;
        this.Description = Description;
        this.Folder = Folder;
    }

    /// <summary>Stable GUID for this composition type. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    public System.Guid Key => System.Guid.Parse(Guid);
    public string Name { get; }
    public string? Alias { get; }
    /// <summary>Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</summary>
    public string? Icon { get; }
    /// <summary>Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</summary>
    public string? Color { get; }
    public string? Description { get; }
    /// <summary>Backoffice folder path, e.g. "Compositions".</summary>
    public string? Folder { get; }
}
