using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public abstract class DataTypeBase : Attribute
{
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public bool Mandatory { get; set; }
    public string? Description { get; set; }

    internal DataTypeAttribute GetDescriptor() =>
        GetType().GetCustomAttribute<DataTypeAttribute>()
        ?? throw new InvalidOperationException(
            $"Data type class '{GetType().Name}' is missing a [DataType] attribute.");

    internal abstract EditorRecipe BuildRecipe(Guid key, string name);
}
