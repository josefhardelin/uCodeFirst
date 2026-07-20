namespace uCodeFirst.Discovery;

internal sealed record MediaTypeDefinition(
    Type ClrType,
    Guid Key,
    string Alias,
    string Name,
    string? Icon,
    string? Color,
    string? Description,
    bool AllowedAtRoot,
    string? Folder,
    IReadOnlyList<Type> AllowedChildTypes,
    IReadOnlyList<PropertyDefinition> Properties,
    IReadOnlyList<Guid> CompositionKeys,
    Guid? ParentKey);
