using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace Basicv17.Models.Pages;

[DocumentType("News Article",
    Icon: ContentTypeIcon.Newspaper,
    Color: ContentTypeColor.Yellow,
    Folder: "Pages",
    DefaultTemplate: "newsArticle",
    VariesByCulture: true,
    Guid = "8f3c1a2b-3e4d-4f5a-b6c7-d8e9f0a1b2c3")]
[PublishedModel("newsArticle")]
public partial class NewsArticle : PublishedContentModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public NewsArticle(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    // Culture-varying: editors provide a translated headline per language.
    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Headline", Mandatory = true, VariesByCulture = true)]
    public string? Headline => this.Value<string>(_publishedValueFallback, "headline");

    // Culture-varying: the article body is translated per language.
    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name = "Body", VariesByCulture = true)]
    public IHtmlEncodedString? Body => this.Value<IHtmlEncodedString>(_publishedValueFallback, "body");

    // Invariant: the same author byline is shown regardless of culture.
    [Group(Groups.Content, SortOrder: 2)]
    [TextString(Name = "Author")]
    public string? Author => this.Value<string>(_publishedValueFallback, "author");

    [Group(Groups.Content, SortOrder: 3)]
    [DatePicker(Name = "Published Date")]
    public DateTime? PublishedDate => this.Value<DateTime?>(_publishedValueFallback, "publishedDate");

    [Group(Groups.Settings, SortOrder: 0)]
    [TrueFalse(Name = "Is Breaking News")]
    public bool IsBreakingNews => this.Value<bool>(_publishedValueFallback, "isBreakingNews");
}
