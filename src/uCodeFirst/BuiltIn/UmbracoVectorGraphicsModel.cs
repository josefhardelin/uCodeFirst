using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "Vector Graphics (SVG)" media type. Inherit from this class to make
/// a media type a true child of it in the Media Types tree, with typed access to its properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("Vector Graphics (SVG)", External: true, Guid = "c4b1efcf-a9d5-41c4-9621-e9d273b52a9c")]
public abstract class UmbracoVectorGraphicsModel : UmbracoImageLikeMediaModel
{
    protected UmbracoVectorGraphicsModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
