using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Basicv17.Models.Media;

// Compositions: built-in Umbraco Image type (cc07b313-...) — gives umbracoFile, width, height, etc.
[MediaType("Site Image",
    Icon: ContentTypeIcon.Picture,
    Color: ContentTypeColor.Blue,
    AllowedAtRoot: true,
    Compositions: ["cc07b313-0843-4aa8-bbda-871c8da728c8"],
    Guid = "b5c6d7e8-f9a0-4b1c-8d2e-3f4a5b6c7d8e")]
[PublishedModel("siteImage")]
public partial class SiteImage : PublishedContentModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public SiteImage(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Alt Text")]
    public string? AltText => this.Value<string>(_publishedValueFallback, "altText");

    [Group(Groups.Content, SortOrder: 1)]
    [TextArea(Name = "Caption")]
    public string? Caption => this.Value<string>(_publishedValueFallback, "caption");
}
