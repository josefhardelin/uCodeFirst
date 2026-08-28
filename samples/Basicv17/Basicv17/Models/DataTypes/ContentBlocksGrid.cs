using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;
using Basicv17.Models.Blocks;

namespace Basicv17.Models.DataTypes;

[DataType("Content Blocks Grid", Guid = "bba636f3-3ab0-4343-bc5e-39a780b18547")]
public sealed class ContentBlocksGrid : BlockGridDataType
{
    public override int GridColumns => 12;

    public override BlockDefinition[] Blocks =>
    [
        new()
        {
            ContentType = typeof(CallToActionBlock),
            Label = "{{title}}",
            AllowAtRoot = true,
            AllowInAreas = false,
            ColumnSpanOptions = [6, 12]
        },
        new()
        {
            ContentType = typeof(TextBlock),
            AllowAtRoot = true,
            AllowInAreas = false,
            ColumnSpanOptions = [4, 8, 12],
            RowMinSpan = 1,
            RowMaxSpan = 2
        }
    ];
}
