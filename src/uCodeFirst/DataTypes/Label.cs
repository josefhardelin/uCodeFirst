using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's read-only "Label" editor to a property, for displaying computed or system-managed values.</summary>
[DataType("Label", Guid = "0405be36-7f7d-4fde-9b14-de8e2b159fae")]
public sealed class Label : LabelDataType { }
