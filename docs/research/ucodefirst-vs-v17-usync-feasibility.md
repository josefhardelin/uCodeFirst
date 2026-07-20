# uCodeFirst vs. the Pronomic v17 uSync export — feasibility assessment

**Researched:** 2026-07-20
**Goal:** Determine whether `uCodeFirst`, exactly as it exists in this repo today (no hypothetical extensions), could re-express every entity in a real production uSync export — `/Users/josefhardelin/Code/Consid/Pronomic/PronomicWeb/Pronomic/uSync/v17` — as C# code, such that a fresh sync would reproduce the same Umbraco configuration.

## Verdict: not feasible today — partially feasible at best, with major structural gaps

uCodeFirst can express a genuine subset of the export (compositions, folders, simple content-type/media-type trees, a handful of property editors, one language pair, flat dictionary key/hierarchy). But it is missing, completely, several features the export depends on pervasively:

1. **Culture variance.** 53 of 69 content types in the export (`Variations>Culture`) and their properties vary by culture. uCodeFirst's `ContentTypeSyncEngine`/`MediaTypeSyncEngine` never set `Variations` on a content type or property at all — everything they create is invariant. This alone means the majority of the site's document types cannot be reproduced with correct behavior.
2. **Dictionary translations.** The export's `Dictionary/` folder is exclusively about per-culture translated strings (12 locales per key across 121 files). uCodeFirst's `DictionaryItemSyncEngine` explicitly, by design, never writes translation values — only key/hierarchy.
3. **Property editor coverage.** Of the ~27 distinct data-type editor aliases used in the export, uCodeFirst ships hard-coded support for only 9 (`Umbraco.TextBox`, `TextArea`, `RichText`, `TrueFalse`, `Integer`, `DateTime`, `DropDown.Flexible`, `BlockList`, `BlockGrid`). The single most-used editor in the export, `Umbraco.MultiNodeTreePicker` (12 data types), plus `MediaPicker3`, `ContentPicker`, `MultiUrlPicker`, `Tags`, `Label`, `CheckBoxList`, `RadioButtonList`, `ImageCropper`, `ColorPicker`, `Slider`, `MemberPicker`, `UploadField`, `ListView`, and third-party editors (`UmbracoForms.*`, `Pronomic.PropertyEditorSchema.DAMPickerNew`, `Struct.Umbraco.StructPimPicker`) have no uCodeFirst equivalent — and, critically, **a consuming project cannot add its own**, because the extension point (`DataTypeBase.BuildRecipe`) is declared `internal abstract` with no `InternalsVisibleTo` granted to external assemblies. This is a hard library-level ceiling, not a documentation gap.
4. **Templates.** uCodeFirst can create a template stub tied to a document type's single default/allowed template, but has no concept of the master-page `Parent` hierarchy the export uses throughout (`_Layout` as parent of 6 of 11 templates).
5. **No languages folder in the export at all**, yet the site is visibly multilingual (12 cultures via `Domains/`, culture variance in content types, per-culture dictionary translations) — meaning the real language configuration in this Umbraco instance isn't even coming from uSync's `Languages/` mechanism, it's presumably pre-seeded/managed another way. uCodeFirst's own language support (`LanguageSyncEngine`) is real but narrow: additive-only, single enum, and does not update an already-installed language's fallback/mandatory settings.
6. Other unaddressed features seen in the export: **ListView** configuration on individual content/media types (`blogfolder.config`), **HistoryCleanup** policy blocks, **Thumbnail** field, and **Domains** (hostname → language/root-node mapping) — none of which exist anywhere in uCodeFirst's attribute set or sync engines.

What uCodeFirst *does* line up with cleanly: compositions (interfaces + `[CompositionType]`), backoffice folder trees, `AllowedChildren`, simple icon+color strings, Block List/Grid block-type wiring, and a handful of editors for genuinely simple invariant properties. That subset is real and would round-trip correctly — it's just a minority of this particular site.

---

## 1. Data Types

**Export:** `DataTypes/` contains 106 `.config` files. Aliases actually used (counted via `grep -oh "EditorAlias>[^<]*"` across all files), most‑used first:

| Editor alias | Count | uCodeFirst support? |
|---|---|---|
| `Umbraco.MultiNodeTreePicker` | 12 | No |
| `Umbraco.BlockList` | 10 | Yes (`BlockListDataType.cs`) |
| `Umbraco.BlockGrid` | 9 | Yes (`BlockGridDataType.cs`) |
| `Umbraco.TrueFalse` | 8 | Yes (`TrueFalseDataType.cs`) |
| `Umbraco.Label` | 8 | No |
| `Umbraco.DropDown.Flexible` | 6 | Yes (`DropdownDataType.cs`) |
| `Pronomic.PropertyEditorSchema.DAMPickerNew` (custom) | 6 | No |
| `Umbraco.UploadField` | 5 | No |
| `Umbraco.MediaPicker3` | 5 | No |
| `UmbracoForms.FormPicker` (3rd party) | 3 | No |
| `Umbraco.Tags` | 3 | No |
| `Umbraco.RichText` | 3 | Yes (`RichTextDataType.cs`, alias `Umbraco.RichText`/Tiptap UI) |
| `Umbraco.ListView` | 3 | No |
| `Umbraco.ContentPicker` | 3 | No |
| `Umbraco.RadioButtonList` | 2 | No |
| `Umbraco.MultiUrlPicker` | 2 | No |
| `Umbraco.Integer` | 2 | Yes (`NumericDataType.cs`) |
| `Umbraco.DateTime` | 2 | Yes (`DatePickerDataType.cs`) |
| `Umbraco.CheckBoxList` | 2 | No |
| `Struct.Umbraco.StructPimPicker` (3rd party) | 2 | No |
| `UmbracoForms.ThemePicker` (3rd party) | 1 | No |
| `Umbraco.TextBox` | 1 | Yes (`TextStringDataType.cs`) |
| `Umbraco.TextArea` | 1 | Yes (`TextAreaDataType.cs`) |
| `Umbraco.Slider` | 1 | No |
| `Umbraco.MemberPicker` | 1 | No |
| `Umbraco.ImageCropper` | 1 | No |
| `Umbraco.ColorPicker` | 1 | No |

Sources checked: `DataTypes/blogItemMultinodeTreepicker.config`, `DataTypes/ImageMediaPicker.config`, `DataTypes/LabelString.config`, `DataTypes/CheckboxList.config`, `DataTypes/ImageCropper.config` (opened to confirm real editor aliases, not just filenames), plus the aggregate grep above.

**uCodeFirst today:** the property-editor → data-type mapping logic that CLAUDE.md's architecture summary calls `Sync/EditorRecipeResolver.cs` no longer exists as a standalone file — the design has moved to a per-editor `DataTypeBase` subclass owning its own `BuildRecipe()`. Confirmed inventory of concrete subclasses, each in `src/uCodeFirst/DataTypes/`:
- `TextString.cs` / `TextStringDataType.cs` → `Umbraco.TextBox`
- `TextArea.cs` / `TextAreaDataType.cs` → `Umbraco.TextArea`
- `RichText.cs` / `RichTextDataType.cs` → `Umbraco.RichText` (Tiptap UI)
- `TrueFalse.cs` / `TrueFalseDataType.cs` → `Umbraco.TrueFalse`
- `Numeric.cs` / `NumericDataType.cs` → `Umbraco.Integer` (integer only — no decimal editor)
- `DatePicker.cs` / `DatePickerDataType.cs` → `Umbraco.DateTime`
- `DropdownDataType.cs` → `Umbraco.DropDown.Flexible`
- `BlockListDataType.cs` → `Umbraco.BlockList`
- `BlockGridDataType.cs` → `Umbraco.BlockGrid`

That is the complete, exhaustive set — 9 editors. `DataTypeSyncEngine.cs:50-53` resolves each property's `EditorRecipe` purely by calling `prop.DataType.BuildRecipe(...)` — there is no separate resolver/switch anywhere else in the codebase (confirmed: `find . -iname "*EditorRecipe*"` returns only `Sync/EditorRecipe.cs`, the plain record type, not a resolver).

**Gap and why it's structural, not just incomplete:** `DataTypeBase.BuildRecipe` (`src/uCodeFirst/DataTypes/DataTypeBase.cs:20`) is declared:
```csharp
internal abstract EditorRecipe BuildRecipe(Guid key, string name);
```
`internal` members cannot be overridden from another assembly without `[InternalsVisibleTo]`. `src/uCodeFirst/uCodeFirst.csproj` has no `InternalsVisibleTo` for `Basicv17` or any consumer (confirmed by reading the full `.csproj` — only a `ProjectReference` to `uCodeFirst.Analyzers` and the Umbraco package reference are present). Consequently, a project consuming uCodeFirst as a compiled package **cannot add support for `MediaPicker3`, `ContentPicker`, `MultiUrlPicker`, `Tags`, `Label`, etc. itself** — those 18+ editor aliases used in this export are unreachable without modifying uCodeFirst's own source.

---

## 2. Media Types

**Export:** `MediaTypes/` has 20 `.config` files: built-ins (`file.config`, `folder.config`, `image.config`) plus a custom DAM (digital asset management) hierarchy — `damimage.config`, `damaudio.config`, `damdocument.config`, `damvideo.config`, `damvector.config`, and matching `*folder.config` variants, each with a `<Parent>` pointing at the corresponding Umbraco built-in (e.g. `damimage.config:12` → `<Parent Key="cc07b313-...">Image</Parent>`, composed with `<Composition Key="cc07b313-...">Image</Composition>`).

**uCodeFirst today:** `[MediaType]` (`src/uCodeFirst/Attributes/MediaTypeAttribute.cs`) supports `Name`, `Alias`, `Icon`, `Color`, `Description`, `AllowedAtRoot`, `Folder`, `Compositions` (GUID array), and `External` (marks a stub for an existing built-in type, e.g. Image, that is never itself synced but can be inherited from to become a true tree-child — see `DocumentTypeScanner.GetMediaTypeParentKey`, lines 102-114, and the `BuiltIn/UmbracoImageModel.cs` etc. stub classes). `MediaTypeSyncEngine.cs` creates/updates media types, wires `AllowedChildren` and compositions, and supports parent-via-inheritance exactly matching this export's DAM-under-Image pattern.

This is the closest match of any entity kind in the export: the DAM media types' actual structure (parent = built-in Image/File/Video/Audio, composition = same, custom properties in one property group/tab) is directly expressible with `[MediaType(..., External: false)]` classes inheriting from an `External: true` stub, using `TextArea`/`TrueFalse` for the DAM metadata fields — but only because those DAM media types happen to use only `Umbraco.Label` (not supported — see below) and `Umbraco.TextArea`/`Umbraco.TrueFalse` (supported) for their fields. `Umbraco.Label` fields (e.g. `damAlternative`, `damCreateDate`, `damId`, `damTitle` in `damimage.config`) have no uCodeFirst equivalent — same editor-coverage gap as Section 1.

Media types in the export also carry `Variations>Nothing` uniformly (all invariant) — so, unlike content types, culture variance is not a gap here.

---

## 3. Content Types

**Export:** `ContentTypes/` has 69 `.config` files — a mix of page types (`page.config`, `article.config`, `startpage.config`), element/block types (51 of 69 have `<IsElement>true</IsElement>`), and composition-only types (`fallbacklanguage.config`, `seo.config`, `hero.config`, `menu.config` — used as `<Composition>` targets elsewhere, e.g. `page.config:18-23` lists `fallbackLanguage`, `hero`, `menu`, `sEO` as compositions).

Representative file read in full: `ContentTypes/page.config`. Structural features present:
- `Icon` with trailing color class: `icon-document color-green` — matches uCodeFirst's `BuildIconString` format exactly (`ContentTypeSyncEngine.cs:400-404`).
- `AllowAtRoot`, `Folder` (`Pages`) — supported.
- `Compositions` — supported (`ContentTypeSyncEngine.SyncCompositionsAsync`).
- `DefaultTemplate` + `AllowedTemplates` — in every content type checked, `AllowedTemplates` contains exactly one entry equal to `DefaultTemplate` (confirmed: no `.config` file in the export has more than one `<Template Key=...>` entry). uCodeFirst's `ApplyTemplateAsync` (`ContentTypeSyncEngine.cs:237-270`) only ever sets `AllowedTemplates = [template]` where `template` is the single default — so this specific (single-template) pattern is fully covered, but the engine has no way to express a document type allowing multiple templates beyond its default, should one exist.
- `Variations>Culture` — **53 of 69 content types** use this (`grep -l "Variations>Culture" ContentTypes/*.config | wc -l`); 31 use `Variations>Nothing`. `page.config` itself is `Culture`-varied at both the content-type level and per-property level (`GenericProperty/Variations>Culture`, line 50). uCodeFirst has **zero** references to `Variations`/`ContentVariation` anywhere in `src/uCodeFirst` (confirmed via `grep -rn "Variation" src/uCodeFirst`) — `ContentType`/`PropertyType` objects it constructs are left at Umbraco's default (invariant). No attribute or engine code path can produce a culture-varying content type or property today.
- `ListView` — `blogfolder.config` sets a real (non-zero) `ListView` GUID, enabling a custom list-view configuration on that content type's children listing. No `[DocumentType]` parameter or `ContentTypeSyncEngine` code path sets `IsContainer`/`ListView` (confirmed: `grep -rn "ListView\|IsContainer" src/uCodeFirst` returns nothing).
- `HistoryCleanup` block (`PreventCleanup`, `KeepAllVersionsNewerThanDays`, `KeepLatestVersionPerDayForDays`) appears on every content type in the export (all left at defaults in the sample checked, but the block exists as a real, settable feature) — no uCodeFirst equivalent (confirmed via `grep -rn "HistoryCleanup" src/uCodeFirst`, only match is an unrelated icon constant `ContentTypeIcon.History`).
- `Thumbnail` (`folder.png` in the samples checked) — no corresponding attribute parameter on `[DocumentType]`/`[MediaType]`/`[ElementType]` (only `Icon`/`Color`).

**uCodeFirst today:** `[DocumentType]` (`src/uCodeFirst/Attributes/DocumentTypeAttribute.cs`) covers `Name`, `Alias`, `Icon`, `Color`, `Description`, `AllowedAtRoot`, `Folder`, `DefaultTemplate`. `[ElementType]` is the same minus `AllowedAtRoot`/`DefaultTemplate`. `[CompositionType]` (interfaces) covers `Name`/`Alias`/`Icon`/`Color`/`Description`/`Folder`. `[AllowedChildren(params Type[])]` wires the `AllowedContentTypes` sort list. `ContentTypeScanner`/`ContentTypeSyncEngine` do three passes (create/update, `AllowedChildren`, compositions) matching CLAUDE.md's description. None of these touch `Variations`, `ListView`, `HistoryCleanup`, or `Thumbnail`.

---

## 4. Languages

**Export:** there is **no `Languages/` folder at all** in this uSync export (confirmed: `find v17 -iname "*Languages*"` under top-level entity folders returns nothing; a top-level `find -type d` listing shows only `Content, ContentTypes, DataTypes, Dictionary, Domains, Media, MediaTypes, MemberTypes, RelationTypes, Templates`). Multi-culture configuration is nonetheless clearly present and load-bearing elsewhere:
- `Domains/` — 24 domain files mapping hostnames/paths to specific languages and root content nodes, e.g. `Domains/de_de-de.config`: `<Domain Alias="/de"><Info><Language>de-DE</Language><Root Key="8397861b-...">/Start</Root></Info></Domain>`. Distinct cultures referenced across the 24 domain files: `de-AT, de-CH, de-DE, en-GB, en-US, es-ES, fi-FI, fr-FR, it, ja-JP, nl-NL, sv-SE` (12 cultures).
- Content types' `Variations>Culture` (Section 3) and Dictionary `Translations` (Section 5) both operate per-culture across that same 12-culture set.

Since uSync doesn't export a `Languages/` folder here, the actual `ILanguageService` configuration for this site is presumably managed some other way (pre-seeded migration, manual backoffice setup never captured by this particular export run, etc.) — outside what this export can tell us directly. What's certain is the *site* is genuinely 12-culture multilingual, and any code-first reproduction needs to create all 12 languages correctly (default, mandatory, fallback chain) for the rest of the export (culture-varying content types, dictionary translations, domains) to mean anything.

**uCodeFirst today:** real, working language support exists — `[Languages(DefaultLanguage:)]` on an enum, `[Language(IsoCode:, Fallback:, IsMandatory:)]` on its members, scanned by `DocumentTypeScanner.ScanLanguages` and applied by `LanguageSyncEngine.cs`. It correctly handles fallback chains (with cycle detection in `PreFlightValidator.ValidateLanguages`) and mandatory flags. Demonstrated end-to-end in `samples/Basicv17/Basicv17/Models/Languages.cs` (`en-US` default, `sv-SE` falling back to it). Two real constraints: (a) `CodeFirstSyncService.cs:81` only calls the language engine `if (languageSetDefinitions.Count == 1)` — exactly one `[Languages]` enum is supported per solution, which 12 languages would fit into, so this isn't itself a blocker; (b) `LanguageSyncEngine` is additive-only — "an already-existing language... is never updated" (its own comment, `LanguageSyncEngine.cs:11-13`), so if Umbraco's pre-installed default language differs from what the code declares, that mismatch is silently left alone rather than corrected. Given this export has no `Languages/` folder to diff against in the first place, this is a moot point for *this* export specifically, but would matter for any site that does export one.

**Domains** have no uCodeFirst equivalent whatsoever — confirmed via `grep -rn "IDomainService\|Domain" src/uCodeFirst`, whose only hit is an unrelated `AppDomain.CurrentDomain` call in `CodeFirstStartupHandler.cs:36`. The 24 domain-to-language/root-node mappings in this export cannot be expressed at all.

---

## 5. Dictionary

**Export:** `Dictionary/` has 121 `.config` files. Representative file read in full — `Dictionary/all-languages.config`:
```xml
<Dictionary Key="49a6c9ff-..." Alias="All languages" Level="1">
  <Info><Parent>DocumentRelated</Parent></Info>
  <Translations>
    <Translation Language="de-AT">Alle Sprachen</Translation>
    <Translation Language="de-CH">Alle Sprachen</Translation>
    <Translation Language="de-DE">Alle Sprachen</Translation>
    <Translation Language="en-GB">All languages</Translation>
    <Translation Language="en-US">All languages</Translation>
    <Translation Language="es-ES">Todos los idiomas</Translation>
    <Translation Language="fi-FI">Kaikki kielet</Translation>
    <Translation Language="fr-FR">Toutes les langues</Translation>
    <Translation Language="it"></Translation>
    <Translation Language="ja-JP">すべての言語</Translation>
    <Translation Language="nl-NL">Alle talen</Translation>
    <Translation Language="sv-SE">Alla språk</Translation>
  </Translations>
</Dictionary>
```
Every dictionary item both nests under a parent (`DocumentRelated` here) and carries up to 12 per-culture translated values. `grep -l "<Translation " Dictionary/*.config` confirms this pattern recurs across the folder (checked several: `0-of-1-products.config`, `acceptmessage.config`, `acceptmessageinfo.config`).

**uCodeFirst today:** `[DictionaryItem]` on a `const string` field, nested inside static classes for hierarchy (`src/uCodeFirst/Attributes/DictionaryItemAttribute.cs`), scanned by `DocumentTypeScanner.ScanDictionaryItems`, applied by `DictionaryItemSyncEngine.cs`. Its own top-of-file comment states the design explicitly: *"Code owns dictionary item keys and hierarchy only — never translation values. Existing items... are left completely untouched; only missing items are created."* (`DictionaryItemSyncEngine.cs:9-11`). It genuinely creates the key/parent structure correctly (confirmed against the nesting pattern), but by design **can never populate the `Translations` block** — the entire reason 121 files exist in this export. Demonstrated in `samples/Basicv17/Basicv17/Models/Dictionary/DictionaryKeys.cs` (keys only, no translation values anywhere in the sample).

---

## 6. Templates

**Export:** `Templates/` has 11 `.config` files. Every file is just `Key`/`Alias`/`Name`/`Parent`:
```xml
<!-- Templates/page.config -->
<Template Key="7b3cf4ed-..." Alias="Page" Level="2">
  <Name>Page</Name>
  <Parent>_Layout</Parent>
</Template>
```
6 of 11 templates declare `<Parent>_Layout</Parent>` — checked directly: `errorpage`, `newproductpage`, `page`, `productcategorypage`, `searchpage`, `startpage` have `_Layout` as parent; `_layout`, `article`, `documentlibraryoverviewpage`, `shortcutpage`, `sitemap` have empty `<Parent />`, i.e. top-level/no master. This is a classic Umbraco master-template (`_Layout` → child views) pattern. `page.config` is also referenced back from `ContentTypes/page.config:24-27` as both the `DefaultTemplate` and the sole `AllowedTemplates` entry.

**uCodeFirst today:** `[DocumentType(DefaultTemplate:)]` (a plain string alias) is read by `ContentTypeSyncEngine.ApplyTemplateAsync` (`ContentTypeSyncEngine.cs:237-270`). If the named template doesn't exist, it calls:
```csharp
await _templateService.CreateAsync(def.DefaultTemplate, def.DefaultTemplate, content: null, Constants.Security.SuperUserKey);
```
No master/parent template argument is passed anywhere in this call or anywhere else in the file (confirmed: `grep -n "CreateAsync" ContentTypeSyncEngine.cs` shows exactly one `ITemplateService.CreateAsync` call, with no parent parameter). So: uCodeFirst can create a flat template stub tied 1:1 to a document type's default template, but has **no way to declare or wire the `_Layout` master-template hierarchy** this export uses for 7 of its 11 templates, and no way to express multiple `AllowedTemplates` beyond the single default (moot here since the export never uses more than one anyway — see Section 3).

---

## 7. Other entity kinds present in the export (out of the requested scope, noted for completeness)

- **`MemberTypes/`** — 1 file (`Member.config`, the built-in). uCodeFirst has no `[MemberType]` attribute or `IMemberTypeService` usage anywhere (only `DocumentType`/`ElementType`/`CompositionType`/`MediaType`/`Language`/`DictionaryItem` attributes exist in `src/uCodeFirst/Attributes/`).
- **`RelationTypes/`** — 2 files (`umbForm`, `umbMember`, both from the Umbraco Forms/Members ecosystem). No uCodeFirst equivalent.
- **`Content/`** (454 files) and **`Media/`** (1097 files) — actual content/media items, not schema; out of scope for a schema-sync tool like uCodeFirst by design, and uSync itself treats these as a separate concern from the entity kinds above.
- **`Domains/`** — covered under Languages (Section 4) since it's inseparable from the site's multi-culture story.

---

## Consolidated gaps

Each gap lists the export evidence and the uCodeFirst source checked to confirm the absence.

1. **Culture variance (content types & properties) — not supported at all.**
   Evidence: 53/69 files in `ContentTypes/*.config` have `<Variations>Culture</Variations>` (e.g. `ContentTypes/page.config:10` and its property at line 50).
   Confirmed absent: `grep -rn "Variation" src/uCodeFirst` matches only unrelated `CultureInfo` usage inside `LanguageSyncEngine.cs`; `ContentTypeSyncEngine.cs` and `MediaTypeSyncEngine.cs` never set `ContentType.Variations`/`PropertyType.Variations`.

2. **Dictionary per-culture translations — explicitly excluded by design.**
   Evidence: `Dictionary/all-languages.config` (12 `<Translation>` values), pattern repeated across all 121 files in `Dictionary/`.
   Confirmed absent: `src/uCodeFirst/Sync/DictionaryItemSyncEngine.cs:9-11` (code comment) and its `EnsureItemAsync` method (lines 37-62), which never touches translation values, only `DictionaryItem(parentKey, itemKey)`.

3. **Most property-editor aliases used in the export have no uCodeFirst data type, and consumers cannot add their own.**
   Evidence: editor-alias table in Section 1 — 18 of 27 distinct aliases used in `DataTypes/*.config` are unsupported (`Umbraco.MultiNodeTreePicker`, `MediaPicker3`, `ContentPicker`, `MultiUrlPicker`, `Tags`, `Label`, `CheckBoxList`, `RadioButtonList`, `ImageCropper`, `ColorPicker`, `Slider`, `MemberPicker`, `UploadField`, `ListView`, plus 4 third-party aliases).
   Confirmed absent/closed: only 9 `DataTypeBase` subclasses exist in `src/uCodeFirst/DataTypes/` (enumerated in Section 1); `DataTypeBase.BuildRecipe` is `internal abstract` (`DataTypeBase.cs:20`) with no `InternalsVisibleTo` in `src/uCodeFirst/uCodeFirst.csproj`, so external consumers cannot implement new ones.

4. **Template master/parent hierarchy — not supported.**
   Evidence: `Templates/page.config` (`<Parent>_Layout</Parent>`); 6 of 11 files in `Templates/` declare a non-empty `Parent`.
   Confirmed absent: `ContentTypeSyncEngine.cs:250-254`, the sole `ITemplateService.CreateAsync` call, passes no parent/master argument.

5. **`ListView` configuration on content/media types — not supported.**
   Evidence: `ContentTypes/blogfolder.config` sets a non-default `<ListView>` GUID.
   Confirmed absent: `grep -rn "ListView\|IsContainer" src/uCodeFirst` returns no matches.

6. **`HistoryCleanup` policy — not supported.**
   Evidence: every file in `ContentTypes/*.config` carries a `<HistoryCleanup>` block (e.g. `page.config:12-16`).
   Confirmed absent: `grep -rn "HistoryCleanup" src/uCodeFirst` matches only an unrelated icon constant in `ContentTypeIcon.cs`.

7. **Domains (hostname/path → language + root node) — no concept at all.**
   Evidence: `Domains/` — 24 files spanning 12 cultures (e.g. `Domains/de_de-de.config`).
   Confirmed absent: `grep -rn "IDomainService\|Domain" src/uCodeFirst` has no relevant hits.

8. **No `Languages/` folder in this export to compare against**, despite the site clearly being 12-culture multilingual (via `Domains/`, content-type `Variations`, and Dictionary `Translations`) — meaning even a fully-capable code-first tool would need language data sourced from somewhere other than this uSync export. uCodeFirst's own language engine (`LanguageSyncEngine.cs`) is real and correctly handles default/mandatory/fallback, demonstrated in `samples/Basicv17/Basicv17/Models/Languages.cs`, but is additive-only (never updates an already-installed language) and limited to exactly one `[Languages]` enum per solution (`CodeFirstSyncService.cs:81`).

9. **`Thumbnail` field on content/media/element types — not supported.**
   Evidence: present on essentially every `ContentTypes/*.config` and `MediaTypes/*.config` file checked (e.g. `page.config:6`, `damimage.config:6`).
   Confirmed absent: `[DocumentType]`, `[MediaType]`, `[ElementType]` (`src/uCodeFirst/Attributes/*.cs`) have no `Thumbnail` parameter.

10. **Member types and relation types — no concept at all** (minor, only 1 and 2 files respectively in the export). Confirmed absent: no `[MemberType]` attribute or `IMemberTypeService`/`IRelationService` usage anywhere in `src/uCodeFirst`.

11. **No automated test coverage exists yet to independently corroborate any of the above** — `tests/uCodeFirst.Tests/` currently contains only `uCodeFirst.Tests.csproj`, `bin/`, and `obj/` (confirmed via directory listing); all findings in this document come from direct reading of `src/uCodeFirst` and the uSync export, not from running or observing the sync pipeline.
