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
    /// <summary>Umbraco's fallback resolver, used by derived classes to read typed property values.</summary>
    protected readonly IPublishedValueFallback ValueFallback;

    /// <param name="content">The underlying published content item.</param>
    /// <param name="fallback">Umbraco's fallback resolver, passed through to <see cref="ValueFallback"/>.</param>
    protected UmbracoFileMediaModel(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => ValueFallback = fallback;

    /// <summary>The media item's URL.</summary>
    public string? Url => this.Url();
    /// <summary>File size in bytes.</summary>
    public long? Bytes => this.Value<long?>(ValueFallback, Constants.Conventions.Media.Bytes);
    /// <summary>File extension, without a leading dot, e.g. "pdf".</summary>
    public string? Extension => this.Value<string?>(ValueFallback, Constants.Conventions.Media.Extension);
}
