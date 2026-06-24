# Consid.Umbraco.CodeFirst

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

---

## Using locally (project reference)

While the package is under development and not yet on NuGet, reference it directly from your Umbraco project.

### 1. Add a project reference

In your Umbraco site's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Consid.Umbraco.CodeFirst/src/Consid.Umbraco.CodeFirst/Consid.Umbraco.CodeFirst.csproj" />
</ItemGroup>
```

Use a relative path or an absolute path depending on your repo layout. For example, if both repos sit side-by-side:

```xml
<ProjectReference Include="../../Consid.Umbraco.CodeFirst/src/Consid.Umbraco.CodeFirst/Consid.Umbraco.CodeFirst.csproj" />
```

### 2. Disable ModelsBuilder

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

### 3. Register the package

The package auto-registers via `CodeFirstComposer` (Umbraco's `IComposer` mechanism). Nothing extra required.

If you prefer explicit registration (e.g. to control order), call it in `Program.cs`:

```csharp
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddCodeFirst()   // ← add this
    .Build();
```

---

## Writing document types

### Minimal example

```csharp
using Consid.Umbraco.CodeFirst.Attributes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

[DocumentType(
    Guid: "8f3c1a2b-3e4d-4f5a-b6c7-d8e9f0a1b2c3",
    Name: "News Article",
    Alias: "newsArticle",          // optional — defaults to camelCase class name
    Icon: "icon-newspaper",
    AllowedAtRoot: true)]
[PublishedModel("newsArticle")]
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

### GUIDs

Every document type needs a **stable, explicit GUID**. Generate one with `uuidgen` (macOS/Linux), `New-Guid` (PowerShell), or any online tool. Never change it — it's the stable identity across environments and renames.

### Property editors

| Attribute | Umbraco editor | Notes |
|---|---|---|
| `[TextString]` | Text Box | `MaxLength` optional |
| `[TextArea]` | Text Area | `MaxLength` optional |
| `[RichText]` | Rich Text (Tiptap) | |
| `[Numeric]` | Integer | |
| `[TrueFalse]` | Toggle | |
| `[DatePicker]` | Date Picker | |
| `[Dropdown]` | Dropdown | `AllowMultiple`, `Options = new[]{"a","b"}` |

All share `Name`, `Alias`, `Mandatory`, `Description` parameters.

### Groups (tabs)

Use the `Groups` constants class or any string:

```csharp
[Group(Groups.Content, SortOrder: 0)]    // "Content" tab
[Group(Groups.Settings, SortOrder: 0)]   // "Settings" tab
[Group("Custom Tab", SortOrder: 0)]      // arbitrary tab name
```

Available constants: `Groups.Content`, `Groups.Settings`, `Groups.SEO`, `Groups.Navigation`, `Groups.Media`.

### Allowed children and root

```csharp
[DocumentType(Guid: "...", Name: "Site Root", AllowedAtRoot: true)]
[AllowedChildren(typeof(NewsArticle), typeof(LandingPage))]
public partial class SiteRoot : PublishedContentModel { ... }
```

Child types must also have `[DocumentType]` attributes. The package validates this at startup.

### Alias derivation

If you omit `Alias` on `[DocumentType]` or `[TextString]` etc., the alias is derived by lowercasing the first letter of the class/property name:
- `NewsArticle` → `newsArticle`
- `Headline` → `headline`

---

## Pre-flight validation

Before touching the database, the package runs a validation pass and **aborts with a single aggregated error** if any of the following are found:

- Duplicate document type alias or GUID across classes
- Duplicate property alias within a single class
- `[AllowedChildren]` referencing a type without `[DocumentType]`

Fix all reported errors and restart. The sync never partially applies.

---

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

---

## Roadmap

- [ ] Source generator (removes explicit getter boilerplate)
- [ ] Element types, Block List, Block Grid
- [ ] Compositions (C# interfaces → Umbraco compositions)
- [ ] Template linkage
- [ ] NuGet package release
