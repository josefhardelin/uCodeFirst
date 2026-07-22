namespace uCodeFirst.Attributes;

/// <summary>
/// Marks an interface as a code-first Umbraco composition. A document/element type class implements
/// the interface to inherit its properties as a composition, rather than declaring them itself.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class CompositionTypeAttribute : Attribute
{
    /// <param name="Name">Display name shown in the backoffice.</param>
    /// <param name="Alias">Content type alias. Derived from <paramref name="Name"/> when left unset.</param>
    /// <param name="Icon">Backoffice icon class. Use <see cref="ContentTypeIcon"/> for available constants.</param>
    /// <param name="Color">Backoffice icon color. Use <see cref="ContentTypeColor"/> for available constants.</param>
    /// <param name="Description">Backoffice description shown for the composition.</param>
    /// <param name="Folder">Backoffice folder path, e.g. "Compositions".</param>
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
    /// <summary>Backoffice description shown for the composition.</summary>
    public string? Description { get; }
    /// <summary>Backoffice folder path, e.g. "Compositions".</summary>
    public string? Folder { get; }
}
