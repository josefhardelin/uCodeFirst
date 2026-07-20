using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "Image" media type. Inherit from this class to make a media type
/// a true child of Image in the Media Types tree, with typed access to Image's own properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("Image", External: true, Guid = "cc07b313-0843-4aa8-bbda-871c8da728c8")]
public abstract class UmbracoImageModel : UmbracoImageLikeMediaModel
{
    protected UmbracoImageModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
