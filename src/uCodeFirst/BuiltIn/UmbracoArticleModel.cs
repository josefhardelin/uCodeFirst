using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "Article" media type. Inherit from this class to make a media type
/// a true child of Article in the Media Types tree, with typed access to Article's own properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("Article", External: true, Guid = "a43e3414-9599-4230-a7d3-943a21b20122")]
public abstract class UmbracoArticleModel : UmbracoFileMediaModel
{
    protected UmbracoArticleModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
