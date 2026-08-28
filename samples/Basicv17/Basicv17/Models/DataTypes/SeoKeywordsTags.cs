using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;

namespace Basicv17.Models.DataTypes;

// Demonstrates configuring Tags' group and delimiter by subclassing TagsDataType, the same
// pattern used by PrioritySlider/PostCategoryDropdown for other config-bearing editors.
[DataType("SEO Keywords", Guid = "c52510f5-f4be-4b12-9575-569f85401d95")]
public sealed class SeoKeywordsTags : TagsDataType
{
    public override string Group { get; } = "seo";
    public override char Delimiter { get; } = '|';
}
