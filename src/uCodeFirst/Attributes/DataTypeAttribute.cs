namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a property-editor class (one deriving from a <c>*DataType</c> base such as
/// <c>TextStringDataType</c>) as a shared Umbraco data type. Sync ensures a matching data type
/// exists in Umbraco, keyed by <see cref="Guid"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DataTypeAttribute : Attribute
{
    /// <param name="Name">Display name shown in the Data Types tree.</param>
    /// <param name="Alias">Data type alias. Derived from <paramref name="Name"/> when left unset.</param>
    /// <param name="Folder">Backoffice folder path, e.g. "Data Types/Text".</param>
    public DataTypeAttribute(string Name, string? Alias = null, string? Folder = null)
    {
        this.Name = Name;
        this.Alias = Alias;
        this.Folder = Folder;
    }

    /// <summary>Stable GUID for this data type. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    /// <summary>The parsed <see cref="System.Guid"/> value of <see cref="Guid"/>.</summary>
    public System.Guid Key => System.Guid.Parse(Guid);
    /// <summary>Display name shown in the Data Types tree.</summary>
    public string Name { get; }
    /// <summary>Data type alias. Derived from <see cref="Name"/> when left unset.</summary>
    public string? Alias { get; }
    /// <summary>Backoffice folder path, e.g. "Data Types/Text".</summary>
    public string? Folder { get; }
}
