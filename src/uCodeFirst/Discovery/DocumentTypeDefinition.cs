using uCodeFirst.Attributes;

namespace uCodeFirst.Discovery;

internal sealed record DocumentTypeDefinition(
    Type ClrType,
    bool IsElement,
    Guid Key,
    string Alias,
    string Name,
    string? Icon,
    string? Description,
    bool AllowedAtRoot,
    string? Folder,
    string? DefaultTemplate,
    IReadOnlyList<Type> AllowedChildTypes,
    IReadOnlyList<PropertyDefinition> Properties,
    IReadOnlyList<Guid> CompositionKeys);

internal sealed record PropertyDefinition(
    string Alias,
    string Name,
    string GroupName,
    int SortOrder,
    bool Mandatory,
    string? Description,
    PropertyEditorAttribute EditorAttribute);
