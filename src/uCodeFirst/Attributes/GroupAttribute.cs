namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class GroupAttribute : Attribute
{
    public GroupAttribute(string name, int SortOrder = 0)
    {
        Name = name;
        this.SortOrder = SortOrder;
    }

    public string Name { get; }
    public int SortOrder { get; }
}
