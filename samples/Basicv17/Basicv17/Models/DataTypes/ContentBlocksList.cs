using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;
using Basicv17.Models.Blocks;

namespace Basicv17.Models.DataTypes;

[DataType("Content Blocks List", Guid = "838d42a4-16af-4831-a4fb-6b7aa374f591")]
public sealed class ContentBlocksList : BlockListDataType
{
    public override BlockDefinition[] Blocks =>
    [
        new() { ContentType = typeof(CallToActionBlock), Label = "{{title}}" }
    ];

    public override bool InlineEditingMode => true;
}
