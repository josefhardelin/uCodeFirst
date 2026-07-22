using uCodeFirst.Attributes;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Tags" editor to a <see cref="string"/>[] property.</summary>
[DataType("Tags", Guid = "620f3f5e-6b4b-456a-9c3f-fc19b38bc15b")]
public sealed class Tags : TagsDataType { }
