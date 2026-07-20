using uCodeFirst.Attributes;
using uCodeFirst.BuiltIn;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Basicv17.Models.Media;

// Inherits from the built-in Umbraco Image type — becomes a true child of Image in the
// Media Types tree, and gets typed Width/Height/Bytes/Extension/Url from UmbracoImageModel.
[MediaType("Site Image",
    Icon: ContentTypeIcon.Picture,
    Color: ContentTypeColor.Blue,
    AllowedAtRoot: true,
    Guid = "b5c6d7e8-f9a0-4b1c-8d2e-3f4a5b6c7d8e")]
[PublishedModel("siteImage")]
public partial class SiteImage : UmbracoImageModel
{
    public SiteImage(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }

    [TextString(Name = "Alt Text")]
    public string? AltText => this.Value<string>(ValueFallback, "altText");

    [TextArea(Name = "Caption")]
    public string? Caption => this.Value<string>(ValueFallback, "caption");
}
