namespace uCodeFirst.Attributes;

/// <summary>
/// Restricts which document/element types may be created as children of this content type in the
/// backoffice. Each type must itself carry <see cref="DocumentTypeAttribute"/> or <see cref="ElementTypeAttribute"/>.
/// Omit this attribute to leave children unrestricted.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AllowedChildrenAttribute : Attribute
{
    /// <param name="types">The document/element types allowed as children.</param>
    public AllowedChildrenAttribute(params Type[] types) => Types = types;

    /// <summary>The document/element types allowed as children.</summary>
    public Type[] Types { get; }
}
