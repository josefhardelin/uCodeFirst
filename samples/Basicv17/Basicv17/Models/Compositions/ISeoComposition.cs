using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;

namespace UmbracoTCodeFIrst.Models.Compositions;

[CompositionType("SEO", Folder: "Compositions", Guid = "c0000001-0000-0000-0000-000000000001")]
public interface ISeoComposition
{
    [Group("SEO", SortOrder: 0)]
    [TextString(Name = "Page Title")]
    string? PageTitle { get; }

    [Group("SEO", SortOrder: 1)]
    [TextArea(Name = "Meta Description")]
    string? MetaDescription { get; }
}
