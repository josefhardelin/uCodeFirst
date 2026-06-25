using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace Basicv17.Models.Blocks;

[ElementType("Text Block", Icon: ContentTypeIcon.Paragraph, Color: ContentTypeColor.Gray, Folder: "Blocks", Guid = "adc904bf-01fa-4885-a907-4b047b0e894a")]
[PublishedModel("textBlock")]
public partial class TextBlock : PublishedElementModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public TextBlock(IPublishedElement content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Heading")]
    public string? Heading => this.Value<string>(_publishedValueFallback, "heading");

    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name = "Text")]
    public Umbraco.Cms.Core.Strings.IHtmlEncodedString? Text => this.Value<Umbraco.Cms.Core.Strings.IHtmlEncodedString>(_publishedValueFallback, "text");
}
