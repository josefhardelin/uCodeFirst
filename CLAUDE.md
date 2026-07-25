# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Sample project

`samples/Basicv17/` is a working Umbraco site that demonstrates the library API. **Always update it when a public API changes** — attribute signatures, namespace moves, new required patterns, etc. The sample should compile and reflect the current API at all times. It references the library via a local `ProjectReference`.

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a single test (by name filter)
dotnet test --filter "FullyQualifiedName~TestClassName"

# Pack the library
dotnet pack src/uCodeFirst/uCodeFirst.csproj
```

Target framework is `net10.0` across all projects (set in `Directory.Build.props`). Nullable and implicit usings are enabled solution-wide.

## Architecture

This is a library (`src/uCodeFirst`) that syncs C# class definitions into an Umbraco 17+ database on startup. The test project lives in `tests/uCodeFirst.Tests` (NUnit). A sample Umbraco site sits in `samples/Basicv17`.

### Startup flow

1. `CodeFirstComposer` (auto-discovered by Umbraco's `IComposer` mechanism) calls `builder.AddCodeFirst()`.
2. `AddCodeFirst()` registers all services and wires `CodeFirstStartupHandler` to `UmbracoApplicationStartedNotification`.
3. On startup, `CodeFirstStartupHandler` calls `CodeFirstSyncService.SyncAsync(assemblies)` — skips if the runtime level is not `Run` (DB not yet installed).

### Sync pipeline (`Sync/`)

`CodeFirstSyncService` orchestrates three sequential steps:

1. **Scan** — `DocumentTypeScanner` reflects over all loaded assemblies and builds `DocumentTypeDefinition` records from classes/interfaces decorated with `[DocumentType]`, `[ElementType]`, or `[CompositionType]`.
2. **Validate** — `PreFlightValidator` checks for duplicate aliases/GUIDs, duplicate property aliases, and dangling `[AllowedChildren]`/block-type/composition references. Throws a single aggregated error on any failure; sync never partially applies.
3. **Sync** — Two engines run in order:
   - `DataTypeSyncEngine`: ensures shared Umbraco data types exist (keyed by a deterministic MD5 GUID derived from editor alias + config fingerprint).
   - `ContentTypeSyncEngine`: create/update content types in three passes — (1) create/update all types and folders, (2) wire `AllowedChildren`, (3) wire compositions.

### Attribute → editor mapping (`Sync/EditorRecipeResolver.cs`)

Each property-editor attribute (`[TextString]`, `[RichText]`, `[Dropdown]`, `[BlockList]`, `[BlockGrid]`, etc.) resolves to an `EditorRecipe` containing the Umbraco editor alias, UI alias, configuration, and a deterministic GUID. Adding a new property editor means adding an attribute in `Attributes/`, a case in `EditorRecipeResolver.Resolve()`, and handling in `DataTypeSyncEngine`.

### Compositions

Compositions are modelled as C# interfaces marked with `[CompositionType]`. A document/element type class implements those interfaces to inherit the composition. The scanner excludes interface properties from the implementing class's own property list to avoid duplication.

### Folders

Backoffice folder paths (e.g. `"Pages/Articles"`) are specified on `[DocumentType(Folder: "...")]`. Folder GUIDs are derived deterministically via MD5 from `consid.codefirst:folder:<path>` so they are stable across restarts.

### Backoffice dry-run dashboard

`CodeFirstSyncService.ComputePlanAsync` computes the same create/update/prune plan the startup log prints, but returns it as a serializable `CodeFirstPlanResult` instead of only logging it. `Api/PlanCodeFirstController` (`GET /umbraco/management/api/v1/code-first/plan`) exposes a live computation of that DTO, authenticated via the inherited `ManagementApiControllerBase` backoffice policies, and works regardless of the `Enabled` setting. A Lit web component (`src/uCodeFirst/wwwroot/App_Plugins/uCodeFirst/plan-dashboard.element.js` + `umbraco-package.json`) registers under the Settings section and renders the plan, with a manual "run dry-run now" button. Shipping these static backoffice assets required switching `uCodeFirst.csproj` to the `Microsoft.NET.Sdk.Razor` SDK (Static Web Assets packaging) and adding an `Umbraco.Cms.Api.Management` package reference.

## Verification

Do not run the sample site (`dotnet run` in `samples/Basicv17`) to verify changes. Verify by building
(`dotnet build`) and reading the code/generated behavior instead.

## Dev vs production

The sync should only run in development. The recommended guard in the consuming site's `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
    umbracoBuilder.AddCodeFirst();
```

In production, use uSync to import `.uSync` files generated from the dev environment.
