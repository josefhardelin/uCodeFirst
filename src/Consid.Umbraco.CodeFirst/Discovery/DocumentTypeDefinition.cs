using Consid.Umbraco.CodeFirst.Attributes;

namespace Consid.Umbraco.CodeFirst.Discovery;

internal sealed record DocumentTypeDefinition(
    Type ClrType,
    Guid Key,
    string Alias,
    string Name,
    string? Icon,
    string? Description,
    bool AllowedAtRoot,
    IReadOnlyList<Type> AllowedChildTypes,
    IReadOnlyList<PropertyDefinition> Properties);

internal sealed record PropertyDefinition(
    string Alias,
    string Name,
    string GroupName,
    int SortOrder,
    bool Mandatory,
    string? Description,
    PropertyEditorAttribute EditorAttribute);
