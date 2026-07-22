using uCodeFirst.Attributes;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's single-line "Text String" editor to a <see cref="string"/> property.</summary>
[DataType("Text String", Guid = "b208db19-381c-4e06-8a73-b41922917e5a")]
public sealed class TextString : TextStringDataType { }
