using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace uCodeFirst.BuiltIn;

/// <summary>
/// Shared typed accessors for Umbraco's upload-based built-in media types (File, Video, Audio,
/// Article, Vector Graphics, Image). Not itself an Umbraco media type — a C# mixin only.
/// </summary>
public abstract class UmbracoFileMediaModel : PublishedContentModel
{
    protected readonly IPublishedValueFallback ValueFallback;

    protected UmbracoFileMediaModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => ValueFallback = fallback;

    public string? Url => this.Url();
    public long? Bytes => this.Value<long?>(ValueFallback, Constants.Conventions.Media.Bytes);
    public string? Extension => this.Value<string?>(ValueFallback, Constants.Conventions.Media.Extension);
}
