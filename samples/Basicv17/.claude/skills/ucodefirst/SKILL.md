---
name: ucodefirst
description: Use when creating, extending, or modifying Umbraco document types, element types, media types, compositions, templates, or properties in a project that references the uCodeFirst NuGet package (C# attributes synced into Umbraco on startup instead of backoffice clicking). Covers requests like "create a document type", "add a property to X", "create an element type for a block", "create a new page called X with fields Y and Z".
---

# uCodeFirst

uCodeFirst is a code-first schema-authoring library for Umbraco 17+: C# attributes on classes/interfaces
are synced into the Umbraco database on startup, instead of building document types by clicking through
the backoffice.

**This skill assumes you already know Umbraco** — document types, element types, media types, data
types/property editors, compositions, templates, tabs, the content tree, Block List/Grid. It does not
re-teach Umbraco. The only new thing is the code-first *authoring* layer: how those same concepts are
expressed as C# attributes in this specific library, and this project's own conventions for using them.

## Concept → attribute map

| Umbraco concept | uCodeFirst attribute |
|---|---|
| Document type (page / tree node) | `[DocumentType]` on a `partial class : PublishedContentModel` |
| Element type (Block List/Grid item content, not a tree node) | `[ElementType]` on a `partial class : PublishedElementModel` |
| Media type | `[MediaType]` on a `partial class`; inherit a `uCodeFirst.BuiltIn.Umbraco*Model` stub (e.g. `UmbracoImageModel`) to extend a built-in type |
| Composition (mixin) | `[CompositionType]` on an `interface`, implemented by the doctype/element class |
| Data type / property editor | An attribute on a property: `[TextString]`, `[TextArea]`, `[RichText]`, `[Numeric]`, `[TrueFalse]`, `[DatePicker]`, `[Dropdown]`, `[MediaPicker3]`, `[ContentPicker]`, `[MultiNodeTreePicker]`, `[MultiUrlPicker]`, `[Tags]`, `[ColorPicker]`, `[Slider]`, `[MemberPicker]`, `[UploadField]`, `[Label]`, `[CheckBoxList]`, `[RadioButtonList]`, `[ImageCropper]` |
| Configured/shared editor (dropdown options, Block List/Grid contents, slider min/max, dynamic-root pickers) | A custom class subclassing the relevant `*DataType` base, decorated `[DataType]` — see `Models/DataTypes/*.cs` in this sample |
| Tab/group | `[Group(Groups.X, SortOrder: n)]` on each property |
| Template | An enum member decorated `[Template(Alias: "...", Master: Other)]`, referenced by `[DocumentType(DefaultTemplate: "alias")]` |
| Master/parent template | `Master:` param on `[Template]`, pointing at another member of the same enum |
| Allowed children | `[AllowedChildren(typeof(X), typeof(Y))]` on the parent doctype |
| Dictionary item | `[DictionaryItem]` on a `const string` field; nested static classes become parent items. Key defaults to the const's `nameof(...)` value (field) or the class name (parent) — set `Alias = "..."` on either when the real Umbraco key needs characters a C# identifier can't hold, e.g. spaces (see `Models/Dictionary/DictionaryKeys.cs`) |
| Language | `[Language(IsoCode: "...", Fallback:, IsMandatory:)]` on an enum member, `[Languages(DefaultLanguage:)]` on the enum itself |
| Backoffice folder (Content Types tree grouping) | `Folder: "Pages"` / `"Pages/Articles"` param, any type-declaring attribute |

## Critical, non-obvious rules for this codebase

1. **`Guid` is a settable property, not a constructor argument.** Set it with `Guid = "..."`, never
   `Guid: "..."`. **Leave it unset** (`""`, the default) — the `UCF001` Roslyn analyzer flags a missing
   GUID as a build error with a code fixer that generates one. Don't hand-write a "plausible-looking"
   GUID yourself.
2. **Property-editor attribute params are also properties**, not constructor args: `Name`, `Alias`,
   `Mandatory`, `Description`, `VariesByCulture` all use `=`, e.g.
   `[TextString(Name = "Headline", Mandatory = true)]` — not `Name:`/`Mandatory:`.
3. By contrast, `[DocumentType]`/`[ElementType]`/`[MediaType]`/`[CompositionType]`'s *own* constructor
   parameters (`Name`, `Alias`, `Icon`, `Color`, `Description`, `AllowedAtRoot`, `Folder`,
   `DefaultTemplate`, `VariesByCulture`, `IsContainer`, `PreventCleanup`,
   `KeepAllVersionsNewerThanDays`, `KeepLatestVersionPerDayForDays`) are real constructor arguments and
   do take `:`. Only `Guid` (and, on data types, config properties — see rule 6) is the odd one out.
4. **Alias derivation**: omit `Alias` anywhere and it's lower-camel-cased from the class/property name
   (`NewsArticle` → `newsArticle`). Only set `Alias` explicitly when the request specifies one.
5. **No ModelsBuilder.** Every doctype/element class needs `[PublishedModel("alias")]`, the standard
   constructor boilerplate (`IPublishedContent`/`IPublishedElement` + `IPublishedValueFallback` stored in
   a private field), and explicit `Value<T>(_publishedValueFallback, "propertyAlias")` getters. Copy this
   boilerplate verbatim from an existing sibling class in this sample rather than reconstructing it.
6. **Configured editors can't be set via named attribute arguments** — `DataTypeBase`'s config properties
   (dropdown options, Block List/Grid contents, slider min/max/step, dynamic-root config) are get-only.
   They need their own `[DataType("...", Guid = "...")]`-decorated class subclassing the relevant
   `*DataType` base and overriding the config properties/methods. See `Models/DataTypes/*.cs` for the
   pattern (e.g. `ContentBlocksList : BlockListDataType`, `PrioritySlider : SliderDataType`).
7. **`[AllowedChildren]` targets must themselves carry `[DocumentType]`/`[ElementType]`** — sync's
   pre-flight validation rejects dangling references before touching the database.
8. **Not every document type needs a template.** Data-only or composition-only content types can skip
   `DefaultTemplate` and have no corresponding `.cshtml` (see `BlogPost.cs` in this sample).

## Scaffolding a new page/document type — procedure

When asked to create a document/element type (e.g. *"create an article page, with image, header,
body"*):

**Step 1 — read the shape of the request.** Identify the type name, its properties, whether it's a page
(document type) or block content (element type), whether it's root-level, and whether it implies
reusable nested content (Block List/Grid).

**Step 2 — infer without asking** (repo-convention-driven; don't interrupt the developer for these):
- Property → editor, from name/semantic hints: "image"/"photo" → `[MediaPicker3]`; "header"/"headline"/
  "title" → `[TextString]`; "body"/"content" (long-form) → `[RichText]`; "summary"/"excerpt" →
  `[TextArea]`; a date-sounding name → `[DatePicker]`; boolean-sounding ("is X", "has X") → `[TrueFalse]`;
  a closed set of options → `[Dropdown]` or a custom `[DataType]` subclass if the options need to be
  shared/reused; free-form keyword/tag lists → `[Tags]` (defaults: comma delimiter, "default" tag
  group, JSON storage) — subclass `TagsDataType` to change `Delimiter`, `Group`, or `StorageType` (see
  `Models/DataTypes/SeoKeywordsTags.cs`).
- `Alias` — leave unset unless the request specifies one.
- Group/tab placement — mirror the closest existing sibling document type in this project (most content
  goes in one `Groups.Content` tab unless there's a clear `Groups.Settings`/`Groups.SEO` split already in
  use nearby).
- `Icon`/`Color` — pick a semantically fitting `ContentTypeIcon`/`ContentTypeColor` constant; cosmetic,
  don't ask.
- `Guid` — leave unset, rely on `UCF001`'s code fixer.

**Step 3 — always confirm before writing code** (surface these explicitly; don't silently guess):
- **Template**: reuse an existing template/master (list the candidates from the project's
  `[Template]`-decorated enum) or create a new one.
- **Folder placement**: propose one based on sibling types (e.g. `"Pages"`) but confirm if ambiguous.
- **Compositions**: whether an existing `[CompositionType]` interface in the project applies (scan
  `Models/Compositions/` or equivalent) — never silently attach or silently skip one that looks relevant.
- **Block List/Grid usage**: if the request implies reusable nested content blocks, confirm whether to
  wire an existing element type or scaffold a new one.
- **Structure**: whether the type is allowed at root (`AllowedAtRoot`) and what may be created under it
  (`[AllowedChildren]`).

**Step 4 — generate the class.** Follow the exact pattern of an existing sibling model file in this
project (constructor boilerplate, `[PublishedModel]`, `Value<T>` getters, `Group`/property-editor
attributes). Match the project's existing namespace/folder convention (e.g. `Models/Pages/`,
`Models/Blocks/`).

**Step 5 — template + view**, only if Step 3 confirmed a template is needed:
- Reusing an existing template: just set `DefaultTemplate` to its alias.
- New template: add a `[Template(Alias: "...", Master: ...)]` member to the project's template enum, and
  create `Views/<Alias>.cshtml` (`@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<TheClass>`,
  rendering the properties the class exposes). Mirror an existing `.cshtml` in `Views/` for the exact
  boilerplate — only set `Layout = "_Layout.cshtml"` if the template has a master.

**Step 6 — report back what was inferred vs. confirmed**, so the developer can correct any inferred
choice (property→editor mapping, group, icon/color) after the fact instead of being interrupted for each
one up front.

## Worked example

*"Create an article page, with image, header, body"*:

1. **Infer**: `Header` → `[TextString(Mandatory = true)]`, `Body` → `[RichText]`,
   `Image` → `[MediaPicker3]`.
2. **Confirm**: new template `articlePage` vs. reusing `NewsArticle`'s `newsArticle` template; folder
   `"Pages"`; whether the SEO composition applies; allowed-at-root or child-only.
3. **Generate** `Models/Pages/ArticlePage.cs`, mirroring `Models/Pages/NewsArticle.cs`'s structure
   exactly, `Guid` left unset.
4. If a new template was confirmed: add it to `Models/Templates.cs`, create `Views/ArticlePage.cshtml`
   mirroring `Views/NewsArticle.cshtml`.
5. Report what was inferred vs. confirmed.
