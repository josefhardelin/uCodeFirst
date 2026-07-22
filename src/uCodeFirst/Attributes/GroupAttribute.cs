namespace uCodeFirst.Attributes;

/// <summary>
/// Assigns a property to a named backoffice content-editing tab/group. Properties without this
/// attribute fall into Umbraco's default "Content" group.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class GroupAttribute : Attribute
{
    /// <param name="name">Name of the tab/group as shown in the backoffice.</param>
    /// <param name="SortOrder">Sort order of the group relative to other groups on the same content type.</param>
    public GroupAttribute(string name, int SortOrder = 0)
    {
        Name = name;
        this.SortOrder = SortOrder;
    }

    /// <summary>Name of the tab/group as shown in the backoffice.</summary>
    public string Name { get; }
    /// <summary>Sort order of the group relative to other groups on the same content type.</summary>
    public int SortOrder { get; }
}
