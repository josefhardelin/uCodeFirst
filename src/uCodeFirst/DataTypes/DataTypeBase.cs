using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

/// <summary>
/// Base for every property-editor attribute (e.g. <c>[TextString]</c>, <c>[RichText]</c>). Applying
/// a subclass to a property both declares the property and selects the Umbraco editor that backs it.
/// The subclass must also carry <see cref="DataTypeAttribute"/> so sync knows which shared Umbraco
/// data type to create/reuse.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public abstract class DataTypeBase : Attribute
{
    /// <summary>Display name of the property. Derived from the C# property name when left unset.</summary>
    public string? Name { get; set; }
    /// <summary>Property alias. Derived from the C# property name when left unset.</summary>
    public string? Alias { get; set; }
    /// <summary>Whether the property is required before content can be published.</summary>
    public bool Mandatory { get; set; }
    /// <summary>Backoffice description shown below the property's editor.</summary>
    public string? Description { get; set; }
    /// <summary>Whether this property varies by culture. Only takes effect if the owning content type also varies by culture.</summary>
    public bool VariesByCulture { get; set; } = false;

    internal DataTypeAttribute GetDescriptor() =>
        GetType().GetCustomAttribute<DataTypeAttribute>()
        ?? throw new InvalidOperationException(
            $"Data type class '{GetType().Name}' is missing a [DataType] attribute.");

    /// <summary>Builds the Umbraco editor configuration this data type resolves to.</summary>
    /// <param name="key">Deterministic GUID for the shared Umbraco data type.</param>
    /// <param name="name">Display name for the shared Umbraco data type.</param>
    public abstract EditorRecipe BuildRecipe(Guid key, string name);
}
