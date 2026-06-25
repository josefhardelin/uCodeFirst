# MVP and roadmap

## MVP — the minimal vertical that delivers the loop

The smallest slice that proves **write class → start site → query it** end-to-end and is genuinely usable.

- **Document types** from a class: `[DocumentType]` with GUID, alias, name, icon, description.
- **Properties with built-in simple editors** via dedicated attributes (textstring, textarea, rich text,
  numeric, true/false, date, dropdown) → each resolves to a **shared, deduplicated, code-owned** data
  type (Q9).
- **Tabs/groups, sort order, name, description, mandatory, basic validation** via attributes, with a
  `Groups` constants class.
- **Startup sync** (development) through `IContentTypeService` / `IDataTypeService`.
- **Class is the runtime model** (`[PublishedModel]`, explicit getters; ModelsBuilder off).
- **Basic structure:** allow-at-root, allowed child types referenced by `typeof`.
- **Pre-flight validation** (Q8 collision handling) before any DB write.
- **Deployment:** dev auto-sync + uSync export; prod = uSync import only (Q7).

---

## Status

### Done ✓

**MVP core**
- All 7 editor attributes: `[TextString]`, `[TextArea]`, `[RichText]`, `[Numeric]`, `[TrueFalse]`, `[DatePicker]`, `[Dropdown]`
- `[DocumentType(Guid, Name, Alias?, Icon?, Description?, AllowedAtRoot, Folder?, DefaultTemplate?)]`
- `[AllowedChildren(params Type[])]`
- `[Group(name, SortOrder)]` + `Groups` constants class
- `DocumentTypeScanner` — assembly scan
- `PreFlightValidator` — duplicate alias/GUID, unresolved refs
- `DataTypeSyncEngine` — creates/updates code-owned, deduplicated data types
- `ContentTypeSyncEngine` — two-pass sync (create structure → wire AllowedChildren)
- `CodeFirstSyncService` — orchestrates everything
- `CodeFirstStartupHandler` — fires on `UmbracoApplicationStartedNotification`, skipped if `RuntimeLevel != Run`
- `CodeFirstComposer` — auto-registered `IComposer`
- `AddCodeFirst()` extension on `IUmbracoBuilder`

**Extras shipped with MVP**
- **Folder support** — `Folder` param on `[DocumentType]`; `EntityContainer` hierarchy with deterministic GUIDs (MD5 of path) for idempotency
- **Template linking** — `DefaultTemplate` param on `[DocumentType]`; looks up template by alias via `ITemplateService`, creates DB entry if missing, wires `AllowedTemplates` + `DefaultTemplateId`
- **View scaffolding** — test project has `@inherits UmbracoViewPage<T>` views, `_Layout`, `_ViewImports`

**Package location:** `~/Code/Consid/Consid.Umbraco.CodeFirst`
**Test project:** `~/Code/Consid/TestProjects/UmbracoTCodeFIrst` (Umbraco 17.4.2, net10.0)

---

## Roadmap — deferred, in priority order

1. **Element types + Block List + Block Grid** — the high-value vision; nested content patterns.
   Needs: `[ElementType]` attribute, Block List/Grid data type config, GUID cross-refs to element types.

2. **Compositions & inheritance** — C# interfaces → Umbraco compositions (mixins); base class → doctype
   inheritance (single parent). Validation must check property-alias collisions across composed types.

3. **Configured pickers with dynamic root** — the Tier-1 instance-reference solution (Q2).

4. **Media types, member types, dictionary items, languages.**

5. **Content seeding** — deterministic-GUID singleton nodes (Tier-2 picker answer, Q2).

6. **Variants** — culture/segment variation.

7. **Native production sync safety** — dry-run/preview, destructive-change gating (the parts uSync covers
   for us in the MVP).

8. **Source generator** — removes `_publishedValueFallback` field and `Value<T>` getter boilerplate.
   Nice to have, but the manual pattern is workable and a generator adds build-time complexity.

---

## Architecture sketch

```
┌─────────────────────────────────────────────────────────────┐
│ Your project (source of truth)                              │
│   [DocumentType] / [PublishedModel] C# classes              │
│   + client assets (umbraco-package.json, css) — hand-authored│
└───────────────┬─────────────────────────────────────────────┘
                │ discovery (assembly scan for [DocumentType])
                ▼
        ┌───────────────┐   pre-flight validation
        │ Code-first     │   (dup alias / dup GUID / reserved
        │ sync engine    │    names / unresolved typeof) → abort
        └──────┬─────────┘    on any error, before DB writes
   DEV only    │ applies via public services
               ▼
   IContentTypeService / IDataTypeService / ITemplateService
               │
               ▼
        Local Umbraco DB ──► uSync export ──► .uSync files (committed)
                                                   │
                                                   ▼  (deploy)
                                          PROD: uSync import
                                          (code-first disabled)
```

At runtime, `IPublishedModelFactory` + `[PublishedModel]` make the same classes the strongly-typed
models — no ModelsBuilder.

---

## Authoring API (current state)

```csharp
[DocumentType(
    Guid: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    Name: "Start Page",
    Icon: "icon-home",
    AllowedAtRoot: true,
    Folder: "Pages",
    DefaultTemplate: "startPage")]
[AllowedChildren(typeof(NewsArticle))]
[PublishedModel("startPage")]
public partial class StartPage : PublishedContentModel
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public StartPage(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name: "Headline", Mandatory: true)]
    public string? Headline => this.Value<string>(_publishedValueFallback, "headline");

    [Group(Groups.Content, SortOrder: 1)]
    [RichText(Name: "Body")]
    public IHtmlEncodedString? Body => this.Value<IHtmlEncodedString>(_publishedValueFallback, "body");
}
```

**Note on `Value<T>`:** extension method in `Umbraco.Extensions`; requires `IPublishedValueFallback` as
explicit second parameter. Base class stores it `private`, so subclasses must capture it themselves.

---

## Known constraints / gotchas

- **Namespace collision**: `Consid.Umbraco.CodeFirst` shares the `Umbraco` segment → use
  `using UmbConstants = global::Umbraco.Cms.Core.Constants;` pattern throughout `Sync/`.
- **SQLite connection string**: `Data Source=|DataDirectory|/Umbraco.sqlite.db;Cache=Shared;Foreign Keys=True;Pooling=True` —
  `Pragma Journal Mode` keyword is NOT valid for `Microsoft.Data.Sqlite`.
- **RuntimeLevel check**: sync skips if `Level != Run` — prevents crash on fresh install before DB exists.
- **HTTPS required**: OpenIddict (Umbraco 17 auth) requires HTTPS — run with `--launch-profile https`.
- **Hot reload**: does NOT trigger sync. Schema changes require a full restart. `.cshtml` views hot-reload fine.
- **`PropertyGroup(isPublishing: true)`** with `Type = PropertyGroupType.Tab` required for v14+ backoffice tabs.
- **Template content**: when creating a template DB entry via `ITemplateService.CreateAsync(name, alias, content: null, ...)`,
  Umbraco picks up the existing `.cshtml` file from disk — the `content` parameter only seeds the file if absent.

---

## Feasibility — write-side API verified (target: Umbraco 17.4.2)

Confirmed against the `release-17.4.2` source that the MVP can be built entirely on **public** services
and domain models — no private APIs:

- **Data types:** `IDataTypeService.CreateAsync(IDataType, userKey)` / `UpdateAsync` / `GetAsync(name|guid)` / `GetByEditorAliasAsync`.
- **Content types:** `IContentTypeService.CreateAsync` / `UpdateAsync` + `CreateContainer` for folders. `IContentTypeBase` exposes
  `AddPropertyGroup` and `AddPropertyType`.
- **Properties:** `PropertyType` constructor taking `IDataType` (`new PropertyType(shortStringHelper, dataType, alias)`).
- **Templates:** `ITemplateService.GetAsync(alias)` + `CreateAsync(name, alias, content, userKey)` — `IFileService` template methods are `[Obsolete]` in v17.

**Two-pass sync:** AllowedChildren wired in a second pass to avoid ordering issues (a type may reference
another type that hasn't been created yet in the same sync run).

> Note: the v18-targeted obsoletes (e.g. `IDataTypeService.Save`) are still present in 17.4.2 — we use
> the `CreateAsync`/`UpdateAsync` APIs throughout to stay forward-compatible.
