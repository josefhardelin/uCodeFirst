using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Color Picker" editor to a <see cref="string"/> property.</summary>
[DataType("Color Picker", Guid = "03146cc2-05f2-4a93-b86c-b05f29075fd3")]
public sealed class ColorPicker : ColorPickerDataType { }
