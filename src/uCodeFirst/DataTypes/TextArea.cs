using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's multi-line "Text Area" editor to a <see cref="string"/> property.</summary>
[DataType("Text Area", Guid = "dabb7dbc-40c9-47c8-a36a-ad31dd206b6f")]
public sealed class TextArea : TextAreaDataType { }
