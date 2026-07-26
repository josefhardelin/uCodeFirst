# Getting Started

## 1. Add a project reference

While the package is under development and not yet on NuGet, reference it directly from your Umbraco project's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/uCodeFirst/src/uCodeFirst/uCodeFirst.csproj" />
</ItemGroup>
```

## 2. Disable ModelsBuilder

In `appsettings.json`, turn off ModelsBuilder — your classes are the models:

```json
{
  "Umbraco": {
    "CMS": {
      "ModelsBuilder": {
        "ModelsMode": "Nothing"
      }
    }
  }
}
```

## 3. Register the package

The package auto-registers via `CodeFirstComposer` (Umbraco's `IComposer` mechanism). Nothing extra required — though see [Dev vs production](#dev-vs-production) below before you register it unconditionally.

## Minimal example

```csharp
using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

[DocumentType(
    "News Article",
    Alias: "newsArticle",          // optional — defaults to camelCase class name
    Icon: "icon-newspaper",
    AllowedAtRoot: true,
    Guid = "8f3c1a2b-3e4d-4f5a-b6c7-d8e9f0a1b2c3")]
[PublishedModel("newsArticle")]
public partial class NewsArticle : PublishedContentModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public NewsArticle(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Headline", Mandatory = true)]
    public string? Headline => this.Value<string>(_publishedValueFallback, "headline");

    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name = "Body")]
    public IHtmlEncodedString? Body => this.Value<IHtmlEncodedString>(_publishedValueFallback, "body");
}
```

Every document type needs a **stable, explicit GUID**, set via `Guid = "..."` (a settable property, not a constructor argument). Leave it unset and the `UCF001` Roslyn analyzer will flag it as a build error with a code fixer that generates one, or generate one yourself with `uuidgen` (macOS/Linux) or `New-Guid` (PowerShell). Never change it once set — it's the stable identity across environments and renames.

## Property editors

All property-editor attributes live under the `uCodeFirst.DataTypes` namespace and share `Name`, `Alias`, `Mandatory`, `Description` parameters. See the [API Reference](../api/index.md) for the full set and their individual options.

## Groups (tabs)

```csharp
[Group(Groups.Content, SortOrder: 0)]    // "Content" tab
[Group(Groups.Settings, SortOrder: 0)]   // "Settings" tab
[Group("Custom Tab", SortOrder: 0)]      // arbitrary tab name
```

## Allowed children and root

```csharp
[DocumentType("Site Root", AllowedAtRoot: true, Guid = "...")]
[AllowedChildren(typeof(NewsArticle), typeof(LandingPage))]
public partial class SiteRoot : PublishedContentModel { ... }
```

Child types must also carry `[DocumentType]`. The package validates this at startup.

## Alias derivation

If you omit `Alias` on `[DocumentType]`, `[TextString]`, etc., the alias is derived by lowercasing the first letter of the class/property name:

- `NewsArticle` → `newsArticle`
- `Headline` → `headline`

## Pre-flight validation

Before touching the database, the package runs a validation pass and **aborts with a single aggregated error** if any of the following are found:

- Duplicate document type alias or GUID across classes
- Duplicate property alias within a single class
- `[AllowedChildren]` referencing a type without `[DocumentType]`

Fix all reported errors and restart. The sync never partially applies.

## Dev vs production

| Environment | Code-first | Schema promotion |
|---|---|---|
| **Development** | Runs on every startup, syncs code → DB | n/a |
| **Production** | **Disable** (set `AddCodeFirst` to env-guarded) | Use uSync to import `.uSync` files generated from dev |

Recommended `Program.cs` pattern:

```csharp
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi();

if (builder.Environment.IsDevelopment())
    umbracoBuilder.AddCodeFirst();

umbracoBuilder.Build();
```

This keeps production schema changes deliberate and uSync-controlled.
