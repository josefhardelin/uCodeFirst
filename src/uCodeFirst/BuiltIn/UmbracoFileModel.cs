using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "File" media type. Inherit from this class to make a media type
/// a true child of File in the Media Types tree, with typed access to File's own properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("File", External: true, Guid = "4c52d8ab-54e6-40cd-999c-7a5f24903e4d")]
public abstract class UmbracoFileModel : UmbracoFileMediaModel
{
    protected UmbracoFileModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
