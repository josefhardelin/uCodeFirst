using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;

namespace Basicv17.Models.DataTypes;

// Demonstrates configuring a built-in editor by subclassing its *DataType base class,
// the same pattern used by ContentBlocksList/ContentBlocksGrid for BlockList/BlockGrid.
[DataType("Priority Slider", Guid = "fd91456a-63b6-475f-b969-1f01959efcb9")]
public sealed class PrioritySlider : SliderDataType
{
    public override int MinValue { get; } = 1;
    public override int MaxValue { get; } = 5;
    public override int Step { get; } = 1;
}
