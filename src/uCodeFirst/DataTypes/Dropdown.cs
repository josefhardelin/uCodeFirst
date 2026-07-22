using uCodeFirst.Attributes;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Dropdown" editor to a <see cref="string"/> property, for picking one or more values from a fixed <see cref="DropdownDataType.Options"/> list.</summary>
[DataType("Dropdown", Guid = "ed94430a-a3be-4231-83e4-c015db0fa6a9")]
public sealed class Dropdown : DropdownDataType { }
