using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Rich Text" (Tiptap) editor to a <see cref="string"/> property.</summary>
[DataType("Rich Text", Guid = "8ed9c3f1-a953-4ece-949b-18d69eac8f28")]
public sealed class RichText : RichTextDataType { }
