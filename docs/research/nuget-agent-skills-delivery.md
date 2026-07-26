# Can a NuGet package auto-deliver AI-agent skills/instructions to a consuming project?

**Researched:** 2026-07-25
**Goal:** Determine whether uCodeFirst, as a NuGet package, could ship AI-agent-facing guidance (attribute reference, Umbraco-concept → uCodeFirst-attribute mapping, a "create an ArticlePage" scripted procedure) that automatically reaches any AI coding agent (Claude Code, Cursor, Copilot) working in a consuming project — with no manual setup step in that project.

## Verdict

No mechanism available today makes this fully automatic and zero-config across agents. Every real, working delivery path requires either (a) the consuming project's repo to already contain a file that names/points at the guidance, or (b) an explicit one-time command a developer runs after installing the package. The closest thing to "automatic" that exists anywhere in this space — TanStack Intent — is itself opt-in: it still requires running `@tanstack/intent install` in the consumer's repo, which then writes guidance into files the consumer's repo owns (`CLAUDE.md`, `.cursorrules`, `AGENTS.md`). Nothing shipped inside `~/.nuget/packages/` or `node_modules/` is read by Claude Code, Cursor, or Copilot without the consuming repo's own tracked files pointing at it first.

Per-mechanism verdict:

1. **TanStack Intent** — (b) technically real and shipping today, but requires an explicit `intent install` run in the consumer's repo; it is a Node/npm-ecosystem tool with no NuGet equivalent. Source: [tanstack.com/intent/latest/docs/overview](https://tanstack.com/intent/latest/docs/overview) — *"Creates or updates lightweight `intent-skills` guidance in your config files (`AGENTS.md`, `CLAUDE.md`, `.cursorrules`, etc.)."*
2. **Claude Code skills discovery** — (c) not currently possible without the consumer's repo already containing a pointer/config. Skills load only from `~/.claude/skills/`, project `.claude/skills/` (including nested and parent-directory lookups), plugin `skills/` directories, or enterprise-managed settings — never from an arbitrary installed-dependency path. Source: [code.claude.com/docs/en/skills](https://code.claude.com/docs/en/skills), "Where skills live" table.
3. **NuGet content-delivery (`contentFiles`, `build`/`buildTransitive` targets)** — (b)/(c) split: MSBuild `.targets` files *can* technically execute arbitrary file-copy logic on restore/build (nothing in NuGet's tooling prevents it), but this is not a documented/sanctioned pattern for writing into the source tree, and real-world precedent (EF Core scaffolding, `dotnet new` templates, legacy `install.ps1`) all requires an explicit, separate developer-invoked command rather than an automatic restore/build side effect. Source: [learn.microsoft.com/nuget/concepts/msbuild-props-and-targets](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets).
4. **Cursor rules / Copilot instructions / AGENTS.md / llms.txt** — (c) every one of these requires the consuming repo to already contain the file (`.cursor/rules/*.mdc`, `.github/copilot-instructions.md`, `AGENTS.md`, `/llms.txt`); none of the primary specs document any package-manager auto-population mechanism. Source: [cursor.com/docs/context/rules](https://cursor.com/docs/context/rules), [docs.github.com copilot-instructions](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions), [agents.md](https://agents.md/), [llmstxt.org](https://llmstxt.org/).
5. **MCP as an alternative delivery vector** — (b) technically real and precedented (NuGet.org itself now hosts MCP servers, installable via the `dnx` command), but explicitly requires the consumer to add a manual entry to `.vscode/mcp.json` or `.mcp.json` — there is no auto-registration from a `PackageReference` alone. Source: [devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart](https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/).

---

## 1. TanStack Intent

**Repo:** [github.com/TanStack/intent](https://github.com/TanStack/intent) — description: *"A CLI for library maintainers to generate, validate, and ship Agent Skills alongside their npm packages."* MIT license, 317 stars, 257 commits on `main`, most recent push 2026-07-25 (checked via `GET /repos/TanStack/intent`).

**npm package:** [`@tanstack/intent`](https://www.npmjs.com/package/@tanstack/intent). Registry data (`registry.npmjs.org/@tanstack/intent`, fetched directly): first published 2026-03-03, latest version `0.3.6`, 43 published versions (starting `0.0.1`), last modified 2026-07-14. Download counts (`api.npmjs.org/downloads`): 81,362 in the last week, 329,403 in the last month — real, non-trivial adoption for a young tool, though these numbers reflect a scoped `@tanstack` package likely pulled in transitively by other TanStack tooling as much as direct installs.

**Problem it solves.** Per the TanStack blog announcement ([tanstack.com/blog/from-docs-to-agents](https://tanstack.com/blog/from-docs-to-agents)): *"Docs target humans who browse. Types check individual API calls but can't encode intent. Training data snapshots the ecosystem as it was, mixing versions with no way to tell which applies. The gap isn't content. It's lifecycle."* The fix: *"Skills ship inside your package and travel with the tool via your normal package manager update flow — not the model's training cutoff, not community-maintained rules files."*

**How a consumer actually gets the content.** This is not automatic at agent runtime. Per the docs overview page ([tanstack.com/intent/latest/docs/overview](https://tanstack.com/intent/latest/docs/overview)):
- Running `@tanstack/intent install` *"discovers every intent-enabled package"* (by scanning `node_modules` for packages carrying the `tanstack-intent` keyword) and *"Creates or updates lightweight `intent-skills` guidance in your config files (`AGENTS.md`, `CLAUDE.md`, `.cursorrules`, etc.)"* — i.e. it writes into files the consuming repo owns and would commit.
- `install` is explicit — a developer or an agent instructed by the developer must run it; it is not triggered by `npm install` itself (no postinstall hook described in the fetched docs/README).
- A separate `intent load <package>#<topic>` command lets an agent pull a specific `SKILL.md`'s content on demand.
- For library maintainers, `intent scaffold` (AI-guided) and `intent validate`/`intent stale` support authoring and CI-checking skills before publishing.

**Agents/tools targeted.** The blog post states explicit adoption/support: *"already adopted by VS Code, GitHub Copilot, OpenAI Codex, Cursor, Claude Code, Goose, Amp, and others,"* built on the open [Agent Skills spec](https://agentskills.io) (the same open standard Claude Code's own skills docs reference).

**Maturity/status.** No "stable"/"production" claim found; the blog post frames it as an active rollout: *"We've started rolling out skills in TanStack DB with other TanStack libraries following. If you maintain a library, tell your coding agent to run `npx @tanstack/intent scaffold` and let us know how it goes."* Versioning is pre-1.0 (`0.3.6`), consistent with an actively-iterating but not yet "done" tool.

**Ecosystem note:** this is entirely an npm/Node-ecosystem tool. It reads `node_modules` and writes to files via a Node CLI. There is no NuGet equivalent, and nothing in its design depends on npm specifically other than the discovery mechanism (scanning installed packages for a marker) — the *pattern* (ship `SKILL.md` files in the package, require an explicit `install` step in the consumer to wire them into agent config) is transferable in principle to a NuGet + MSBuild-target-invoked-CLI shape, but no such NuGet tool exists today (see §3).

Sources checked: [github.com/TanStack/intent](https://github.com/TanStack/intent), [tanstack.com/intent/latest/docs/overview](https://tanstack.com/intent/latest/docs/overview), [tanstack.com/blog/from-docs-to-agents](https://tanstack.com/blog/from-docs-to-agents), `registry.npmjs.org/@tanstack/intent`, `api.npmjs.org/downloads/point/{last-week,last-month}/@tanstack/intent`, `api.github.com/repos/TanStack/intent`.

---

## 2. Claude Code skills discovery

Per the current official docs page ([code.claude.com/docs/en/skills](https://code.claude.com/docs/en/skills), fetched in full 2026-07-25), the "Where skills live" table is exhaustive and explicit:

| Location   | Path                                                | Applies to                     |
| :--------- | :--------------------------------------------------- | :------------------------------ |
| Enterprise | managed settings                                     | All users in your organization |
| Personal   | `~/.claude/skills/<skill-name>/SKILL.md`             | All your projects              |
| Project    | `.claude/skills/<skill-name>/SKILL.md`               | This project only              |
| Plugin     | `<plugin>/skills/<skill-name>/SKILL.md`              | Where plugin is enabled        |

Additional documented discovery details, quoted directly:

- **Nested/monorepo discovery**: *"Skills also load from nested `.claude/skills/` directories below your working directory... This lets a monorepo package provide its own skills that apply when working on that package, even if the session started at the repo root."*
- **Parent-directory discovery**: *"Project skills load from `.claude/skills/` in your starting directory and in every parent directory up to the repository root."*
- **Symlinks**: *"A `<skill-name>` entry in the enterprise, personal, or project locations can be a symlink to a directory elsewhere on disk. Claude Code follows the symlink and reads `SKILL.md` from the target directory."* — this means a project's `.claude/skills/` *could* contain a symlink into `~/.nuget/packages/uCodeFirst/1.0.0/skills/`, but the symlink itself must still be created and committed inside the consuming project's own `.claude/skills/` — it is not auto-created.
- **`--add-dir` exception**: *"The `--add-dir` flag and `/add-dir` command grant file access rather than configuration discovery, but skills are an exception: `.claude/skills/` within an added directory is loaded automatically."* This is the one documented case where a skill *outside* the project root can load without a pointer file inside the project — but it still requires the user to explicitly pass `--add-dir <path-to-nuget-package-folder>` (or `/add-dir`) at session start; nothing does this automatically, and the NuGet global packages path (`~/.nuget/packages/<id>/<version>/`) is not a conventional place a developer would think to `--add-dir`.
- **Plugins/marketplaces**: per [code.claude.com/docs/en/plugins-reference](https://code.claude.com/docs/en/plugins-reference), plugin-provided skills are *"automatically discovered when the plugin is installed,"* but installation itself is never automatic from a dependency tree — it requires either `claude plugin install <plugin>@<marketplace>` (writing to `~/.claude/settings.json` or, with `--scope project`, to the committed `.claude/settings.json`), or a `.claude-plugin/plugin.json` manifest dropped directly under an existing skills directory (again requiring the consumer's own `.claude/` tree to contain it). Project-scoped plugin installs are explicitly repo-declared: *"`--scope project` writes to `enabledPlugins` in .claude/settings.json, making the plugin available to everyone who clones the project repository."*

**Direct answer to the question posed**: there is no documented mechanism today by which a file shipped inside `~/.nuget/packages/uCodeFirst/1.0.0/` is automatically picked up without the consuming project's own repo (its `.claude/` tree) containing something — a real skill file, a symlink, or a settings.json plugin declaration — that points at it. The `--add-dir` skills exception is the only loophole, and it still requires an explicit per-session flag from the developer, not an automatic wiring from `dotnet restore`.

Sources: [code.claude.com/docs/en/skills](https://code.claude.com/docs/en/skills), [code.claude.com/docs/en/plugins-reference](https://code.claude.com/docs/en/plugins-reference).

---

## 3. NuGet content-delivery mechanisms

### `build`/`buildTransitive` MSBuild targets

Per [learn.microsoft.com/nuget/concepts/msbuild-props-and-targets](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets):

> *"NuGet packages may sometimes add custom build targets or properties to projects that consume that package. This can be achieved by adding a valid MSBuild file, in the form `<package_id>.targets` or `<package_id>.props`... within the build folders of the project."*

Folder semantics (table from the doc):

| Folder | NuGet Version | Use |
| --- | --- | --- |
| `build` | 2.5+ | Build logic for every framework of a project. |
| `buildMultiTargeting` | 4.0+ | Build logic for the outer build for multi-targeted projects (PackageReference only). |
| `buildTransitive` | 5.0+ | Build logic for assets that flow transitively to any consuming project (PackageReference only). |

For `PackageReference` projects (which is what `src/uCodeFirst` and any modern consumer would use): *".props and .targets are not added to the project file but are instead made available through `{projectName}.nuget.g.targets` and `{projectName}.nuget.g.props`. These files are automatically generated when restore is run."* — so a `build/{packageid}.targets` file's `<Target>` elements genuinely execute during every `dotnet restore`/`dotnet build` in the consumer, automatically, with no consumer opt-in beyond referencing the package.

**Can a `.targets` file legitimately copy a file into the consumer's repo root or `.claude/skills/` as a side effect of build?** Technically, yes — nothing in NuGet's own documentation restricts what a `.targets` `<Target>` can do; it is arbitrary MSBuild/task logic (a `<Copy>` or `<WriteLinesToFile>` task pointing at `$(MSBuildProjectDirectory)/.claude/skills/...` would run and write the file). But this is not a documented, sanctioned use case anywhere in NuGet's own docs — the guidance section only lists restrictions around properties/items that affect *restore itself* (`TargetFramework`, `PackageReference`, etc., which must not be modified), not source-tree writes. It is a capability that exists as an emergent property of "arbitrary MSBuild code runs," not a designed content-delivery feature.

**Known real-world friction with this approach** (found via search, not in a single Microsoft doc but corroborated across NuGet GitHub issues): NuGet's own issue tracker documents `MSBuildThisFileDirectory` evaluation bugs *"applied to the customer's project tree location"* rather than the package's own directory ([dotnet/project-system#5214](https://github.com/dotnet/project-system/issues/5214)), and target-name collisions across packages' `.targets` files ("if two `.targets` files define targets with the same name, the latter will overwrite the former"). Writing to the consumer's source tree from restore is also fundamentally in tension with reproducible/locked builds: `dotnet restore --locked-mode` and CI pipelines expect restore to be side-effect-free against the lock file/source tree; a target that mutates tracked files during restore would produce a dirty working tree on every CI run and any Central Package Management setup that pins exact restore behavior. No official doc explicitly says "don't do this," but there is no documented precedent of a mainstream package doing it either (see next section).

### `contentFiles`

Per [learn.microsoft.com/nuget/reference/nuspec](https://learn.microsoft.com/en-us/nuget/reference/nuspec) (§ "Using the contentFiles element for content files"): content files are placed under `/contentFiles/{codeLanguage}/{TxM}/{any?}` in the package, and attributes control behavior:

- `buildAction` — MSBuild item type (`Content`, `None`, `EmbeddedResource`, `Compile`, default `Compile`).
- `copyToOutput` — *"A Boolean indicating whether to copy content items to the build (or publish) output folder. The default is false."*
- `flatten` — only relevant when `copyToOutput` is true.

Critically, `copyToOutput` copies into the **build/publish output folder** — i.e. `bin/`/`obj`/publish output — not the source tree. `contentFiles` with `PackageReference` are wired in as project items referenced from the NuGet package cache (`~/.nuget/packages/...`) via the generated `.nuget.g.props`; they are not physically copied into the consumer's checked-in source directory. This mechanism is designed for immutable assets consumed at build time (e.g., an embedded config resource), not for writing editable/discoverable files like `SKILL.md` into a location an agent would scan.

### Real precedent for writing into the consumer's SOURCE TREE (not bin/obj)

- **Legacy `install.ps1`** (packages.config only): historically, NuGet's Visual Studio Package Manager could run a PowerShell `install.ps1` script at package-install time that could write arbitrary files into the project directory (e.g. old EF6 T4 templates were seeded this way). This is confirmed dead for the `PackageReference` format that `uCodeFirst` and its consumers use: *"With NuGet v3 and PackageReference, PowerShell script support was modified to no longer execute install and uninstall scripts... only `init.ps1` is supported"* — and `init.ps1` runs once per solution/session, not per-project, and cannot be relied on for per-consuming-project file writes in the modern SDK-style/PackageReference world uCodeFirst and Umbraco 17 both use.
- **EF Core scaffolding** (`dotnet ef dbcontext scaffold`, `Scaffold-DbContext`): does write real `.cs` files into the consumer's source tree — but only via an explicit, separate CLI/PMC command a developer runs deliberately, never as a side effect of `dotnet restore`/`dotnet build`. It ships as a design-time tool (`dotnet-ef`, a .NET tool) invoked on demand, not a build-time MSBuild target.
- **`dotnet new` templates**: also write real files into a target directory — but again only via an explicit `dotnet new <template>` invocation the developer runs after `dotnet new install <package>`. Per [learn.microsoft.com/dotnet/core/tools/custom-templates](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates), template packages structure content under a `content` folder specifically because the *template engine*, not MSBuild restore, materializes those files — a fundamentally different, explicitly-invoked mechanism, not something a regular library `PackageReference` gets for free.
- **StyleCop.Analyzers**: ships as a Roslyn analyzer (`analyzers/` folder, loaded in-memory by the compiler) — it does not write any files into the consumer's project at all.

**Conclusion for §3**: the technical capability for a `.targets` file to write into a consumer's source tree exists (arbitrary MSBuild task execution), but there is no first-party NuGet mechanism designed for this, no documented sanctioned pattern, and no real precedent among mainstream .NET tooling of a plain library package doing so as an automatic restore/build side effect — every real precedent (EF Core scaffolding, `dotnet new`, legacy `install.ps1`) requires an explicit, separate, developer-invoked command outside the normal restore/build cycle.

Sources: [learn.microsoft.com/nuget/concepts/msbuild-props-and-targets](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets), [learn.microsoft.com/nuget/reference/nuspec](https://learn.microsoft.com/en-us/nuget/reference/nuspec), [learn.microsoft.com/dotnet/core/tools/custom-templates](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates), [dotnet/project-system#5214](https://github.com/dotnet/project-system/issues/5214), NuGet/Home issue discussion on `install.ps1`/`init.ps1` PackageReference behavior.

---

## 4. Cursor rules / GitHub Copilot custom instructions / AGENTS.md / llms.txt

**Cursor rules.** Per [cursor.com/docs/context/rules](https://cursor.com/docs/context/rules): *"Project rules live in `.cursor/rules` as `.mdc` files and are version-controlled."* They are *"scoped using path patterns, invoked manually, or included based on relevance."* Cursor also reads `AGENTS.md` files placed in any subdirectory: *"they will be automatically applied when working with files in that directory or its children."* User-level rules are separate, defined in the app's Customize UI, global across projects. No mention anywhere on this page of a package manager (npm or NuGet) auto-populating `.cursor/rules/` or `.cursorrules`; rules are created manually (`/create-rule` command, the Customize UI, or importing rules from a GitHub repo by hand).

**GitHub Copilot custom instructions.** Per [docs.github.com — Adding repository custom instructions for GitHub Copilot](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions): repo-wide instructions live at `.github/copilot-instructions.md`; path-scoped instructions live at `.github/instructions/NAME.instructions.md`; Copilot also reads `AGENTS.md` files anywhere in the repo, or a root `CLAUDE.md`/`GEMINI.md`. Discovery is automatic *once the file exists in the repo*: *"The instructions in the file(s) are available for use by Copilot as soon as you save the file(s). Instructions are automatically added to requests that you submit to Copilot."* No package-manager auto-population is documented; the only automation mentioned is Copilot's own cloud agent offering to *generate* such a file interactively — still a manual, in-repo action.

**AGENTS.md.** Per [agents.md](https://agents.md/): *"a **README for agents**: a dedicated, predictable place to provide the context and instructions to help AI coding agents work on your project."* It lives *"at the root of the repository"* (with nested-directory support per-tool, e.g. Cursor's nested `AGENTS.md` handling above); discovery is hierarchical: *"Agents automatically read the nearest file in the directory tree, so the closest one takes precedence."* Declared tool support is broad — OpenAI Codex, Google Jules, Factory, Aider, goose, opencode, Zed, Warp, VS Code, Devin, JetBrains Junie, Amp, Cursor, Gemini CLI, GitHub Copilot, Windsurf, and others. No package-manager auto-generation mechanism is described; the spec only notes *"Most coding agents can even scaffold one for you if you ask nicely"* — i.e. an agent can be asked to write the file, which is still a manual, in-repo, per-project action.

**llms.txt.** Per [llmstxt.org](https://llmstxt.org/): a proposed convention, explicitly modeled on `robots.txt`/`sitemap.xml`, for *"the root path `/llms.txt` of a website (or, optionally, in a subpath)"* — this is a **web-serving** convention (content an LLM fetches over HTTP when browsing a *site*), not a source-repository or package-manager convention at all. It is fetched by an agent making an HTTP request to the documentation site's URL, wholly disconnected from `dotnet restore`/NuGet's install path. Some doc-generation tooling (nbdev, VitePress/Docusaurus plugins) auto-*generates* `llms.txt` as a build artifact of a documentation site build, but that is unrelated to a NuGet package auto-populating anything inside a consumer's *repository*.

**Cross-cutting finding for §4**: every one of these four conventions requires the consuming repository to already contain the file — there is no known real-world case of a package manager (npm, NuGet, or pip) auto-populating any of them. The pattern that exists (TanStack Intent, §1) is the closest analog, and even it requires an explicit developer-run command that then writes into these same convention files.

Sources: [cursor.com/docs/context/rules](https://cursor.com/docs/context/rules), [docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions), [agents.md](https://agents.md/), [llmstxt.org](https://llmstxt.org/).

---

## 5. MCP as an alternative delivery vector

**What MCP is for here.** Rather than injecting static skill files, uCodeFirst could ship an MCP server that exposes tools/resources (e.g. "list supported editors," "generate an ArticlePage skeleton") that an agent calls on demand instead of reading a pre-loaded document.

**Claude Code's MCP connection model.** Per [code.claude.com/docs/en/mcp](https://code.claude.com/docs/en/mcp), MCP servers are added via `claude mcp add` (writing to project-scoped `.mcp.json`, user-scoped `~/.claude.json`, or via `claude mcp add-json`), with transports `http`, `sse` (deprecated), stdio, or `ws`. Project-scoped servers checked into `.mcp.json` require developer approval on first use in that repo: *"Project-scoped servers from `.mcp.json` that are awaiting your approval appear in `claude mcp list`... as `⏸ Pending approval (run \`claude\` to approve)`."* As of Claude Code v2.1.196, this approval gate is explicitly untrust-safe: *"A cloned repository can't approve its own servers: `enableAllProjectMcpServers` or `enabledMcpjsonServers` committed to the project's `.claude/settings.json` is ignored in an untrusted folder."* This confirms there is no zero-config, auto-connect path for an MCP server referenced from a repo the user hasn't explicitly trusted — every route requires either a manual `claude mcp add` command or a committed-and-approved `.mcp.json` entry.

**Precedent for a library shipping its own MCP server, found via NuGet itself.** NuGet.org has recently added first-class MCP server hosting/consumption, confirmed via multiple primary sources found in this research:
- [learn.microsoft.com/nuget/concepts/nuget-mcp-server](https://learn.microsoft.com/en-us/nuget/concepts/nuget-mcp-server) — "Using the NuGet Model Context Protocol (MCP) Server."
- [devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart](https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/) — "Building Your First MCP Server with .NET and Publishing to NuGet," which documents the .NET 10 SDK's `dnx` command (*"adds a command, `dnx`, that is used to download, install, and run the MCP server from nuget.org"*) and the exact consumer opt-in flow: search nuget.org filtered to the `mcpserver` package type, open the package's "MCP Server" tab, and copy a ready-made JSON snippet into `.vscode/mcp.json` (or the equivalent `.mcp.json` for Claude Code) — after which the editor/agent prompts for any required input values on first use.
- Example published MCP-server packages found on NuGet.org: `NuGet.Mcp.Server` (v1.4.16), `Community.Mcp.DotNet` (v1.1.0).

**Opt-in required — not zero-config.** Adding the library as a normal `PackageReference` does not register or expose its MCP server to any agent. The consumer must separately add a `command`/`args` entry (typically `dnx <packageid> --yes` plus env vars) to their own `.mcp.json`/`.vscode/mcp.json`, exactly mirroring the "consumer's repo must already contain a pointer file" pattern found in §§2 and 4. There is no discovered mechanism, in either the MCP spec or Claude Code's docs, for a `PackageReference` alone to cause an MCP server to be auto-registered.

Sources: [code.claude.com/docs/en/mcp](https://code.claude.com/docs/en/mcp), [learn.microsoft.com/nuget/concepts/nuget-mcp-server](https://learn.microsoft.com/en-us/nuget/concepts/nuget-mcp-server), [devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart](https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/), [modelcontextprotocol.io](https://modelcontextprotocol.io/introduction) (protocol overview, referenced from Claude Code's own MCP docs).

---

## Claude-Code-specific vs. portable-across-agents

**Claude-Code-specific:**
- The `SKILL.md` frontmatter dialect described in §2 (`disable-model-invocation`, `context: fork`, `${CLAUDE_SKILL_DIR}` substitution, etc.) — though the base `SKILL.md` format itself is the open [Agent Skills](https://agentskills.io) standard, which Claude Code explicitly says it "extends."
- The `--add-dir` skills-loading exception, `claude mcp add`/`.mcp.json` approval-gate behavior, and the plugin/marketplace system (`.claude-plugin/plugin.json`, `claude plugin install`) are all Claude Code CLI concepts with no direct Cursor/Copilot equivalent.
- Claude Tag channel-based skill loading and Cowork/cloud-session skill sync are Claude-product-specific, not portable.

**Portable across agents (by design or broad adoption):**
- The Agent Skills open standard itself ([agentskills.io](https://agentskills.io)) — TanStack Intent explicitly targets it and lists VS Code, GitHub Copilot, OpenAI Codex, Cursor, Claude Code, Goose, and Amp as adopters.
- `AGENTS.md` — the broadest-adopted single-file convention found in this research, explicitly supported by Cursor, GitHub Copilot, OpenAI Codex, Windsurf, Aider, and many others per [agents.md](https://agents.md/)'s own tool list.
- MCP itself is a cross-vendor open protocol (modelcontextprotocol.io); an MCP server uCodeFirst shipped would in principle be connectable from any MCP-capable client (Claude Code, Cursor, VS Code Copilot Chat), not just Claude Code — though each client's opt-in config file differs (`.mcp.json` vs `.vscode/mcp.json` vs Cursor's own MCP config).
- The core finding — "every mechanism requires the consumer's repo to already contain a pointer, or requires an explicit developer-run command" — holds identically across all agents surveyed; none of Claude Code, Cursor, or Copilot differ on this point.
