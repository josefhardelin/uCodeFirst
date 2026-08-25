# uCodeFirst

Code-first schema authoring for Umbraco 17+. Define document types as C# classes; the package syncs them into the Umbraco database on startup — no backoffice clicking, no generate step.

```csharp
[DocumentType("News Article", AllowedAtRoot: true, Guid = "8f3c1a2b-3e4d-4f5a-b6c7-d8e9f0a1b2c3")]
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

---

## Installation

Requires Umbraco 17.4.2+ and .NET 10.

### 1. Add the package

```bash
dotnet add package Our.Umbraco.CodeFirst --prerelease
```

`--prerelease` is required while the package is in beta.

> **The NuGet id is `Our.Umbraco.CodeFirst`, but the namespace is `uCodeFirst`.**
> The shorter id was already taken on nuget.org by an unrelated package, so the
> published id follows the `Our.Umbraco.*` convention the Umbraco community uses
> for third-party packages. Nothing changes in your code — you still write
> `using uCodeFirst;`.

<details>
<summary>Or reference the project directly (contributing, or tracking <code>main</code>)</summary>

In your Umbraco site's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/uCodeFirst/src/uCodeFirst/uCodeFirst.csproj" />
</ItemGroup>
```

Use a relative or absolute path depending on your repo layout. For example, if both repos sit side-by-side:

```xml
<ProjectReference Include="../../uCodeFirst/src/uCodeFirst/uCodeFirst.csproj" />
```

This is what `samples/Basicv17` does.

</details>

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

### Versioning

The **major version tracks the Umbraco major** it targets — `17.x` → Umbraco 17 — following the
convention uSync and other Umbraco libraries use. Minor and patch are this package's own; `17.3.0`
would mean "3rd feature release for the Umbraco 17 line", not "built for Umbraco 17.3".

So pick the major matching your Umbraco version. The exact minimum (currently Umbraco 17.4.2) is
declared as a normal NuGet dependency range and resolved for you on install.

---

## Writing document types

### Minimal example

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

### GUIDs

Every document type needs a **stable, explicit GUID**, set via `Guid = "..."` (a settable property, not a constructor argument). Easiest: leave it unset — the `UCF001` Roslyn analyzer flags the missing GUID as a build error with a code fixer that generates one for you. Or generate one yourself with `uuidgen` (macOS/Linux), `New-Guid` (PowerShell), or any online tool. Never change it once set — it's the stable identity across environments and renames.

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
[DocumentType("Site Root", AllowedAtRoot: true, Guid = "...")]
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

## AI coding agent skill

`samples/Basicv17/.claude/skills/ucodefirst/` ships a [Claude Code skill](https://code.claude.com/docs/en/skills)
that bridges familiar Umbraco backoffice concepts to uCodeFirst's C# attribute API and walks an agent
through scaffolding a new document/element type end-to-end (class, properties, template, view) — assuming
Umbraco fluency already, teaching only the code-first delta. `samples/Basicv17/AGENTS.md` points other
agents (Cursor, Copilot, etc.) at the same file.

Copy `.claude/skills/ucodefirst/` (and `AGENTS.md`, if useful) into your own project to get the same
guidance there. There's no automatic delivery via the NuGet package — see
`docs/research/nuget-agent-skills-delivery.md` for why (no package manager, for any AI coding tool,
auto-populates agent config today; every real mechanism needs an explicit action in the consuming repo).

---

## Roadmap

- [x] Element types, Block List, Block Grid
- [x] Compositions (C# interfaces → Umbraco compositions)
- [x] Template linkage
- [x] Code-first data type classes (`DataTypeBase` hierarchy, `[DataType]` attribute)
- [x] Roslyn analyzer (UCF001 — build error on missing GUID, with code fixer)
- [x] Media types
- [x] Dictionary items (keys/hierarchy only — code owns structure, translations are backoffice/uSync-owned)
- [x] Languages
- [x] Backoffice dry-run dashboard (Settings-section Lit dashboard showing the live create/update/prune plan, with a manual "run dry-run now" trigger)
- [ ] Dictionary item coverage dashboard (backoffice screen showing code-grounded vs. backoffice-only keys, translation status per culture)
- [ ] member types
- [ ] Property type validation — UCF002 analyzer + `PreFlightValidator` check that the C# property return type matches the data type (e.g. `[TrueFalse]` must be on a `bool?`)
- [ ] Source generator (removes explicit getter boilerplate)
- [ ] NuGet package release
