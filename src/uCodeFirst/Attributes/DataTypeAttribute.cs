namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DataTypeAttribute : Attribute
{
    public DataTypeAttribute(string Name, string? Alias = null, string? Folder = null)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Folder = Folder;
    }

    /// <summary>Stable GUID for this data type. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    public System.Guid Key => System.Guid.Parse(Guid);
    public string Name { get; }
    public string? Alias { get; }
    /// <summary>Backoffice folder path, e.g. "Data Types/Text".</summary>
    public string? Folder { get; }
}
