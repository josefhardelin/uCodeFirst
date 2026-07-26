namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a class as a code-first content seed — an empty stub content node (no property values)
/// created once at a deterministic <see cref="Guid"/> so other code-first config (e.g. a future
/// picker's dynamic root) has a stable node to point at. Sync creates the node if it doesn't already
/// exist and immediately publishes it; an existing node (matched by <see cref="Guid"/>) is never
/// updated or deleted. Apply this to a plain marker class with no members.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SeedContentAttribute : Attribute
{
    /// <param name="DocumentType">The code-first document type to create an instance of. Must carry a <see cref="DocumentTypeAttribute"/>.</param>
    /// <param name="Name">Display name for the seeded content node.</param>
    /// <param name="Parent">
    /// Another class marked with <see cref="SeedContentAttribute"/> whose node becomes this seed's
    /// parent. Null means the content root.
    /// </param>
    public SeedContentAttribute(Type DocumentType, string Name, Type? Parent = null)
    {
        this.DocumentType = DocumentType;
        this.Name = Name;
        this.Parent = Parent;
    }

    /// <summary>Stable GUID for this seeded content node. Leave unset — the code fixer will generate one.</summary>
    public string Guid { get; set; } = "";

    /// <summary>The parsed <see cref="System.Guid"/> value of <see cref="Guid"/>.</summary>
    public System.Guid Key => System.Guid.Parse(Guid);
    /// <summary>The code-first document type to create an instance of. Must carry a <see cref="DocumentTypeAttribute"/>.</summary>
    public Type DocumentType { get; }
    /// <summary>Display name for the seeded content node.</summary>
    public string Name { get; }
    /// <summary>Another class marked with <see cref="SeedContentAttribute"/> whose node becomes this seed's parent. Null means the content root.</summary>
    public Type? Parent { get; }
}
