using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "Audio" media type. Inherit from this class to make a media type
/// a true child of Audio in the Media Types tree, with typed access to Audio's own properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("Audio", External: true, Guid = "a5ddeee0-8fd8-4cee-a658-6f1fcdb00de3")]
public abstract class UmbracoAudioModel : UmbracoFileMediaModel
{
    /// <param name="content">The underlying published content item.</param>
    /// <param name="fallback">Umbraco's fallback resolver.</param>
    protected UmbracoAudioModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
