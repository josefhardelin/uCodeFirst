using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using UmbracoTCodeFIrst.Models.Blocks;

namespace UmbracoTCodeFIrst.Models.DataTypes;

[DataType("Content Blocks List", Guid = "838d42a4-16af-4831-a4fb-6b7aa374f591")]
public sealed class ContentBlocksList : BlockListDataType
{
    public override Type[] BlockTypes => [typeof(CallToActionBlock)];
}
