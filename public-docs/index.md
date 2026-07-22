---
_layout: landing
---

# uCodeFirst

Code-first schema authoring for Umbraco 17+. Define document types as C# classes; the package syncs them into the Umbraco database on startup — no backoffice clicking, no generate step.

```csharp
[DocumentType(Guid: "8f3c1a2b-3e4d-4f5a-b6c7-d8e9f0a1b2c3", Name: "News Article", AllowedAtRoot: true)]
public partial class NewsArticle : PublishedContentModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public NewsArticle(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name: "Headline", Mandatory: true)]
    public string? Headline => this.Value<string>(_publishedValueFallback, "headline");

    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name: "Body")]
    public IHtmlEncodedString? Body => this.Value<IHtmlEncodedString>(_publishedValueFallback, "body");
}
```

Start with [Getting Started](docs/getting-started.md), or jump straight into the [API Reference](api/index.md).
