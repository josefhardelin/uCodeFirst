using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Stub for Umbraco's built-in "Folder" media type. Inherit from this class to make a media type
/// a true child of Folder in the Media Types tree. Folder itself carries no properties.
/// This class is never created or updated by sync — it already exists in every Umbraco install.
/// </summary>
[MediaType("Folder", External: true, Guid = "f38bd2d7-65d0-48e6-95dc-87ce06ec2d3d")]
public abstract class UmbracoFolderModel : PublishedContentModel
{
    protected UmbracoFolderModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }
}
