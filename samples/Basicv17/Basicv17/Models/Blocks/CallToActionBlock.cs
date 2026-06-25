using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace Basicv17.Models.Blocks;

[ElementType("Call To Action Block", Icon: ContentTypeIcon.Flash, Color: ContentTypeColor.Red, Folder: "Blocks", Guid = "e1000001-0000-0000-0000-000000000001")]
[PublishedModel("callToActionBlock")]
public partial class CallToActionBlock : PublishedElementModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public CallToActionBlock(IPublishedElement content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Title", Mandatory = true)]
    public string? Title => this.Value<string>(_publishedValueFallback, "title");

    [Group(Groups.Content, SortOrder: 1)]
    [TextString(Name = "Link URL")]
    public string? LinkUrl => this.Value<string>(_publishedValueFallback, "linkUrl");

    [Group(Groups.Content, SortOrder: 2)]
    [TextString(Name = "Link Label")]
    public string? LinkLabel => this.Value<string>(_publishedValueFallback, "linkLabel");
}
