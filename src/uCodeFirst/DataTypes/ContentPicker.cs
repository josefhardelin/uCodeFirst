using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Content Picker" editor to a <see cref="Guid"/>/<see cref="string"/> property.</summary>
[DataType("Content Picker", Guid = "c97987b5-0ee5-499d-91e8-ec746e7a1e53")]
public sealed class ContentPicker : ContentPickerDataType { }
