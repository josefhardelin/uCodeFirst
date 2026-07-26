# uCodeFirst vs. v17 uSync export — gap status tracker

Tracks decisions on each gap identified in [`ucodefirst-vs-v17-usync-feasibility.md`](./ucodefirst-vs-v17-usync-feasibility.md).

Statuses: **Not decided** · **Skipped** · **Postponed** · **Partially done** · **Done**

| # | Gap | Status |
|---|---|---|
| 1 | Culture variance (content types & properties) | Done |
| 2 | Dictionary per-culture translations | Skipped |
| 3 | Property editor coverage + `BuildRecipe` extensibility ceiling (`internal`, no `InternalsVisibleTo`) | Done |
| 4 | Template master/parent hierarchy | Done |
| 5 | `ListView` configuration on content/media types | Done |
| 6 | `HistoryCleanup` policy block | Done |
| 7 | Domains (hostname/path → language + root node) | Skipped |
| 8 | Language engine limitations (additive-only; single `[Languages]` enum per solution) | Done |
| 9 | `Thumbnail` field on content/media/element types | Skipped |
| 10 | Member types and relation types (no concept at all) | Skipped |
| 11 | No automated test coverage in `tests/uCodeFirst.Tests/` | Partially done |

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

### 6. HistoryCleanup — Implement — Done
- `[DocumentType]` gains `bool PreventCleanup = false`, `int? KeepAllVersionsNewerThanDays = null`, `int? KeepLatestVersionPerDayForDays = null`, threaded through `DocumentTypeDefinition` and set on `ContentType.HistoryCleanup` by `ContentTypeSyncEngine` (new instance on create, mutated in place on update, matching the rest of `UpdateAsync`'s field-by-field style).
- Content types only — confirmed `MediaType` has no `HistoryCleanup` property, so `[MediaType]`/`MediaTypeSyncEngine` are untouched; `[ElementType]` is also out of scope (element types are Block List/Grid item content, never tree nodes with their own version history policy in practice).
- Unset params default to Umbraco's own defaults (`PreventCleanup: false`, both day-counts `null`), matching the export evidence (0/69 content types overrode the defaults) — no separate "leave untouched" sentinel needed.
- No new `PreFlightValidator` rule: Umbraco's `HistoryCleanup` is a flat POCO with no cross-field ordering constraint between the two day-count fields, so there's no real invalid combination to reject.

### 7. Domains — Skipped
- Absolute hostnames were never on the table (environment-specific, doesn't belong in committed code). Considered a relative-path-only version, but an `IDomain` in Umbraco always binds to a specific content *instance* (`rootContentId`), not a content *type* — and uCodeFirst is schema-only, never creating or referencing content items (the same reason `Content/`/`Media/` are out of scope, per feasibility doc section 7). Even a partial version can't avoid that coupling. Left entirely to uSync content import / manual backoffice setup.

### 8. Language engine limitations — Implement (update support only) — Done
- Fix additive-only behavior: `LanguageSyncEngine` should update an already-installed language's fallback/mandatory flags to match code, matching the create-or-update pattern already used by `ContentTypeSyncEngine`/`MediaTypeSyncEngine`.
- Single-enum-per-solution limit (`CodeFirstSyncService.cs:81`) is left as-is — one enum comfortably covers even a 12-language site, no concrete need for multiple.

### 9. Thumbnail field — Skipped
- All 69 content types in the export use the literal default `folder.png` (never customized). Media types vary but mostly just duplicate the already-supported Icon field. Legacy Umbraco field, superseded by Icon in the modern backoffice — no real signal to reproduce here.

### 10. Member types and relation types — Skipped
- Export only has the built-in `Member` type and 2 Umbraco Forms/Members-ecosystem relation types — nothing custom to reproduce in this export. Member types are a different domain (member/auth schema) that would need its own design pass. Previously tracked as a roadmap "postponed" item; explicitly dropped instead (2026-07-25) after confirming there's no concrete driving use case on a ~1 year+ horizon — moved to `plan/mvp-and-roadmap.md`'s "Explicitly out of scope" section rather than carried as speculative backlog. Revisit from scratch if a real need appears.

### 11. No automated test coverage — Partially done
- `tests/uCodeFirst.Tests/Discovery/DocumentTypeScannerTests.cs` (13 cases) and `tests/uCodeFirst.Tests/Validation/PreFlightValidatorTests.cs` (9 cases) now cover the pure-logic scanner/validator paths called out in `docs/research/testing-strategy.md` (duplicate alias/GUID, dangling `[AllowedChildren]` refs, composition property exclusion, dictionary parent-chain resolution). Sync-engine coverage for `ContentTypeSyncEngine`/`MediaTypeSyncEngine`/`DataTypeSyncEngine` also exists (create/update/prune/destructive cases) — see those files under `tests/uCodeFirst.Tests/Sync/`. Broader integration-style coverage (real `IContentTypeService` wiring, SQLite-backed) is still open and would need its own scoping conversation (unit vs. integration, mocking strategy) rather than a single subagent task.
- **Known gap found while writing this coverage** (documented, not fixed — see `PreFlightValidatorTests.DuplicatePropertyAlias_AcrossCompositionAndImplementingClass_IsNotCurrentlyDetected`): `PreFlightValidator`'s duplicate-property-alias check only scans each `DocumentTypeDefinition.Properties` list in isolation. A composition's properties live on a separate `DocumentTypeDefinition` (the one scanned for the `[CompositionType]` interface) and are never merged with the implementing class's own definition for this check. So a class and a composition it implements can declare a property with the same explicit `Alias` via different C# member names — the scanner's name-based exclusion doesn't catch it, and `PreFlightValidator` currently reports no error. It would only surface later, at the real Umbraco API level, when `ContentTypeSyncEngine` wires up the composition. Candidate follow-up: merge composition properties into the alias-collision check per implementing type before pre-flight passes.
