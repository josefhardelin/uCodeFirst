# Design decisions

Each decision below was worked through deliberately. Format: the question, the options, the choice, and
*why*. Source references are to this repo's `Umbraco.CMS` (v18.1.0-rc).

---

## Q1 — Ownership: who wins when code and the backoffice disagree on schema?
**Decision: code wins (A).** Code is the single source of truth for *schema*; content stays
database-owned. Editing schema in the backoffice is discouraged/overwritten.

**Why:** the entire value proposition (write class → start site) only pays off if code is authoritative.
Bidirectional merge ("coexistence") is a multi-year effort that overlaps with uSync's mature
serialization. The editor concern is about *content* (DB-owned), not *schema* (code-owned) — the
Episerver division. See [comparison.md](comparison.md) for the editor angle.

---

## Q2 — How are data types and property configuration modeled in code?
**Decision: hybrid, first-class code data types (C).** Built-in simple editors usable **inline via
attributes** with sensible defaults (the 80%); **first-class code-defined data types** for anything
configured/shared (pickers, rich text, dropdowns), referenced by properties **by type**. Configuration
is expressed by populating Umbraco's **existing** strongly-typed `…Configuration` POCOs.

**Why:** honors "code wins / never open the backoffice," keeps data types **shared and deduplicated**
(no per-property explosion — an explicit goal), and reuses Umbraco's own config model instead of a
parallel one.

**Source evidence:** every picker config is already a strongly-typed C# class with `[ConfigurationField]`
attributes, e.g. `Umbraco.Core/PropertyEditors/MultiNodePickerConfiguration.cs`,
`ContentPickerConfiguration.cs`, `RichTextConfiguration.cs`, `BlockGridConfiguration.cs`.

### Sub-problem: config that references content *instances* (e.g. a content picker start node)
The "chicken-and-egg": a start node is a content node created at runtime; its GUID isn't known at
compile time. Three tiers:
1. **Reference types/structure, not instances (~80%).** Use `DynamicRoot` (origin `"Root"`/`"Site"`/
   `"Current"`/`"Parent"` + query steps filtering by **doctype keys** you define) and
   `AllowedContentTypeIds` — all code-knowable. (Source: `MultiNodePickerConfigurationTreeSource`,
   `Umbraco.Core/DynamicRoot/*`.)
2. **Deterministic GUIDs for genuine singletons.** Code-first seeds the node with a fixed GUID; config
   references it. (Expands into content seeding — roadmap.)
3. **Escape hatch.** Mark that one config field "backoffice-owned" so the editor sets it once and code
   never overwrites it.

---

## Q3 — Identity: how does a code type get a stable GUID and match the DB across renames?
**Decision: explicit GUID per type (B).** Each type declares a stable GUID (e.g.
`[DocumentType(Guid: "…")]`). In code, cross-references use **`typeof(SomeType)`**, resolved to the
declared GUID via reflection at sync time.

**Why:** Umbraco cross-references *everything* by GUID, not alias — `ContentElementTypeKey`,
`AnyOfDocTypeKeys`, area `Key`, etc. (Source: `BlockGridConfiguration.cs`,
`MultiNodePickerConfigurationTreeSource.cs`.) Alias-matching would turn a class rename into
delete+recreate = **content data loss**, and cross-refs would be brittle. Explicit GUIDs make renames
safe and references deterministic, and are AI-friendly (an AI can generate them). This matches the
Episerver model and is what makes the uSync export (Q7) deterministic.

> Considered auto-deriving GUIDs from type identity (zero ceremony) but rejected it as the default:
> moving/renaming a class silently changes a derived GUID → silent data loss. Explicit is bulletproof.

---

## Q4 — Where does this live: Umbraco core, a package, or uSync?
**Decision: standalone NuGet package (built on public APIs), "propose for core later via RFC."**

**Why:**
- **uSync — wrong home.** Opposite philosophy (DB → disk). Reuse its *ideas* (sync, diffing) and use it
  for *deployment* (Q7), don't build inside it.
- **Core — too high a bar, too slow a loop.** RFC + maintainer buy-in from a team that just rewrote the
  backoffice; every change is a core PR. Kills iteration speed.
- **Package — fast, no gatekeeping, usable now, and the *correct path to core anyway*** (prove as a
  popular package, then RFC). This is how uSiteBuilder and every serious extension work.

Plug into `IContentTypeService`, `IDataTypeService`, `ITemplateService`, startup/notification hooks.

---

## Q5 — Runtime model: is the code class also the published model? How do getters bind?
**Decision: the class *is* the runtime model; ModelsBuilder is disabled.** MVP uses **explicit one-liner
getters** (A); a **source generator** (B) is the headline roadmap item. (Runtime proxy (C) rejected — it
fights Umbraco's lazy published-cache model.)

**Why:** the whole point is "no generate step." The class wears two hats: schema declaration *and*
runtime model. Explicit getters need zero infrastructure and prove the loop fastest; the boilerplate is
trivial for an AI to emit. The source generator later removes it entirely.

**Source evidence (the hooks exist):**
- `IPublishedModelFactory` maps content-type **alias → CLR type** and wraps `IPublishedElement`.
- `[PublishedModel("alias")]` declares "this class is the model for this content type."
- Base classes `PublishedElementModel` / `PublishedContentModel`.
- (`Umbraco.Core/Models/PublishedContent/*`.)

This is exactly what ModelsBuilder generates from the DB — code-first inverts the arrow.

---

## Q6 — MVP scope vs. roadmap
**Decision:** MVP = document types + simple built-in property editors + groups/sort/validation +
startup sync + class-as-model + basic structure. **Block Grid deferred to roadmap #2.** Full list in
[mvp-and-roadmap.md](mvp-and-roadmap.md).

**Why:** prove "class → doctype → query it" end-to-end first. Block Grid sits on three unbuilt
foundations (element types, configured data types, cross-type references); build the simple loop first
and Block Grid becomes a natural extension rather than a cliff.

---

## Q7 — Sync lifecycle and production safety
**Decision (refined by the uSync insight):**
- **Development:** code-first **auto-syncs on startup**; uSync **import OFF** (export/report only).
- **Production:** code-first **disabled entirely**; **uSync imports** the committed `.uSync` files on
  deploy, as teams already do.
- Native production code-first sync (with destructive-change gating, dry-run/preview) → **roadmap**.

**Why:** offloads *all* production-sync risk to a mature, battle-tested tool instead of reinventing it
on a deadline. Clean separation: code-first = dev authoring; uSync = environment promotion. Exploits
Q3's stable GUIDs to make exports deterministic.

**Caveats to document:**
- Per-environment config prevents the two tools fighting (dev: code-first on / uSync import off; prod:
  reverse).
- Two representations exist: **C# classes = source of truth**; **`.uSync` files = generated deploy
  artifact**, committed like a lockfile, never hand-edited.

---

## Q8 — Authoring API surface
**Decision:** attribute-based, one class = one doctype = one runtime model.
- **(a)** Property alias **derived** from property name, with override. *(Less ceremony.)*
- **(b)** **Dedicated editor attributes** (`[TextString]`, `[RichText]`), not a generic
  `[Property(Editor=…)]`. *(Discoverable, type-safe, self-documenting — great for AI.)*
- **(c)** **String-based groups**, plus a `Groups` constants class (`Groups.Content`, `Groups.Settings`,
  …) à la Episerver's `SystemTabNames` — typo-proof common tabs, raw strings still allowed.

Example in [mvp-and-roadmap.md](mvp-and-roadmap.md).

---

## Q9 — Built-in editors: reuse Umbraco's data types, or own our own?
**Decision: code-first owns its own data types (B).** Built-in editors resolve to **code-owned,
deduplicated** data types with deterministic keys (one shared `[TextString]`-default type reused
everywhere).

**Why:** "code wins" (Q1) requires the data type's config to be code-controlled, not silently editable
in the backoffice; deterministic code-owned keys make uSync export clean (Q7). Reusing Umbraco's
editor-owned built-ins would let the backoffice quietly change config out from under code. The cosmetic
duplication in the data-type list is acceptable; name/folder them so editors see they're code-managed.

> Rejected hybrid "reuse-if-config-matches" (C) as implicit magic: the same attribute could bind to an
> editor-owned or code-owned type depending on invisible state.

---

## Q10 — The schema ↔ client-manifest boundary
**Decision:** code-first owns **server-side schema only** — document types, data types *and their
configuration*, templates. It does **not** own **client manifests / static assets** — property-editor
UIs, dashboards, RTE toolbar extensions (`umbraco-package.json`), stylesheets. Where schema needs a
client extension, it **references it by manifest alias (a string)**, never generating the manifest.

**Why:** Umbraco v14+ pushed huge amounts of behavior into frontend manifests. Owning that world too
would mean reimplementing the entire client-extension system in C# — scope death. Same seam as Q5 (the
class is the model; the editor UI is not our problem).

**Worked example — RTE style menu:** the style menu *definition* stays a `umbraco-package.json`
`tiptapToolbarExtension` of kind `styleMenu` (a client asset). Code-first's RTE data type config merely
**enables** the Style Select toolbar action and references it **by alias**. The manifest supplies *what's
in the menu*; code-first supplies *that it's on*. (Source: `Umbraco.Core/PropertyEditors/
RichTextConfiguration.cs`; Umbraco docs: RTE style-menu.)

**Concession:** client cross-references are **by string alias**, not `typeof` (manifests aren't C#
types). Offer constants for common built-in aliases to keep them typo-safe.
