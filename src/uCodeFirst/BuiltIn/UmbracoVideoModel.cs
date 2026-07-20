using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "Video" media type. Inherit from this class to make a media type
/// a true child of Video in the Media Types tree, with typed access to Video's own properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("Video", External: true, Guid = "f6c515bb-653c-4bdc-821c-987729ebe327")]
public abstract class UmbracoVideoModel : UmbracoFileMediaModel
{
    protected UmbracoVideoModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
