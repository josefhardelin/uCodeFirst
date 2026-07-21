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

    // Picks the news article this post relates to.
    [Group(Groups.Content, SortOrder: 2)]
    [ContentPicker(Name = "Related Article")]
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
}
