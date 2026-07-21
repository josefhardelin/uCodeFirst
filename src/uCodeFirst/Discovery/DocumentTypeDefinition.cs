using uCodeFirst.DataTypes;

namespace uCodeFirst.Discovery;

internal sealed record DocumentTypeDefinition(
    Type ClrType,
    bool IsElement,
    Guid Key,
    string Alias,
    string Name,
    string? Icon,
    string? Color,
    string? Description,
    bool AllowedAtRoot,
    string? Folder,
    string? DefaultTemplate,
    IReadOnlyList<Type> AllowedChildTypes,
    IReadOnlyList<PropertyDefinition> Properties,
    IReadOnlyList<Guid> CompositionKeys,
    bool VariesByCulture,
    bool IsContainer);

internal sealed record PropertyDefinition(
    string Alias,
    string Name,
    string GroupName,
    int SortOrder,
    bool Mandatory,
    string? Description,
    DataTypeBase DataType,
    bool VariesByCulture);
