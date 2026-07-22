# Documentation-Hosting Strategy for uCodeFirst

**Researched:** 2026-07-21
**Goal:** Decide how to host public documentation for uCodeFirst (a solo/small-team, company-backed OSS .NET library) with a modern, TanStack-like look and feel, deployable via GitHub Actions to GitHub Pages, with docs content living in this repo — while keeping ongoing authoring/maintenance burden realistic for ~34 attribute classes today, growing over time, against today's sparse XML doc-comment coverage.

## Executive summary

**tanstack.com is not a stack to copy** — it's a bespoke full-stack React application (`@tanstack/react-start` on Vite, Tailwind CSS 4, Drizzle ORM, PostgreSQL, deployed to Cloudflare Workers, source: https://github.com/TanStack/tanstack.com/blob/main/package.json), built by the team that *makes* the router framework it runs on, with a database behind it. It's realistic to take from it *visual and IA inspiration only* (clean typographic docs, framework/category switcher, left-nav tree, dark mode, integrated search) — not the underlying tooling, which is neither a documentation-site generator nor remotely proportionate to a solo/small-team library's needs.

Of the general-purpose static-site generators surveyed, three are genuinely strong current options for a from-scratch content-driven docs site with GitHub Pages support, hierarchical nav, and dark mode out of the box: **Docusaurus** (v3.10.2, actively released, official GH Pages + GitHub Actions docs), **Starlight** (v0.41.3, pre-1.0 but shipping frequent releases, official GH Pages support via the Astro GitHub Action, dark mode built in, but sidebar is hand-configured not auto-generated from folders), and **VitePress** (v1.6.4 stable with a 2.0 alpha in progress, official GH Pages Actions workflow, dark mode built in, sidebar hand-configured). **mkdocs-material** is a real outlier: as of November 2025 the project entered maintenance mode — the team is redirecting new development to a successor project, **Zensical** (https://zensical.org) — because the upstream MkDocs 2.0 removes the plugin system Material for MkDocs depends on (https://squidfunk.github.io/mkdocs-material/blog/2026/02/18/mkdocs-2.0/). New projects adopting it today are choosing to freeze on MkDocs 1.x indefinitely; not a good long-term bet. **Nextra** (v4.6.1, last published ~8 months ago) is real and Next.js-native but shows a slower release cadence and open PRs going unreviewed (https://github.com/shuding/nextra/discussions/3125) — and, notably, TanStack itself does **not** use Nextra; tanstack.com is TanStack's own router-based app, so "Nextra is TanStack-adjacent" is not actually true.

For C#-specific API reference generation: **DocFX** (dotnet/docfx, latest stable 2.78.5, no fixed release cadence but actively developed with prereleases) is the only option with first-party Microsoft backing, an official GitHub Pages Actions workflow, and — critically — a **"modern" template** (`"template": ["default", "modern"]`) that supports dark mode and looks materially better than the old default theme, alongside `docfx metadata` for emitting intermediate `.yml`/JSON API metadata usable outside DocFX's own renderer. **XMLDoc2Markdown / xmldoc2md** tools exist and are maintained-ish (multiple competing forks; the most-used one, charlesdevandiere/xmldoc2md, was last released mid-2024) and can emit plain Markdown droppable into any Markdown-based site, but produce bare signature/parameter dumps with no prose — they need manual augmentation to read as real documentation. **Sandcastle** is not dead — community-maintained continuation EWSoftware/SHFB is still cutting releases in 2026 (https://github.com/EWSoftware/SHFB/releases) — but it targets CHM/MSBuild-era workflows and has been functionally superseded by DocFX as Microsoft's own recommended tool for new .NET projects.

**GitHub Pages mechanics**: per GitHub's own docs, branch-based publishing (root or `/docs`) is the low-control option (Jekyll-only unless you pre-build), while a GitHub Actions workflow using `actions/upload-pages-artifact` + `actions/deploy-pages` is the documented path for any non-Jekyll build (Docusaurus, Starlight, VitePress, DocFX all fit here) and is what all four generators' own official docs point to. `paths:` filters on the workflow trigger let a docs subfolder build independently of `src/`/`tests/`/`samples/` changes in this same repo — with one documented caveat: skipped path-filtered workflows leave required status checks "Pending" indefinitely, which can block PR merges if that workflow is marked required.

Given ~34 attribute classes today and **sparse-to-absent XML doc comments across most of them** (confirmed directly in this repo — see background), a pure auto-generation pipeline (DocFX or xmldoc2md) would produce mostly-empty reference pages *today*, not a shortcut. The recommended path (see final section) is a hand-authored Reference tree in a modern SSG, matching TanStack's actual approach and this project's actual scale, with auto-generation revisited only once XML doc coverage across the ~34 attribute files is backfilled.

## 1. What powers tanstack.com

Source: `github.com/TanStack/tanstack.com` README and `package.json` (https://github.com/TanStack/tanstack.com, https://raw.githubusercontent.com/TanStack/tanstack.com/main/package.json).

- The README states the site is **"Built with TanStack Router and deployed on Cloudflare Workers."**
- `package.json` confirms: **`@tanstack/react-start`** (1.168.26, TanStack's own full-stack React framework) on **Vite** (v8), **React 19**, **TypeScript**, **Tailwind CSS 4**, **Drizzle ORM** + PostgreSQL (a real database backs the site — likely for things like the AI chat / analytics features bundled into the repo), deployed via **Wrangler** to Cloudflare Workers.
- It is **not** Next.js, Remix, Nextra, Docusaurus, or any off-the-shelf docs generator — it is TanStack's flagship product dogfooding itself.

**Adoptability verdict:** this stack is not realistically adoptable by a small solo/small-team C# OSS project. It requires fluency in a bleeding-edge React meta-framework the team itself maintains, a database, and Cloudflare infrastructure, for a payoff (a fully custom app) that a documentation site doesn't need. The practical takeaway is **visual/IA inspiration only** — the clean two-pane layout, framework picker, left-nav category tree, in-page TOC, and dark-mode-first design are all reproducible in any of the SSGs below without touching TanStack's own tooling.

## 2. Static site generator options

All four were checked against their own official docs/repos for: GitHub Pages deployment path, sidebar/nav hierarchy support, dark mode, and release recency.

### Docusaurus (Meta)

- **Version / cadence:** v3.10.2 (July 10, 2026), following v3.10.1 (Apr 30, 2026) and v3.10.0 (Apr 7, 2026) — actively released roughly monthly (https://github.com/facebook/docusaurus/releases).
- **GitHub Pages:** Officially documented, including a native GitHub Actions workflow using `actions/deploy-pages@v4` for same-repo deployments, plus an SSH-based cross-repo variant (https://docusaurus.io/docs/deployment#deploying-to-github-pages).
- **Sidebar/nav:** Supports **both** `{ type: 'autogenerated', dirName: '.' }` (sidebar built straight from the docs folder tree) **and** fully manual, arbitrarily nested `category`/`items` config — the two can be mixed (https://docusaurus.io/docs/sidebar). This is the best fit of the four for "Reference > [attribute categories] > [individual attributes]" since it can grow from folder structure alone as attributes are added.
- **Dark mode:** Built in via `themeConfig.colorMode` (`defaultMode`, `disableSwitch`, `respectPrefersColorScheme`) — on by default with a toggle, no plugin needed (https://docusaurus.io/docs/api/themes/configuration).
- **Code highlighting:** Prism by default, per Docusaurus's own docs; Shiki is available via community plugin, not core — not independently re-verified further since it wasn't decision-critical here.

### Starlight (Astro)

- **Version / cadence:** `@astrojs/starlight` v0.41.3, last published ~16 days before this research (pre-1.0, "beta software" per its own docs, but shipping frequently — https://starlight.astro.build/getting-started/, version confirmed via search of npm/GitHub release metadata).
- **GitHub Pages:** No Starlight-specific deploy doc; relies on Astro's own official path — Astro **maintains its own official GitHub Action** (`withastro/action`) explicitly recommended as "the recommended way to deploy to GitHub Pages" (https://docs.astro.build/en/guides/deploy/github/), which Starlight sites use unmodified since Starlight is an Astro integration, not a separate build tool.
- **Sidebar/nav:** **Manually configured** in `astro.config.mjs` (a `sidebar` array), not auto-generated from `src/content/docs/` folder structure — the getting-started guide explicitly directs users to a separate "Sidebar Navigation" guide to hand-build it.
- **Dark mode:** Built in out of the box — the docs UI itself ships a Dark/Light/Auto selector with zero config.

### VitePress

- **Version / cadence:** v1.6.4 is the current stable release; a 2.0 alpha (`2.0.0-alpha.18`) is in active development per the docs site's own deploy guide, indicating the project is still moving, not stalled.
- **GitHub Pages:** Official, fully worked GitHub Actions workflow in VitePress's own docs — checkout, Node setup, `npm run docs:build`, `actions/upload-pages-artifact@v3`, `actions/deploy-pages@v4` (https://vitepress.dev/guide/deploy#github-pages).
- **Sidebar/nav:** **Manually configured** via `themeConfig.sidebar`, supporting arbitrarily nested `items` arrays for hierarchy — no folder-driven auto-generation (https://vitepress.dev/reference/default-theme-config#sidebar).
- **Dark mode:** Built in (`darkModeSwitchLabel`/`darkModeSwitchTitle` config, plus themeable light/dark image variants) — no plugin required.

### mkdocs-material

- **Version / status:** Latest release 9.7.7 (per GitHub releases metadata) — but the material point is **maintenance mode**, entered November 2025 with v9.7.0 (Nov 11, 2025) being the last feature release. Per the project's own blog: upstream MkDocs 2.0 removes the plugin system Material for MkDocs is built on, breaks the theme architecture (nav becomes pre-rendered HTML instead of structured data), and switches config format from YAML to TOML — so *"if your documentation is built with Material for MkDocs, it will cease to work with MkDocs 2.0."* Only critical bug/security fixes are promised through November 2026. The team's own successor project is **Zensical** (https://zensical.org), explicitly not yet feature-complete (https://squidfunk.github.io/mkdocs-material/blog/2026/02/18/mkdocs-2.0/).
- **GitHub Pages:** Well documented, `mkdocs gh-deploy --force` or a GitHub Actions workflow building to the `gh-pages` branch (https://squidfunk.github.io/mkdocs-material/publishing-your-site/).
- **Sidebar/nav:** Manually configured `nav:` tree in `mkdocs.yml`, with full support for nested sections and index pages (https://squidfunk.github.io/mkdocs-material/setup/setting-up-navigation/) — no folder auto-generation out of the box.
- **Dark mode:** Built in via `theme.palette` scheme toggling (`default`/`slate`), trivial config, available since v7.1.0.
- **Python dependency note:** the docs build itself needs a Python + `pip`/`mkdocs` toolchain, wholly separate from the .NET solution and CI already in this repo. That's a tolerable extra dependency in isolation — the disqualifying issue is the project's own admitted terminal trajectory on MkDocs 1.x, not the Python runtime.

### Nextra

- **Version / cadence:** v4.6.1, last published ~8 months before this research (https://nextra.site/docs; version/date via npm/GitHub release search). A GitHub discussion titled *"Is Nextra still being maintained?"* (https://github.com/shuding/nextra/discussions/3125) reflects real community concern that open PRs are going unreviewed.
- **Built on:** Next.js App Router (Nextra 4 dropped Pages Router support) — "Nextra is a framework on top of Next.js" (https://nextra.site/docs).
- **GitHub Pages:** Supported via Next.js's own static export mode (`output: 'export'`) — Nextra's docs reference a "Static Exports" guide, which is the same static-export mechanism any Next.js site would use for GitHub Pages, not something Nextra adds itself.
- **Sidebar/nav:** Folder + `_meta.js`/`_meta.json` file-based convention drives the nav tree automatically, similar in spirit to Docusaurus's autogenerated mode.
- **Dark mode:** Built in with a toggle in the default theme.
- **TanStack connection:** Verified **false** — TanStack's own site is not built with Nextra (see §1); it's TanStack's own router. Nextra is a reasonable generator on its own merits but should not be chosen on a mistaken "it's what TanStack uses" premise.

## 3. C#-specific API reference generation from XML doc comments

### DocFX (Microsoft, `dotnet/docfx`)

- **What it does:** "Converts .NET assembly, XML code comment, REST API Swagger files and markdown into rendered HTML pages, JSON model or PDF files" (https://dotnet.github.io/docfx/).
- **GitHub Pages:** Official documented GitHub Actions workflow — `actions/checkout`, `actions/setup-dotnet`, `dotnet tool update -g docfx`, `docfx <path>/docfx.json`, then `actions/upload-pages-artifact` + `actions/deploy-pages` (https://dotnet.github.io/docfx/, confirmed via the quick-start page's own workflow sample).
- **Theming:** Ships a legacy **`default`** theme (the dated-looking one most people associate with DocFX) and a newer **`modern`** template, enabled via `"template": ["default", "modern"]` in `docfx.json`. DocFX's own docs recommend it: *"We recommend using the modern template that matches the look and feel of this site. It supports dark mode, more features, rich customization options."* Fully custom templates are also supported (`template` folder with `public/main.css`/`main.js`, Mustache-based HTML components) (https://dotnet.github.io/docfx/docs/template.html).
- **Metadata export:** `docfx metadata` emits intermediate `.yml` API metadata files independent of the HTML-rendering step — this is the hook a JS-based site generator could consume to render its own API pages, rather than using DocFX's HTML output directly (confirmed via DocFX's own metadata/build pipeline structure; the specific reference page fetch 404'd during this research, but the pipeline split — `docfx metadata` producing YAML consumed by `docfx build` — is DocFX's documented two-stage architecture).
- **Maintenance:** Latest stable **2.78.5**; DocFX does not follow a fixed release cadence — "new versions arrive when maintainers see enough changes that warrant a release," with prereleases used to dogfood breaking changes — but the repo shows continued activity (prerelease builds published within days of this research). Actively maintained, just not on a calendar.

### XMLDoc2Markdown / xmldoc2md family

- Real and multiple: `charlesdevandiere/xmldoc2md` (the most established, `dotnet tool install -g XMLDoc2Markdown`, v5.0.0 last published mid-2024), plus independent forks/rewrites (`jaime-olivares/xmldoc2md`, `FRACerqueira/xmldoc2md`, `ejball/XmlDocMarkdown`, `bartsokol/XmlDoc2Md`) — a fragmented ecosystem rather than one canonical maintained tool.
- charlesdevandiere's tool explicitly supports a `--github-pages` flag (strips `.md` extensions from links) and a `--structure tree` option, meaning its output is designed to drop straight into a Markdown-based static site's content folder.
- **Prose gap confirmed:** these tools translate `<summary>`/`<param>`/`<returns>` XML comments into Markdown tables/headings — they do not write narrative "how to use this attribute" prose. Given this repo's XML doc coverage is sparse today (see background), running any of these tools now would emit mostly bare class/member signatures with empty summaries for most of the ~34 attribute files — real content would still need to be hand-written per attribute either way.

### Sandcastle

- Original Microsoft Sandcastle was discontinued in October 2012; the actively maintained continuation is the community project **Sandcastle Help File Builder** (`EWSoftware/SHFB`), which explicitly states Sandcastle tools "have been merged into the Sandcastle Help File Builder project and all future development and support...are handled at this project site."
- **Not dead:** SHFB shows real 2026 releases (e.g. `2026.1.20.0`, `2026.3.29.0` per https://github.com/EWSoftware/SHFB/releases), maintained by Eric Woodruff.
- **Still effectively legacy for this use case:** it's oriented around CHM/MAML help-file generation and MSBuild/Visual Studio integration, predating the GitHub-Pages-first, Markdown-content-in-repo model this project wants. DocFX is the tool Microsoft itself points .NET OSS projects toward today; SHFB is the right call for teams already invested in CHM/help-file workflows, not a good starting point for a new project.

## 4. GitHub Pages deployment mechanics

Per GitHub's own docs (https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site):

- **Branch-based (`/` or `/docs` on the default branch, or a dedicated `gh-pages` branch):** lowest control — "If you do not need any control over the build process for your site, we recommend that you publish your site when changes are pushed to a specific branch." Defaults to a Jekyll build unless you disable it; **does not support symbolic links** ("If your repository contains symbolic links, you will need to publish your site using a GitHub Actions workflow"). A `/docs`-folder source is the least amount of CI setup but couples the docs source and build output into the same commit history as the app code, awkward once a real SSG build step is involved.
- **GitHub Actions workflow (recommended for any non-Jekyll build):** GitHub's own guidance — "If you want to use a build process other than Jekyll or you do not want a dedicated branch to hold your compiled static files, we recommend that you write a GitHub Actions workflow to publish your site" (https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site). The documented mechanics (https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages): a build job that runs `actions/upload-pages-artifact` against the built static output directory, and a `deploy` job — with `permissions: pages: write, id-token: write` and `needs: build` — that runs `actions/deploy-pages`. This is exactly the pattern all four SSGs' own official docs point to (Docusaurus, VitePress, Astro/Starlight all document this same two-action combo) and what DocFX's own quick-start uses too.
- **Custom domain (CNAME):** configured via a `CNAME` file at the site root plus DNS records pointed at GitHub Pages, with an optional domain-verification step "to increase the security of your custom domain and avoid takeover attacks" (https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site). Not decision-relevant unless/until a custom domain is desired over `<org>.github.io/uCodeFirst`.
- **Monorepo scoping (`paths:` filter):** GitHub's official workflow-trigger docs give exactly this shape (https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/triggering-a-workflow):
  ```yaml
  on:
    push:
      paths:
        - 'sub-project/**'
        - '!sub-project/docs/**'
  ```
  Applied here: a docs workflow triggered on `paths: ['docs-site/**']` (or wherever the docs source lands) would not run on `src/`/`tests/`/`samples/` changes, and — inverted — the library's own build/test workflow can exclude the docs path so unrelated docs edits don't trigger a full `dotnet build`/`dotnet test` run. **Caveat GitHub calls out explicitly:** a workflow skipped by path filtering leaves any status checks it would have produced in a **"Pending" state indefinitely**, which blocks PR merges if that check is marked "required" in branch protection — something to account for when deciding whether the docs workflow's success should ever be a required check on the main library's PRs (it should not be).

## 5. Trade-off: hand-authored vs auto-generated vs hybrid reference

Grounded in this repo's actual state: `src/uCodeFirst/Attributes/` (12 type-level/structural attributes) + `src/uCodeFirst/DataTypes/` (22 property-editor attributes) = **~34 attribute classes** forming the enumerable "Reference" surface, with XML doc (`///`) coverage confirmed sparse-to-absent across most of them in a direct spot check (`DocumentTypeAttribute.cs`, `TextString.cs`/`TextStringDataType.cs`, `RichText.cs`/`RichTextDataType.cs`, all `*DataType.cs` abstract bases carry effectively zero doc comments; only `DictionaryItemAttribute.cs`, `LanguagesAttribute.cs`, `LanguageAttribute.cs`, `TemplateAttribute.cs` have some).

- **Full auto-generation (DocFX or xmldoc2md today):** Technically the lowest ongoing-maintenance option once running — it regenerates from source on every build, so it can never drift from the actual attribute signatures. But it is **not a shortcut right now**: with most of the 34 files carrying no `///` comments, a first DocFX/xmldoc2md pass would render mostly bare class/member names with empty descriptions — the ~34-files backfill has to happen *before* auto-gen produces anything worth publishing. DocFX's `modern` template closes the "looks dated" gap reasonably well, but the generated page shape (member tables, inheritance lists) is still API-reference-shaped, not tutorial/usage-shaped — a reader wanting "how do I configure a `[BlockList]` property" gets a parameter table, not a worked example, unless the XML comments themselves carry `<example>` blocks (extra authoring burden layered on top of the backfill).
- **Full hand-authored Markdown/MDX per attribute:** Matches TanStack's actual approach (their reference pages are hand-written, not XML-doc-generated) and gives full control over structure, cross-linking (e.g. `[BlockList]` page linking to the composition guide), and worked examples pulled straight from `samples/Basicv17`. The real cost is ongoing: **34 pages today, growing with every new attribute**, each needing a human pass whenever a signature changes — a real, continuous authoring tax with no automatic drift-detection (nothing fails CI if a hand-written page falls out of sync with the actual attribute's constructor parameters).
- **Hybrid (hand-written prose + generated signature/parameter tables):** e.g. a short script/step that runs `docfx metadata` (or an xmldoc2md-style extraction) to produce per-attribute parameter tables as data, and hand-authored MDX/Markdown pages that embed that generated table alongside prose and examples — giving automatic drift *detection* (CI can diff the generated table against what's checked into the page, or the page can literally import the generated fragment at build time) without asking a human to re-type parameter lists. This still requires the XML-comment backfill to have anything worth extracting, and requires a small amount of custom plumbing (a script or a DocFX-metadata-to-MDX-frontmatter step) that doesn't exist off the shelf in any of the SSGs surveyed — it's the most powerful option and the most upfront engineering effort.

## Recommended approaches

### Option A — Docusaurus, hand-authored Reference tree, sidebar autogenerated from folder structure

- **Pros:** Only SSG surveyed that supports **folder-driven sidebar autogeneration** (`type: 'autogenerated'`) *and* manual nesting in the same config — so a `docs/reference/attributes/{document-type,element-type,...}/*.md` tree naturally becomes the "Reference > category > attribute" nav with zero nav-file maintenance as new attribute pages are added. Actively released (monthly cadence), official GH Pages + Actions docs, dark mode on by default, large ecosystem (versioning, i18n, search plugins) if the project grows.
- **Cons:** React/MDX-based — heavier toolchain (Node, React) than a .NET team may want to touch day-to-day; visual starting point is "generic modern docs site," further from TanStack's specific look than a from-scratch Tailwind build would be, though themeable.
- **Setup effort:** Low-to-medium — `npx create-docusaurus`, wire the GH Actions workflow from its own docs, done in under a day.
- **Ongoing burden:** One hand-authored Markdown page per attribute (~34 today), same as any hand-authored option — but zero nav-file upkeep, which is a real recurring cost the other two SSGs don't remove.
- **Reference-tree fit:** Best of the three general SSGs for this specific "grows by folder" requirement.

### Option B — Starlight (Astro), hand-authored Reference pages in a content collection tree

- **Pros:** Closest out-of-the-box aesthetic to a clean, TanStack-adjacent modern docs feel (typed content collections, built-in dark mode, fast Astro islands architecture, official first-party GH Pages Action). Strong for a team that wants minimal JS shipped to the client.
- **Cons:** Pre-1.0 (`0.41.3`) — API/config surface can still shift between releases, though release velocity is healthy. **Sidebar is hand-configured**, not folder-driven — every new attribute page needs a manual sidebar-array edit in `astro.config.mjs`, a real recurring cost at ~34-and-growing pages.
- **Setup effort:** Low — `npm create astro@latest -- --template starlight`, Astro's official GH Action is a few lines.
- **Ongoing burden:** Hand-authored pages **plus** hand-maintained sidebar config — the highest per-attribute-addition friction of the three SSGs.
- **Reference-tree fit:** Fully capable of the nested tree, just not automatic from folders the way Docusaurus is.

### Option C — DocFX generating raw API metadata, hand-authored prose site on top (hybrid)

- **Pros:** Only option that gets real drift-safety on signatures — `docfx metadata` output can't silently go stale the way a fully hand-written parameter table can. First-party Microsoft tool, so the API-extraction half needs no custom XML-parsing code. `modern` template supports dark mode, so even a "just use DocFX's own site" fallback isn't ugly the way the classic DocFX theme is.
- **Cons:** Most engineering effort of the three — requires either (a) accepting DocFX's own site shape (further from TanStack's look than Docusaurus/Starlight even with `modern`), or (b) building the plumbing to pull DocFX's `.yml` metadata into a separate Docusaurus/Starlight/VitePress site, which is custom, unproven glue with no official recipe from any of the four SSGs surveyed. Worthless today regardless, until the ~34 attribute files get XML doc-comment coverage — this option's entire value proposition (auto-sync) doesn't kick in until that backfill happens.
- **Setup effort:** High relative to A/B — backfill ~34 files' `///` comments, stand up DocFX metadata extraction, build or find the JSON/YAML-to-MDX bridge.
- **Ongoing burden:** Lowest *once built* — signatures stay in sync automatically; only prose sections need manual touch-up.
- **Reference-tree fit:** Whatever the destination site supports (pairs naturally with Option A's autogenerated sidebar if the generated pages land in the same folder convention).

## Recommended path forward

**Option A — Docusaurus, with a hand-authored Markdown page per attribute under an autogenerated Reference sidebar, deployed via GitHub Actions to GitHub Pages with a `paths:`-scoped workflow.**

Reasoning, grounded in the comparison above: at ~34 attributes with XML doc coverage confirmed sparse today, auto-generation (Option C) has no content to generate yet — its entire advantage (drift-safety) is latent until a real backfill effort happens, and that backfill is needed regardless of which SSG hosts the result. That leaves hand-authoring as the only option that produces a usable site *now*, which narrows the real choice to Docusaurus vs. Starlight. Docusaurus wins that comparison specifically because **its folder-autogenerated sidebar removes the one ongoing maintenance cost that scales with attribute count** — every other cost (writing the prose page itself) is identical between Docusaurus and Starlight and unavoidable in any hand-authored approach. Docusaurus is also the most actively released of the four general SSGs checked, has official, current GitHub Pages + Actions documentation, and ships dark mode by default — covering the "TanStack-like feel with minimal ongoing maintenance" brief about as well as a general-purpose SSG can.

Revisit Option C (DocFX metadata feeding hand-authored pages) as a **follow-up**, not a starting point: once the ~34 attribute files carry real `<summary>`/`<param>` XML comments (worth doing for IntelliSense/IDE tooltips regardless of the docs-site decision), a small script pulling DocFX's `.yml` metadata into each Docusaurus page's frontmatter (rendered as a parameter table above the hand-written prose) upgrades Option A into the hybrid model without a platform migration — Docusaurus's MDX support means that table can be a plain React/MDX component fed by a build-time data file, no separate site needed.
