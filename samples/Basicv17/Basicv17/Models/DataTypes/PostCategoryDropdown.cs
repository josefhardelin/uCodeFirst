using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;

namespace Basicv17.Models.DataTypes;

// Fixed option list configured by subclassing DropdownDataType (see PrioritySlider.cs for the
// same pattern) since DataTypeBase config properties are get-only.
[DataType("Post Category", Guid = "8a2e1c4d-6b5f-4a3e-9c1d-2f3e4a5b6c7d")]
public sealed class PostCategoryDropdown : DropdownDataType
{
    public override string[] Options { get; } = ["News", "Tutorial", "Announcement"];
}
