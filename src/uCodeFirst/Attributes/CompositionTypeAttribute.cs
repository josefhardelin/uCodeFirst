namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class CompositionTypeAttribute : Attribute
{
    public CompositionTypeAttribute(
        string Guid,
        string Name,
        string? Alias = null,
        string? Icon = null,
        string? Description = null,
        string? Folder = null)
    {
        Key = System.Guid.Parse(Guid);
        this.Name = Name;
        this.Alias = Alias;
        this.Icon = Icon;
        this.Description = Description;
        this.Folder = Folder;
    }

    public System.Guid Key { get; }
    public string Name { get; }
    public string? Alias { get; }
    public string? Icon { get; }
    public string? Description { get; }
    /// <summary>Backoffice folder path, e.g. "Compositions".</summary>
    public string? Folder { get; }
}
