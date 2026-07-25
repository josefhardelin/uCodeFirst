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

- **Element types + Block List + Block Grid** — `[ElementType]` attribute (element types are content types used as Block List/Grid item content, not standalone tree nodes); `BlockListDataType`/`BlockGridDataType` config classes wire GUID cross-refs to the element types allowed in a block
- **Compositions & inheritance** — `[CompositionType]` interfaces; `ContentTypeSyncEngine`'s three-pass sync (create/update types and folders → wire `AllowedChildren` → wire compositions); `PreFlightValidator` checks property-alias collisions across composed types
- **Roslyn analyzer** — `uCodeFirst.Analyzers` project, `UCF001` (`MissingGuidAnalyzer`): build error when a `[DocumentType]`/`[ElementType]`/`[MediaType]` GUID is missing, with a code fixer that generates one
- **Code-first data type classes** — `DataTypeBase` is the abstract base every property-editor attribute (`[TextString]`, `[RichText]`, etc.) derives from; paired with `[DataType]` it's also a public extensibility point, letting consumers author their own property-editor attributes for editors beyond the built-in seven
- **Folder support** — `Folder` param on `[DocumentType]`; `EntityContainer` hierarchy with deterministic GUIDs (MD5 of path) for idempotency
- **Template linking** — `DefaultTemplate` param on `[DocumentType]`; looks up template by alias via `ITemplateService`, creates DB entry if missing, wires `AllowedTemplates` + `DefaultTemplateId`
- **View scaffolding** — test project has `@inherits UmbracoViewPage<T>` views, `_Layout`, `_ViewImports`
- **Media types** — `[MediaType]` attribute, `MediaTypeDefinition`, `MediaTypeSyncEngine`; scanner, data-type sync, and pre-flight validation all extended to cover media types
- **Dictionary items** — `[DictionaryItem]` attribute (field-targeted, on `const string` fields using `nameof` so the C# identifier and the Umbraco `ItemKey` are always identical); `DictionaryItemDefinition`, `DictionaryItemSyncEngine`. Code owns keys/hierarchy only — nested static classes become real parent dictionary items, translations are never written by sync (backoffice/uSync-owned), and existing items are never touched or deleted. `PreFlightValidator` rejects duplicate `ItemKey`s across the whole scan (leaves and auto-created parents share one flat Umbraco namespace)
- **Languages** — one enum per project carries `[Languages(DefaultLanguage: ...)]`; individual members carry `[Language(IsoCode: "...", Fallback = ..., IsMandatory = ...)]` and are skipped if unattributed (so the enum can hold a sentinel/`None` member or unrelated values). `LanguageSetDefinition`/`LanguageDefinition`, `LanguageSyncEngine`. The enum is the full language roster for the site (existing + new — `Fallback`/`DefaultLanguage` are compile-time-checked references to sibling members, boxed as `object` since an attribute can't be generic over "the enum it's applied to"). Sync is create-only: `GetAsync(isoCode)` is checked first and an existing language (including the built-in `en-US` from installation) is never updated, only ensured to exist; creation order recursively resolves `Fallback` dependencies first. `CultureName` is never authored — derived from `CultureInfo.GetCultureInfo(isoCode)` at creation time. `PreFlightValidator` rejects more than one `[Languages]`-marked enum, a `DefaultLanguage`/`Fallback` that isn't a `[Language]`-attributed sibling, duplicate ISO codes, and fallback cycles/self-references — all pure, offline reflection checks
- **Backoffice dry-run dashboard** — `CodeFirstSyncService.ComputePlanAsync` extracts the plan computation (content types + media types, same scope as the startup dry-run log) into a serializable `CodeFirstPlanResult` (creates/updates by alias, pruned properties/groups, effective `Strategy`, `Enabled`, and a UTC generation timestamp); `PlanAsync` (the startup path) now just calls it and logs, unchanged. A new Management API controller (`Api/PlanCodeFirstController`, `GET /umbraco/management/api/v1/code-first/plan`) exposes a live computation of the same DTO, authenticated via the inherited `ManagementApiControllerBase` backoffice policies, working regardless of `Enabled`. A Lit dashboard (`wwwroot/App_Plugins/uCodeFirst/plan-dashboard.element.js` + `umbraco-package.json`) registers under the Settings section (`Umb.Section.Settings`), fetches the endpoint on connect, renders creates/updates/prunes with the `NonDestructive`-pruning caveat, and has a "Run dry-run now" button for a fresh on-demand computation. Shipping static backoffice assets required switching `uCodeFirst.csproj`'s SDK to `Microsoft.NET.Sdk.Razor` (Static Web Assets packaging) and adding a `Umbraco.Cms.Api.Management` package reference.
- **Test project migrated from xUnit to NUnit** — `tests/uCodeFirst.Tests` now references `NUnit`/`NUnit3TestAdapter` instead of xUnit, clearing the way to later host Umbraco's own SQLite-backed `Umbraco.Cms.Tests.Integration` suite (NUnit-only) in the same project without a split framework. See `docs/research/testing-strategy.md`.

**Package location:** `~/Code/Consid/Consid.Umbraco.CodeFirst`
**Test project:** `~/Code/Consid/TestProjects/UmbracoTCodeFIrst` (Umbraco 17.4.2, net10.0)

---

## Roadmap — deferred, in priority order

1. **Unit tests for `DocumentTypeScanner`, `PreFlightValidator`, and the sync engines** — both scanner and
   validator are pure logic with zero Umbraco dependency (plain reflection over records), so they're
   testable today with in-memory fixture assemblies and no mocking. Goal: replace the "boot Basicv17,
   click through the backoffice" verification loop with a fast local test run for scanner/validator
   behavior. See `docs/research/testing-strategy.md` for sketches (duplicate alias/GUID, dangling
   `[AllowedChildren]` refs, composition property exclusion, dictionary parent-chain resolution). Focused
   validator/engine tests now exist for culture variance, template cycles, and language update-on-drift
   (added alongside those features), but scanner coverage and broader sync-engine coverage
   (`ContentTypeSyncEngine`, `MediaTypeSyncEngine`, `DataTypeSyncEngine`) are still open — see
   `docs/research/ucodefirst-vs-v17-usync-status.md` gap #11.

2. **Configured pickers with dynamic root** — the Tier-1 instance-reference solution (Q2).

3. **Member types & relation types.** (Media types ✓ done, Dictionary items ✓ done, Languages ✓ done — see
   above.) No export evidence of anything custom to reproduce (built-in `Member` type + 2 Umbraco
   Forms/Members-ecosystem relation types only), but member types are a different domain (member/auth
   schema, not content schema) that would need its own design pass if a real need shows up. See
   `docs/research/ucodefirst-vs-v17-usync-status.md` gap #10.

4. **Dictionary item coverage dashboard** — backoffice screen showing which keys are code-grounded vs.
   backoffice-only, and which have translations for which cultures. Split out of the dictionary items
   work above; needs its own scoping (Umbraco dashboards are a Lit/web-component + package-manifest
   registration, not part of the sync pipeline).

5. **Content seeding** — deterministic-GUID singleton nodes (Tier-2 picker answer, Q2).

6. **Segment variance** — culture variance shipped (`VariesByCulture` on `[DocumentType]`/`[ElementType]`
   and per-property on `DataTypeBase`), segment/culture+segment variation is still open.

7. **HistoryCleanup policy** — `PreventCleanup`/`KeepAllVersionsNewerThanDays`/`KeepLatestVersionPerDayForDays`
    on content types. No export evidence of anyone customizing it (every content type in the reference
    export is at Umbraco's default), so not urgent — revisit if a concrete need appears. See
    `docs/research/ucodefirst-vs-v17-usync-status.md` gap #6.

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

**Dictionary items:**

```csharp
public static class DictionaryKeys
{
    public static class Buttons               // nested static class = real parent DictionaryItem
    {
        [DictionaryItem]
        public const string Submit = nameof(Submit);   // ItemKey = "Submit"

        [DictionaryItem]
        public const string Cancel = nameof(Cancel);
    }

    [DictionaryItem]
    public const string SiteTitle = nameof(SiteTitle); // root-level item, no nesting required
}

// Razor
@Umbraco.GetDictionaryValue(DictionaryKeys.Buttons.Submit)
```

The `nameof` value keeps the C# identifier and the Umbraco `ItemKey` identical (rename-safe, no
string literal to keep in sync). Sync only creates missing keys — it never writes translation
values and never touches an existing item.

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
