namespace Consid.Umbraco.CodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AllowedChildrenAttribute : Attribute
{
    public AllowedChildrenAttribute(params Type[] types) => Types = types;

    public Type[] Types { get; }
}
