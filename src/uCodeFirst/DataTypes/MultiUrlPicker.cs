using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Multi URL Picker" editor to a property for picking one or more links (content, media or external URLs).</summary>
[DataType("Multi URL Picker", Guid = "da7f7b72-9ec7-4ea4-9fb1-98105e7d0825")]
public sealed class MultiUrlPicker : MultiUrlPickerDataType { }
