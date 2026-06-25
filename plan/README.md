# Code-First for Umbraco — Plan

Exploration of a **code-first** authoring experience for Umbraco, where C# classes are
the source of truth for *schema* (document types, data types, etc.) and Umbraco's
database is built/updated from them — the way Episerver/Optimizely has worked since v6/v7.

The motivation: in the age of AI-assisted development, the slow part of Umbraco work is
*clicking around the backoffice and generating models before you can write code*. Code-first
inverts that — you write the class, start the site, and verify. No generate step in between.

**Target version: Umbraco CMS 17.4.2** (the `Umbraco.CMS` clone is checked out at `release-17.4.2`).
Design decisions were originally cross-checked against v18.1.0-rc and re-verified on 17.4.2; the model is
version-agnostic and the write-side feasibility is confirmed on 17.4.2
(see [mvp-and-roadmap.md](mvp-and-roadmap.md#feasibility--write-side-api-verified-target-umbraco-1742)).
uSync is cloned side-by-side as the deployment partner.

## Documents

- **[prior-art.md](prior-art.md)** — Does this already exist? (Short answer: partially. Read this first.)
- **[comparison.md](comparison.md)** — Current approach vs. code-first, pros/cons, and the **editor** angle.
- **[design-decisions.md](design-decisions.md)** — Every decision (Q1–Q10) with rationale.
- **[mvp-and-roadmap.md](mvp-and-roadmap.md)** — What v1 covers, what's deferred, architecture sketch, example API.

## Executive summary

- **Vehicle:** a standalone NuGet **package** built on Umbraco's public services — *not* a core
  contribution (too high a bar, too slow a loop) and *not* built inside uSync (opposite philosophy).
  "Propose for core later via RFC" stays a long-term goal.
- **Ownership model:** **code wins** for schema; content stays database-owned. Editors create
  content; developers own structure. (This is the Episerver division that kept editors happy.)
- **Identity:** every code type declares an **explicit, stable GUID**; cross-references in code use
  `typeof(...)`, resolved to GUIDs via reflection. Umbraco cross-references everything by GUID, so
  this is non-negotiable.
- **Runtime:** your class is *both* the schema definition *and* the runtime published model.
  **ModelsBuilder is replaced.** MVP uses explicit property getters; a source generator is the headline
  roadmap item.
- **Deployment:** **dev** = code-first auto-syncs on startup; **prod** = code-first disabled, **uSync**
  promotes the schema (DB → disk → prod DB). Each tool in its lane. This removes all production-sync
  risk from the MVP.
- **Boundary:** code-first owns **server-side schema only**. Client manifests (`umbraco-package.json`),
  property-editor UIs, dashboards, RTE toolbar extensions, stylesheets remain normal project assets;
  schema **references** them by string alias.
- **Safety:** a **pre-flight validation** pass runs before any DB write — duplicate aliases, duplicate
  GUIDs, reserved names, unresolved `typeof` references — and aborts with one aggregated, fixable error.
  Never half-applies.

## Package identity

- **Name / NuGet ID / root namespace:** `Consid.Umbraco.CodeFirst` (company-prefixed; avoids the
  bare `Umbraco.` prefix HQ discourages for community packages).
- **Location:** `~/Code/Consid/Consid.Umbraco.CodeFirst` — a standalone repo, *separate* from the
  test/clone project at `~/Code/Consid/TestProjects/UmbracoCodeFirst`.
- **Target framework:** `net10.0` (matches Umbraco 17.4.2 / SDK 10.0.100).
- **Umbraco reference:** NuGet `Umbraco.Cms.Web.Common` `17.4.2` — never a project-reference to the
  cloned source (the clone is read-only reference material).

## Status

Design/feasibility exploration. No code written yet. Next step after sign-off: scaffold the package
and build the MVP vertical slice (see [mvp-and-roadmap.md](mvp-and-roadmap.md)).
