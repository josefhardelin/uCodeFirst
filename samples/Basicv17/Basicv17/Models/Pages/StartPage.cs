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
    Icon: ContentTypeIcon.Home,
    Color: ContentTypeColor.Blue,
    AllowedAtRoot: true,
    Folder: "Pages",
    DefaultTemplate: "startPage",
    // News articles live directly under the start page — show them as a sortable/filterable
    // list view in the backoffice instead of a tree.
    IsContainer: true,
    // Singleton, business-critical page — never let scheduled history cleanup remove old versions,
    // regardless of the site-wide history cleanup policy.
    PreventCleanup: true,
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

    [Group(Groups.Content, SortOrder: 3)]
    [ContentBlocksGrid(Name = "Content Grid")]
    public BlockGridModel? ContentGrid => this.Value<BlockGridModel>(_publishedValueFallback, "contentGrid");

    // Editorial picks for the homepage, rooted dynamically under this site rather than a fixed node id
    // (see Models/DataTypes/FeaturedArticlesPicker.cs — roadmap #2, "Configured pickers with dynamic root").
    [Group(Groups.Content, SortOrder: 4)]
    [FeaturedArticlesPicker(Name = "Featured Articles")]
    public IEnumerable<IPublishedContent>? FeaturedArticles => this.Value<IEnumerable<IPublishedContent>>(_publishedValueFallback, "featuredArticles");

    string? ISeoComposition.PageTitle => this.Value<string>(_publishedValueFallback, "pageTitle");
    string? ISeoComposition.MetaDescription => this.Value<string>(_publishedValueFallback, "metaDescription");
}
