using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Slider" editor to an <see cref="int"/> property.</summary>
[DataType("Slider", Guid = "7bb78e52-fe93-4b0e-9d69-0ea6a2bb8a59")]
public sealed class Slider : SliderDataType { }
