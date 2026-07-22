# Publishing uCodeFirst to NuGet.org

**Researched:** 2026-07-21
**Goal:** Figure out, from nuget.org's / Microsoft's / GitHub's / Umbraco's own documentation, everything needed to take `src/uCodeFirst/uCodeFirst.csproj` from "local ProjectReference only" to a properly published, discoverable, debuggable NuGet package — account setup, required `.csproj` metadata, a sensible versioning scheme given the package is coupled to Umbraco 17+, a CI/CD publish pipeline (including whether NuGet's newer OIDC "Trusted Publishing" is actually usable today), symbol/Source Link setup, how to test a packed `.nupkg` locally first, and how (if at all) it shows up on the Umbraco Marketplace.

## Executive summary

The current `src/uCodeFirst/uCodeFirst.csproj` already has `Version`, `Authors`, `Description`, and `PackageTags` — but is missing every other metadata property NuGet's own best-practices guide calls a `DO`: no `PackageLicenseExpression` (despite an MIT `LICENSE` file existing at the repo root), no `PackageProjectUrl`/`RepositoryUrl`/`RepositoryType`, no `PackageReadmeFile` (despite a root `README.md` existing), no `Copyright`, no symbol-package or Source Link wiring, and the `umbraco-marketplace` tag Umbraco's marketplace crawler specifically looks for is absent from `PackageTags`. None of this requires new tooling — it's `.csproj` edits plus one new NuGet package reference (`Microsoft.SourceLink.GitHub`).

Publishing itself needs a one-time nuget.org account, and a one-time decision between a scoped API key (works everywhere, today) and NuGet's newer OIDC-based **Trusted Publishing** — confirmed from nuget.org's own docs to be real and working via the `NuGet/login` GitHub Action, but nuget.org's own page says it's still being "rolled out gradually" and may not appear on every account yet, so this is a "check when you get there" item rather than something to hard-commit to in a workflow file today.

For versioning, the real-world precedent from the closest sibling project — **uSync**, which is also an Umbraco schema-management library and is tagged `umbraco-marketplace` — is to make the package's own major version track the Umbraco major version it targets (uSync 18.x → Umbraco 18, uSync 17.x → Umbraco 17, etc.), rather than using independent SemVer. Given uCodeFirst is explicitly built and pinned (`[17.4.2, 18.0.0)`) against Umbraco 17+, the same convention is the concrete recommendation here.

There is no automatic Umbraco Marketplace submission form — listing is **semi-automatic**: nuget.org tags (`umbraco-marketplace` plus a direct dependency on an `Umbraco.Cms.*`/`Umbraco.Commerce.*` package) get a package auto-discovered and re-scanned on a schedule, with all display metadata (icon, description, readme, project URL) sourced straight from the NuGet listing — so getting the `.csproj` metadata right *is* the marketplace listing step.

---

## 1. NuGet account/publisher setup

- **Account.** Sign in or create an account at nuget.org before doing anything else — required for both API keys and Trusted Publishing. (Microsoft Learn, [Publish a package](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package))
- **API keys are scoped, not global, by default now.** Under your username → **API Keys** → **Create**: give the key a name, a **Push** scope, a **Glob Pattern** for which package ID(s) it applies to (e.g. `uCodeFirst` or `uCodeFirst.*` if the project ever splits into multiple packages), and an expiration. Scoping exists specifically so a leaked/rotated key doesn't expose every package on the account, and each key can be independently refreshed or deleted without affecting others. (Microsoft Learn, [Scoped API keys](https://learn.microsoft.com/en-us/nuget/nuget-org/scoped-api-keys))
  - The key value is shown exactly once at creation time (via a **Copy** button) and can never be redisplayed — only regenerated. (Same source.)
  - Push with: `dotnet nuget push <package-file> --api-key <API-key> --source https://api.nuget.org/v3/index.json`. (Microsoft Learn, [How to publish NuGet packages](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package))
- **ID prefix reservation ("verified publisher" checkmark).** A separate, manual, human-reviewed process — there is no self-service UI toggle. You email **account@nuget.org** with your nuget.org owner display name and the prefix you want reserved (e.g. `uCodeFirst`), and nuget.org staff evaluate it against published criteria: the prefix must clearly identify the reservation owner, must not be a common/generic word or shorter than 4 characters, and its absence would otherwise cause "ambiguity, confusion, or other harm." Once approved, any future package matching that prefix from a *different* owner is rejected outright, and your matching packages get a checkmark badge on nuget.org and in Visual Studio 2017 15.4+. (Microsoft Learn, [ID Prefix Reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation))
  - This is explicitly listed as a **CONSIDER** (not a **DO**) in the best-practices guide, and is most valuable once a package ID is unique/first-of-its-kind, which `uCodeFirst` already appears to be. (Microsoft Learn, [Package authoring best practices § Package ID](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices))

## 2. Required and recommended package metadata

Microsoft's [Package authoring best practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices) and the [MSBuild pack-target property reference](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets) give the authoritative mapping of Visual Studio property → MSBuild `.csproj` property → `.nuspec` element. Checked directly against `src/uCodeFirst/uCodeFirst.csproj` (read 2026-07-21):

| Property | In csproj today? | Recommendation |
|---|---|---|
| `PackageId` | Not set (defaults to `AssemblyName`/`uCodeFirst`) | Fine to leave implicit, or set explicitly for clarity |
| `Version` | ✅ `0.1.0-alpha.1` | see §3 below |
| `Authors` | ✅ `Josef Härdelin` | — |
| `Company` | ❌ missing | optional; skip for a solo/OSS project |
| `Description` | ✅ present | — |
| `PackageTags` | ✅ `umbraco;code-first;schema;content-types;document-types` | **missing `umbraco-marketplace`** — required for Marketplace discovery, see §7 |
| `PackageLicenseExpression` | ❌ missing | **DO** add `<PackageLicenseExpression>MIT</PackageLicenseExpression>` — repo `LICENSE` is MIT (confirmed by reading the file); NuGet.org only accepts SPDX-approved expressions, which `MIT` is |
| `PackageProjectUrl` | ❌ missing | **DO** add, e.g. `https://github.com/josefhardelin/uCodeFirst` (confirmed via `git remote -v`) |
| `RepositoryUrl` / `RepositoryType` | ❌ missing | **CONSIDER** setting manually, but note: enabling Source Link (§5) with `PublishRepositoryUrl=true` populates both of these *and* the exact commit automatically — so this can be left to Source Link rather than hand-maintained |
| `PackageReadmeFile` | ❌ missing | **DO** — repo already has a root `README.md`; needs both the property and an explicit `<None Include>` pack item (see snippet below), since NuGet won't auto-include it |
| `PackageIcon` | ❌ missing | **CONSIDER** — no icon file exists in the repo yet; skip unless one is designed, it's optional |
| `PackageReleaseNotes` | ❌ missing | **CONSIDER** — worth adding per-release once versioning stabilizes past `0.x` |
| `Copyright` | ❌ missing | **DO** — cheap addition, e.g. `Copyright (c) Josef Härdelin 2026` |

Concrete metadata additions (illustrative, not applied — this is a research doc, not a change):

```xml
<PropertyGroup>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/josefhardelin/uCodeFirst</PackageProjectUrl>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <Copyright>Copyright (c) Josef Härdelin 2026</Copyright>
  <PackageTags>umbraco;umbraco-marketplace;code-first;schema;content-types;document-types</PackageTags>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

`PackageReadmeFile` only supports Markdown and must be paired with a pack item or the build silently omits it — this is explicit in the MSBuild property reference. (Microsoft Learn, [msbuild-targets § PackageReadmeFile](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets))

### Umbraco-ecosystem tagging convention

Fetching the live nuget.org listing for **uSync** (`https://www.nuget.org/packages/uSync`, version 18.0.2, checked 2026-07-21) shows its tags are exactly: `umbraco, usync, umbraco-marketplace`. This directly confirms, from a real current Umbraco-17/18-era package, the pattern documented by Umbraco itself (§7 below): a generic `umbraco` tag for ecosystem discoverability plus the literal `umbraco-marketplace` tag that Umbraco's marketplace crawler requires. uCodeFirst's tags should follow the same shape.

## 3. Versioning strategy

**SemVer basics** (semver.org, referenced directly by NuGet's own docs): `MAJOR.MINOR.PATCH[-prerelease]`, MAJOR for breaking changes, MINOR for backward-compatible features, PATCH for backward-compatible fixes. NuGet's best-practices guide explicitly endorses this ("CONSIDER using SemVer... Major.Minor.Patch[-prerelease] format") and separately says to publish anything unstable as a pre-release package. (Microsoft Learn, [Package authoring best practices § Package Version](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices))

**Real-world precedent for Umbraco-coupled packages — checked directly, not assumed:**

- **uSync** (`KevinJump/uSync`) ties its own package major version to the Umbraco major version it supports: version `10.x` for Umbraco 10, `13.x` for Umbraco 13, `17.x`/`18.x` for Umbraco 17/18, per its own nuget.org release notes and version-history pages (`nuget.org/packages/uSync/10.7.2`, `/13.3.2`, `uSync.Core/17.3.0`, `uSync/18.0.2` — fetched 2026-07-21). This is a hard version-per-Umbraco-major convention, not independent SemVer with a dependency range doing the compatibility signaling.
- This is a deliberate, ecosystem-wide pattern (Umbraco core itself does the same — each Umbraco major targets one CMS major), so consumers can eyeball a NuGet version number and immediately know which Umbraco major it targets, without reading the dependency graph.

**Recommendation for uCodeFirst:** given the library is *explicitly* pinned to Umbraco 17+ (`PackageReference Include="Umbraco.Cms.Web.Common" Version="[17.4.2, 18.0.0)"` in the current `.csproj`) and its entire purpose is syncing into that specific Umbraco generation's content-type APIs, follow the uSync convention: once the API is stable enough for a `1.0.0`, make it **`17.x.y`** (major = Umbraco major it supports), bump to `18.x.y` when/if the library is ported to target Umbraco 18, and keep `0.x.y-alpha`/`-beta` pre-release identifiers for everything before that first stable cut (the current `0.1.0-alpha.1` is already correctly shaped for this). Continue to also declare the exact Umbraco dependency range in the `PackageReference` — the Marketplace itself parses that range to compute displayed compatibility (§7) — the version-number convention and the dependency-range declaration are complementary, not alternatives.

## 4. CI/CD automation

No `.github/workflows/` directory exists in this repo yet (checked 2026-07-21) — this would be a new addition, not a modification.

### Workflow shape: pack + push on GitHub Release

GitHub's own docs confirm the `release` event's `published` activity type is the correct trigger — it fires for the release being made non-draft (covers both full releases and pre-releases), unlike `created`/`edited` which do *not* fire for draft releases. (GitHub Docs, [Events that trigger workflows](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows))

```yaml
on:
  release:
    types: [published]
```

### Trusted Publishing (OIDC) — confirmed status, not assumed

Checked directly against nuget.org's own current documentation page (`learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing`, page `ms.date: 2025-07-01`, last content-updated `2026-02-02` — i.e. genuinely current as of this research date):

- It is real and functioning today: a GitHub Actions job requests a short-lived OIDC token from GitHub, exchanges it via the `NuGet/login` GitHub Action for a temporary NuGet API key valid **1 hour**, single-use per token, then pushes with that key. No long-lived secret ever lives in the repo.
- **However, nuget.org's own page carries an explicit caveat: "If you don't see the Trusted Publishing option in your nuget.org account, it might not be available to you yet. We're rolling it out gradually."** This is not GA-for-everyone language — it is a phased rollout as of this writing. Treat it as "check your account when you get to this step," not as something to hard-wire into a workflow file sight unseen.
- Setup, once available on the account: nuget.org → username → **Trusted Publishing** → new policy → enter GitHub repo owner (`josefhardelin`), repo name (`uCodeFirst`), and **workflow file name only** (e.g. `publish.yml`, not the `.github/workflows/` path). Optionally restrict to a GitHub Actions **environment** (e.g. `release`).
- New policies against **private** repos start in a 7-day "pending" window that must see one successful publish to become permanent (this is to bind the policy to GitHub's immutable repo/owner IDs and prevent "resurrection" attacks on deleted-and-recreated repos). uCodeFirst's repo is public, so this caveat likely doesn't apply, but confirm at setup time.
- Minimal workflow shape (from nuget.org's own example):

```yaml
jobs:
  build-and-publish:
    permissions:
      id-token: write   # required — without it the OIDC request silently fails
    steps:
      # ... build/pack src/uCodeFirst/uCodeFirst.csproj ...
      - uses: NuGet/login@v1
        id: login
        with:
          user: ${{ secrets.NUGET_USER }}   # nuget.org profile name, NOT email
      - run: dotnet nuget push artifacts/*.nupkg --api-key ${{ steps.login.outputs.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

(Microsoft Learn, [Trusted Publishing on nuget.org](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing); action confirmed as the official NuGet-org-owned action at `github.com/NuGet/login`, requiring `id-token: write` + `contents: read` permissions.)

**Recommendation:** write the workflow to push with a **scoped API key stored as a repo secret** first (works immediately, no dependency on rollout status), structured so the push step is a single isolated step — swapping in `NuGet/login` + Trusted Publishing later is then a small, contained diff (delete the secret-based `--api-key`, add the `NuGet/login` step, add `id-token: write`) once the option appears on the account.

### Keeping `samples/Basicv17` out of the pack

- `dotnet pack` accepts either a specific `.csproj` or a solution/directory; **when given a solution, it packs every project in it whose `IsPackable` is `true`, which defaults to `true` for any SDK-style project.** (Microsoft Learn, [msbuild-targets § pack target inputs](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets))
- This repo has a solution file, `uCodeFirst.slnx` (confirmed via `find`), and `samples/Basicv17/Basicv17/Basicv17.csproj` (read 2026-07-21) does **not** set `IsPackable`, meaning it defaults to packable. `tests/uCodeFirst.Tests/uCodeFirst.Tests.csproj` already correctly sets `<IsPackable>false</IsPackable>`, but the sample does not.
- Today this is not actually a live bug — `CLAUDE.md`'s documented pack command is `dotnet pack src/uCodeFirst/uCodeFirst.csproj` (a specific project, not the `.slnx`), which only ever packs the library. But a CI workflow is exactly the kind of place someone might later write `dotnet pack` against the solution/repo root for convenience, at which point `Basicv17.csproj` (an executable Umbraco site, not a library) would attempt to pack too.
- Two independent mitigations, both straight from Microsoft's own docs: (a) keep the CI `pack` step scoped to `src/uCodeFirst/uCodeFirst.csproj` explicitly, matching the existing documented command; (b) as a defense-in-depth belt-and-suspenders addition, add `<IsPackable>false</IsPackable>` to `samples/Basicv17/Basicv17/Basicv17.csproj`'s `PropertyGroup` so it's inert even if someone later packs at the solution level.

## 5. Symbol packages and Source Link

**Symbol packages (`.snupkg`).** A `.snupkg` is a companion package containing only the library's Portable PDB debug symbols, uploaded to nuget.org's symbol server so Visual Studio can fetch it on demand during step-through debugging rather than requiring PDBs to ship inside the main `.nupkg`. Enable via:

```xml
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

`dotnet pack` then emits both `uCodeFirst.<version>.nupkg` and `uCodeFirst.<version>.snupkg`; `dotnet nuget push` on the `.nupkg` automatically also pushes the sibling `.snupkg` if it's present in the same folder. NuGet.org's symbol server only accepts the modern `.snupkg` format (not the legacy `.symbols.nupkg`), and only Portable PDBs — native/Windows PDBs aren't accepted, which isn't a concern here since this is a pure managed C# library. (Microsoft Learn, [How to publish NuGet symbol packages using .snupkg](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg))

**Source Link.** A separate but complementary technology: it embeds source-control metadata (repo URL + exact commit) into the PDB at pack time, so a consumer with the `.snupkg` downloaded can step directly into the *exact* GitHub source for that build, fetched live from GitHub, inside the debugger — no local source checkout needed. Setup, confirmed from the `dotnet/sourcelink` repository (the canonical source Microsoft's own library-guidance docs point to):

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
</ItemGroup>

<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>
```

`PrivateAssets="All"` keeps it a build-time-only dependency that never flows to consumers. `PublishRepositoryUrl=true` is also what auto-populates the package's `RepositoryUrl`/`RepositoryType`/commit metadata mentioned in §2, so this single property does double duty. (dotnet/sourcelink README via GitHub; cross-referenced against Microsoft Learn, [Source Link and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink), which explicitly recommends both Source Link and publishing symbol files together for the best debugging experience.)

## 6. Local testing before publishing

Two documented, complementary approaches — no single official "recommended" one, but both are first-party:

1. **Folder-based local feed.** A local NuGet feed is literally just a directory tree; `nuget add <pkg> -source <path>` (or a bare output folder from `dotnet pack -o <path>`) makes it usable as a package source via `dotnet nuget add source <path> --name local-test`. (Microsoft Learn, [Setting up Local NuGet Feeds](https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds); [dotnet nuget add source](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-add-source))
2. **Cache-busting is the real gotcha, not the feed setup.** NuGet's global-packages cache is keyed by package ID **and exact version** — restoring `uCodeFirst 0.1.0-alpha.1` a second time from a local feed after repacking it will silently serve the *stale cached copy* rather than re-reading the new `.nupkg`, unless the cache is cleared. `dotnet nuget locals` is the first-party tool for this: `dotnet nuget locals global-packages --clear` (or `all --clear` to also flush http-cache/temp/plugins-cache). (Microsoft Learn, [dotnet nuget locals](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-locals))
   - The lower-friction alternative to clearing caches on every iteration, standard in the community and consistent with NuGet's own SemVer pre-release guidance (§3): bump to a throwaway pre-release suffix each time you repack for local testing (`0.1.0-localtest.1`, `.2`, ...) — a new version string can never collide with a cached one, so no cache-clearing is needed mid-loop.

Practical loop for uCodeFirst: `dotnet pack src/uCodeFirst/uCodeFirst.csproj -o ./local-feed`, add `./local-feed` as a source in a scratch consuming project (or point `samples/Basicv17` at it temporarily via `nuget.config` instead of the `ProjectReference`), restore, and exercise it — clearing `global-packages` or bumping the version between iterations.

## 7. Umbraco Marketplace listing

Checked directly against `docs.umbraco.com/umbraco-dxp/marketplace/listing-your-package` (fetched 2026-07-21; the CMS docs' own "Creating a Package" pages link out to this as the authoritative marketplace space). Listing is **semi-automatic, not a manual submission form**:

- **Required:** the NuGet package must carry the literal tag **`umbraco-marketplace`**, and must declare a **direct NuGet dependency** on one of `Umbraco.Cms.*` (e.g. `Umbraco.Cms.Core`), `UmbracoCms.*` (legacy, Umbraco 8 only), or `Umbraco.Commerce.*`. Without the dependency, the tag alone does **not** get a package listed.
- uCodeFirst already satisfies the dependency requirement (`Umbraco.Cms.Web.Common`), and just needs the tag added (see §2's suggested `PackageTags` edit, which already includes it).
- **All display metadata is pulled straight from the NuGet listing** — package name, icon, authors, description, README, and project URL — meaning the metadata work in §2 (readme, project URL, description) directly is the marketplace listing content; there's no separate marketplace-specific metadata to author unless you want the *optional* enhanced `umbraco-marketplace.json` file hosted at your project URL for extra fields.
- **Supported-version detection is automatic from the dependency range.** The marketplace parses the `Umbraco.Cms.*` dependency's version range to display which Umbraco versions the package supports — e.g. a range like `[17.4.2, 18.0.0)` (uCodeFirst's actual current range) reads as "supports Umbraco 17."
- **Sync cadence:** new tagged packages are scanned in roughly every 24 hours (04:00 UTC); already-listed package metadata refreshes every ~2 hours; download counts every ~1 hour. A manual re-sync HTTP POST endpoint exists, throttled to once/minute per package, for forcing an immediate refresh after a metadata fix rather than waiting for the next scheduled pass.
- **No review/approval gate is described** for the open-source-package path — tag + dependency is sufficient to be picked up.

## Recommended order of operations

1. **Create/confirm the nuget.org account** (§1) — needed before anything else, including even checking whether Trusted Publishing is available on it yet.
2. **Add the metadata `.csproj` edits from §2** (license expression, project URL, readme pack item, copyright, the `umbraco-marketplace` tag) — cheap, no external dependency, and this *is* the Marketplace listing prep (§7) at the same time.
3. **Decide the version for first publish** per §3 — either continue the `0.x-alpha` pre-release track a while longer, or if the API is considered stable enough, cut straight to `17.0.0` to start the Umbraco-major-tracking convention immediately rather than migrating version schemes later.
4. **Add Source Link + symbol package properties** (§5) — small, mechanical, and worth doing before the *first* published version rather than retrofitting, since every version from then on benefits.
5. **Pack locally and test via a local feed** (§6) before ever touching nuget.org, using a throwaway pre-release suffix to sidestep cache issues.
6. **Create a scoped, push-only, glob-restricted API key** (§1) as a repository secret, and write the GitHub Actions `on: release: types: [published]` workflow (§4) using it — this works immediately regardless of Trusted Publishing rollout status.
7. **Check the nuget.org account for the Trusted Publishing option** (§4) once steps above are working end-to-end; if present, swap the workflow's push step from the stored API key to `NuGet/login` + a Trusted Publishing policy scoped to the exact repo + workflow filename, and delete the stored secret.
8. **Apply for ID prefix reservation** (§1) only after the package has shipped at least one real version and the ID's long-term shape is settled — this is a manual, human-reviewed process with no urgency to front-load.
9. **Verify the Marketplace listing appears** (§7) within ~24 hours of the first tagged publish, or use the manual re-sync endpoint if the metadata needs a faster refresh after a fix.

## Open questions / risks

- **Trusted Publishing's rollout status is account-specific and not something this research could verify further** — nuget.org's own docs page explicitly says it may not be visible yet on a given account ("We're rolling it out gradually"). The recommendation above (API key first, migrate later) sidesteps needing to know the answer in advance, but whoever executes step 6 above should actually check the account UI rather than assume either way.
- **No icon exists yet** for `PackageIcon` — left as a `CONSIDER`-tier optional per Microsoft's own guidance, not blocking.
- **`Company` property** was intentionally left out of the recommended additions — it's for organizational packages, and this appears to be an individual/solo OSS project (`Authors: Josef Härdelin`); revisit if that changes.
- **Version-scheme migration risk:** if uCodeFirst publishes several `0.x`-series versions before adopting the `17.x` (Umbraco-major-tracking) scheme, jumping straight to `17.0.0` is a large visible jump from `0.x` — this is exactly what uSync itself does across its history (its earliest versions were `8.x`/`9.x` for early Umbraco versions before the current major-tracking convention fully solidified), so it's a precedented, not unusual, jump — but worth calling out explicitly in release notes when it happens so consumers don't read `17.0.0` as "17 major versions of iteration."
- **`samples/Basicv17/Basicv17/Basicv17.csproj` missing `IsPackable=false`** is flagged in §4 as a latent (not currently triggered) risk — confirmed by reading the file directly; no other project in the repo has this gap (`tests/uCodeFirst.Tests.csproj` already sets it correctly).
