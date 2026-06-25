using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;
using Basicv17.Models.Blocks;
using Basicv17.Models.Compositions;
using Basicv17.Models.DataTypes;

namespace Basicv17.Models.Pages;

[DocumentType("Start Page",
    Icon: "icon-home",
    AllowedAtRoot: true,
    Folder: "Pages",
    DefaultTemplate: "startPage",
    Guid = "a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
[AllowedChildren(typeof(NewsArticle))]
[PublishedModel("startPage")]
public partial class StartPage : PublishedContentModel, ISeoComposition
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public StartPage(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Headline", Mandatory = true)]
    public string? Headline => this.Value<string>(_publishedValueFallback, "headline");

    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name = "Body")]
    public IHtmlEncodedString? Body => this.Value<IHtmlEncodedString>(_publishedValueFallback, "body");

    [Group(Groups.Content, SortOrder: 2)]
    [ContentBlocksList(Name = "Content Blocks")]
    public BlockListModel? ContentBlocks => this.Value<BlockListModel>(_publishedValueFallback, "contentBlocks");

    string? ISeoComposition.PageTitle => this.Value<string>(_publishedValueFallback, "pageTitle");
    string? ISeoComposition.MetaDescription => this.Value<string>(_publishedValueFallback, "metaDescription");
}
