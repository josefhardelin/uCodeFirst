# uCodeFirst vs. v17 uSync export — gap status tracker

Tracks decisions on each gap identified in [`ucodefirst-vs-v17-usync-feasibility.md`](./ucodefirst-vs-v17-usync-feasibility.md).

Statuses: **Not decided** · **Skipped** · **Postponed** · **Done**

| # | Gap | Status |
|---|---|---|
| 1 | Culture variance (content types & properties) | Done |
| 2 | Dictionary per-culture translations | Skipped |
| 3 | Property editor coverage + `BuildRecipe` extensibility ceiling (`internal`, no `InternalsVisibleTo`) | Done |
| 4 | Template master/parent hierarchy | Done |
| 5 | `ListView` configuration on content/media types | Done |
| 6 | `HistoryCleanup` policy block | Postponed |
| 7 | Domains (hostname/path → language + root node) | Skipped |
| 8 | Language engine limitations (additive-only; single `[Languages]` enum per solution) | Done |
| 9 | `Thumbnail` field on content/media/element types | Skipped |
| 10 | Member types and relation types (no concept at all) | Postponed |
| 11 | No automated test coverage in `tests/uCodeFirst.Tests/` | Postponed |

## Decision log

### 1. Culture variance — Implement
- `[DocumentType]` and `[ElementType]` gain `bool VariesByCulture = false`.
- `DataTypeBase` (base of all property-editor attributes) gains `bool VariesByCulture = false` for per-property control.
- `[MediaType]` is out of scope (export shows media types uniformly invariant).
- Segment variance is out of scope — only Culture/Nothing, matching the export.
- `PreFlightValidator` must reject a property with `VariesByCulture: true` on a content/element type with `VariesByCulture: false`, aggregated with existing pre-flight errors.
- `ContentTypeSyncEngine` must set `ContentType.Variations`/`PropertyType.Variations` accordingly on create/update.
- Sample project (`samples/Basicv17`) must be updated to demonstrate the new params per CLAUDE.md.

### 2. Dictionary per-culture translations — Skipped
- Keeping the existing design: code owns dictionary item keys/hierarchy only. Translation values are a translator/content-editor concern, not a code-first schema concern (analogous to why Content/Media items are out of scope). No change.

### 3. Property editor coverage + BuildRecipe extensibility — Implement
- Part A: change `DataTypeBase.BuildRecipe` from `internal abstract` to `public abstract` so external consumers can add their own editors without forking uCodeFirst.
- Part B: add all 13 remaining native Umbraco editor aliases as built-in `DataTypeBase` subclasses: MultiNodeTreePicker, Label, UploadField, MediaPicker3, Tags, ContentPicker, RadioButtonList, MultiUrlPicker, CheckBoxList, Slider, MemberPicker, ImageCropper, ColorPicker.
- Explicitly excluded: ListView (tracked separately as gap #5), and third-party aliases (UmbracoForms.*, Pronomic.PropertyEditorSchema.DAMPickerNew, Struct.Umbraco.StructPimPicker) — those are exactly what the Part A extensibility fix exists to unblock, not something a generic library should hardcode.
- Queued behind gap #1 (both touch `DataTypeBase.cs`) to avoid concurrent-edit collisions.

### 4. Template master/parent hierarchy — Implement
- Dedicated `[Template(Alias)]` attribute applied to enum members (mirrors the existing `[Languages]`/`[Language]` pattern), with a `Master` property referencing another sibling enum member for the parent/master template relationship.
- `[DocumentType]`/`[ElementType]`'s existing `DefaultTemplate` string param is unchanged — it resolves against templates registered via `[Template]` instead of always creating a flat stub.
- Needs: `DocumentTypeScanner.ScanTemplates()`, cycle-detection validation in `PreFlightValidator` (same pattern as `ValidateLanguages`), and wiring in `ContentTypeSyncEngine.ApplyTemplateAsync` (or a new small engine) to set each template's master via `ITemplateService` after creation.

### 5. ListView — Implement
- Add `bool IsContainer = false` (and optionally a data-type override) to `[DocumentType]`/`[MediaType]`/`[ElementType]`, defaulting to Umbraco's built-in "List View - Content"/"List View - Media" data type when true and no override given.
- Export evidence: almost all content types have `<ListView>00000000-...</ListView>` (unset); only 2/69 have a real GUID.
- Queued behind gap #4 (both touch `ContentTypeSyncEngine.cs`) to avoid concurrent-edit collisions.

### 6. HistoryCleanup — Postponed
- Checked the export: 0/69 content types override the defaults (`PreventCleanup=False`, day-count fields empty) — every file is already at Umbraco's default, so this gap doesn't affect reproducing this specific export. Real gap for sites that do customize retention policy; revisit if a concrete need appears.

### 7. Domains — Skipped
- Absolute hostnames were never on the table (environment-specific, doesn't belong in committed code). Considered a relative-path-only version, but an `IDomain` in Umbraco always binds to a specific content *instance* (`rootContentId`), not a content *type* — and uCodeFirst is schema-only, never creating or referencing content items (the same reason `Content/`/`Media/` are out of scope, per feasibility doc section 7). Even a partial version can't avoid that coupling. Left entirely to uSync content import / manual backoffice setup.

### 8. Language engine limitations — Implement (update support only) — Done
- Fix additive-only behavior: `LanguageSyncEngine` should update an already-installed language's fallback/mandatory flags to match code, matching the create-or-update pattern already used by `ContentTypeSyncEngine`/`MediaTypeSyncEngine`.
- Single-enum-per-solution limit (`CodeFirstSyncService.cs:81`) is left as-is — one enum comfortably covers even a 12-language site, no concrete need for multiple.

### 9. Thumbnail field — Skipped
- All 69 content types in the export use the literal default `folder.png` (never customized). Media types vary but mostly just duplicate the already-supported Icon field. Legacy Umbraco field, superseded by Icon in the modern backoffice — no real signal to reproduce here.

### 10. Member types and relation types — Postponed
- Export only has the built-in `Member` type and 2 Umbraco Forms/Members-ecosystem relation types — nothing custom to reproduce in this export. Member types are a different domain (member/auth schema) that would need its own design pass. Tracked in `plan/mvp-and-roadmap.md`'s roadmap rather than closed outright, since it's a plausible future need for a site that does define custom member types.

### 11. No automated test coverage — Postponed
- No longer literally empty — gaps #1 and #4's implementation agents each added a couple of focused `PreFlightValidator` tests along the way. Still narrow (nothing for scanners or sync engines). A dedicated broader push is open-ended (unit vs. integration, mocking strategy for `IContentTypeService` etc.) and needs its own scoping conversation rather than a single subagent task. Leave to accrue incidentally for now.
