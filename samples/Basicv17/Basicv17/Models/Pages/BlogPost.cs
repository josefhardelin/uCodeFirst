using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;
using Basicv17.Models.DataTypes;

namespace Basicv17.Models.Pages;

// Demonstrates several of the newly-added built-in property editors: ContentPicker,
// Tags, MultiUrlPicker, Slider and ColorPicker.
[DocumentType("Blog Post",
    Icon: ContentTypeIcon.Notepad,
    Color: ContentTypeColor.Green,
    Folder: "Pages",
    VariesByCulture: true,
    // High-churn content type — keep every version for the first 30 days (easy rollback of recent
    // edits), then thin history down to one version per day for the following 90 days.
    KeepAllVersionsNewerThanDays: 30,
    KeepLatestVersionPerDayForDays: 90,
    Guid = "b6c1f0a2-4d3e-4a5b-9c6d-7e8f9a0b1c2d")]
[PublishedModel("blogPost")]
public partial class BlogPost : PublishedContentModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public BlogPost(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Headline", Mandatory = true, VariesByCulture = true)]
    public string? Headline => this.Value<string>(_publishedValueFallback, "headline");

    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name = "Body", VariesByCulture = true)]
    public IHtmlEncodedString? Body => this.Value<IHtmlEncodedString>(_publishedValueFallback, "body");

    // Picks the news article this post relates to. Restricted to News Article nodes only via
    // RelatedArticlePicker's AllowedContentTypes (see Models/DataTypes/RelatedArticlePicker.cs).
    [Group(Groups.Content, SortOrder: 2)]
    [RelatedArticlePicker(Name = "Related Article")]
    public IPublishedContent? RelatedArticle => this.Value<IPublishedContent>(_publishedValueFallback, "relatedArticle");

    // Free-form keywords for filtering/search.
    // Fully qualified: "Tags" would otherwise be ambiguous with Microsoft.AspNetCore.Http.TagsAttribute,
    // which is in scope via the Web SDK's implicit global usings.
    [Group(Groups.Content, SortOrder: 3)]
    [uCodeFirst.DataTypes.Tags(Name = "Topics")]
    public IEnumerable<string>? Topics => this.Value<IEnumerable<string>>(_publishedValueFallback, "topics");

    // External "further reading" links.
    [Group(Groups.Content, SortOrder: 4)]
    [MultiUrlPicker(Name = "Further Reading")]
    public IEnumerable<Link>? FurtherReading => this.Value<IEnumerable<Link>>(_publishedValueFallback, "furtherReading");

    // Editorial "importance" rating used to influence listing order.
    // Min/max/step are configured by subclassing SliderDataType (see Models/DataTypes/PrioritySlider.cs)
    // since DataTypeBase config properties are get-only and can't be set via named attribute arguments.
    [Group(Groups.Settings, SortOrder: 0)]
    [PrioritySlider(Name = "Priority")]
    public string? Priority => this.Value<string>(_publishedValueFallback, "priority");

    // Accent color used for the post's category tag in the UI.
    [Group(Groups.Settings, SortOrder: 1)]
    [ColorPicker(Name = "Accent Color")]
    public string? AccentColor => this.Value<string>(_publishedValueFallback, "accentColor");

    // Fixed option list configured in PostCategoryDropdown (see Models/DataTypes/PostCategoryDropdown.cs).
    [Group(Groups.Settings, SortOrder: 2)]
    [PostCategoryDropdown(Name = "Category")]
    public string? Category => this.Value<string>(_publishedValueFallback, "category");

    // Search-engine keywords, kept in a separate "seo" tag group with a pipe delimiter for
    // pasted lists (see Models/DataTypes/SeoKeywordsTags.cs) — contrast with Topics above, which
    // uses the plain [Tags] default (comma delimiter, "default" group).
    [Group(Groups.SEO, SortOrder: 0)]
    [SeoKeywordsTags(Name = "Keywords")]
    public IEnumerable<string>? SeoKeywords => this.Value<IEnumerable<string>>(_publishedValueFallback, "seoKeywords");
}
