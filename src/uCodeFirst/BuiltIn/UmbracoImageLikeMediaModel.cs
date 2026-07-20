using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Shared typed accessors for the built-in media types that carry dimensions (Image, Vector
/// Graphics). Not itself an Umbraco media type — a C# mixin only.
/// </summary>
public abstract class UmbracoImageLikeMediaModel : UmbracoFileMediaModel
{
    protected UmbracoImageLikeMediaModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) { }

    public int? Width => this.Value<int?>(ValueFallback, Constants.Conventions.Media.Width);
    public int? Height => this.Value<int?>(ValueFallback, Constants.Conventions.Media.Height);
}
