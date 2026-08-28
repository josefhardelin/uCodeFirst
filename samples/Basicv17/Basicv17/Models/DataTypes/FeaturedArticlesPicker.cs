using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;
using Basicv17.Models.Pages;

namespace Basicv17.Models.DataTypes;

// Demonstrates a dynamic root (roadmap #2): instead of a fixed start node id, the root is computed
// at render time as the nearest News Article descendant-or-self of the current site's root. In a
// multi-site setup this means editors on any site only ever see that site's own articles, with no
// hardcoded node id to break when content is copied/moved between environments.
[DataType("Featured Articles Picker", Guid = "d2e3f4a5-6b7c-4d8e-9f0a-1b2c3d4e5f6a")]
public sealed class FeaturedArticlesPicker : MultiNodeTreePickerDataType
{
    public override int MaxItems { get; } = 5;

    public override Type[] AllowedContentTypes { get; } = [typeof(NewsArticle)];

    public override DynamicRootConfig? DynamicRoot { get; } = new()
    {
        Origin = DynamicRootOrigin.Site,
        QuerySteps =
        [
            new DynamicRootQueryStep
            {
                Direction = DynamicRootQueryStepDirection.NearestDescendantOrSelf,
                DocumentTypes = [typeof(NewsArticle)]
            }
        ]
    };
}
