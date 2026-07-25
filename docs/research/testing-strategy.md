# Testing Strategy for uCodeFirst

**Researched:** 2026-07-20
**Update (2026-07-25):** `tests/uCodeFirst.Tests` has since been migrated wholesale from xUnit to NUnit
(see [mvp-and-roadmap.md](../../plan/mvp-and-roadmap.md)), following the "second option" below rather than
adding a separate NUnit project — the rest of this document's analysis (sections 1-4, package/sketch
recommendations) still reflects the research as originally written and is otherwise unchanged.
**Goal:** Stop verifying sync behavior by booting `samples/Basicv17` and clicking through the backoffice. Establish a layered automated test strategy: pure unit tests for reflection/validation logic (no Umbraco at all), interface-mocked tests for the sync engines, and — if worthwhile — a thin slice of real SQLite-backed integration tests for the riskiest paths.

## Executive summary

uCodeFirst's own logic (`DocumentTypeScanner`, `PreFlightValidator`) has **zero dependency on Umbraco services** and should be unit-tested today with plain xUnit + in-memory test assemblies — no package additions needed beyond a mocking library. The four sync engines (`DataTypeSyncEngine`, `ContentTypeSyncEngine`, `MediaTypeSyncEngine`, `DictionaryItemSyncEngine`) depend **only on Umbraco interfaces** (`IContentTypeService`, `IDataTypeService`, `IMediaTypeService`, `IDictionaryItemService`, `ITemplateService`) plus a few small concrete helper types (`IShortStringHelper`, `PropertyEditorCollection`) — all realistically mockable with NSubstitute, so most of the sync logic can be verified without a database. Umbraco *does* publish real integration-test infrastructure (`Umbraco.Cms.Tests` and `Umbraco.Cms.Tests.Integration` on NuGet, SQLite-backed, confirmed compatible with net10.0/Umbraco 17 and already used this way by uSync's own v17 test suite) — but it is NUnit-based, boots a full generic host per test, and has a history of breakage for third-party consumers, so it should be added later, sparingly, for the handful of scenarios (folder creation, composition wiring) that are too risky to trust to mocks alone.

## 1. What Umbraco itself ships for testing services like IContentTypeService/IDataTypeService

Umbraco's own repo (`umbraco/Umbraco-CMS`) has four test projects under `tests/`, confirmed at tag `release-17.5.3`:

- `tests/Umbraco.Tests.Common` — packs as NuGet ID **`Umbraco.Cms.Tests`** (not `Umbraco.Cms.Tests.Common` — that name does not exist). Contains `Builders/` (e.g. `ContentTypeBuilder.cs`, `DataTypeBuilder.cs`, `MediaTypeBuilder.cs`, `DictionaryItemBuilder.cs`, `PropertyTypeBuilder.cs`, `ContentTypeSortBuilder.cs`, etc.) plus `TestHelperBase.cs`.
  Source: https://github.com/umbraco/Umbraco-CMS/blob/release-17.5.3/tests/Umbraco.Tests.Common/Umbraco.Tests.Common.csproj (`<PackageId>Umbraco.Cms.Tests</PackageId>`, deps: `NUnit`, `Moq`, `AutoFixture.AutoMoq`, `AutoFixture.NUnit3`).
- `tests/Umbraco.Tests.Integration` — packs as **`Umbraco.Cms.Tests.Integration`**. Contains the `Testing/` folder with `UmbracoIntegrationTestBase.cs`, `UmbracoIntegrationTest.cs`, `UmbracoIntegrationTestWithContent.cs`, `UmbracoIntegrationTestWithContentEditing.cs`, `SqliteTestDatabase.cs`, `SqlServerTestDatabase.cs`, `TestDatabaseFactory.cs`.
  Source: https://github.com/umbraco/Umbraco-CMS/blob/release-17.5.3/tests/Umbraco.Tests.Integration/Umbraco.Tests.Integration.csproj (`<PackageId>Umbraco.Cms.Tests.Integration</PackageId>`, deps: `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, `NUnit3TestAdapter`).
- `tests/Umbraco.Tests.UnitTests` and `tests/Umbraco.Tests.Benchmarks` — internal only, not packed, not published (no `PackageId`/`IsPackable` in their structure the way the two above have it).

**`UmbracoIntegrationTest`** (`Testing/UmbracoIntegrationTest.cs`, https://github.com/umbraco/Umbraco-CMS/blob/release-17.5.3/tests/Umbraco.Tests.Integration/Testing/UmbracoIntegrationTest.cs) is the base class real integration tests derive from. Per its `[SetUp] Setup()` method, **every single test** builds a fresh `IHostBuilder` via `Host.CreateDefaultBuilder().ConfigureUmbracoDefaults()`, calls `AddUmbracoCore().AddWebComponents().AddBackOfficeCookieAuthentication().AddBackOfficeOpenIddictServices().AddBackOfficeIdentity().AddMembersIdentity().AddExamine().AddUmbracoSqlServerSupport().AddUmbracoSqliteSupport().AddUmbracoHybridCache()`, then starts the host and attaches a test database. This is a **full Umbraco DI graph boot per test**, not a lightweight shim — "fast" is relative to a manually-driven browser session, not to a pure unit test.

**SQLite backing** is confirmed in `SqliteTestDatabase.cs` (https://github.com/umbraco/Umbraco-CMS/blob/release-17.5.3/tests/Umbraco.Tests.Integration/Testing/SqliteTestDatabase.cs): it uses `Microsoft.Data.Sqlite` connections, and — notably — pre-builds a pool of schema/empty databases on background threads (`PrepareDatabase` threads, `_settings.PrepareThreadCount`) ahead of time so individual tests attach to an already-prepared DB rather than paying full schema-creation cost inline. The `[UmbracoTest(Database = ...)]` attribute selects `None | NewEmptyPerFixture | NewEmptyPerTest | NewSchemaPerFixture | NewSchemaPerTest`.

**Framework note:** both packages are **NUnit**-based (`NUnit3TestAdapter`, `[TestFixture]`, `[SetUp]`/`[TearDown]`), not xUnit. `tests/uCodeFirst.Tests` in this repo currently references xUnit 2.9.3 (see `/Users/josefhardelin/Code/Consid/uCodeFirst/tests/uCodeFirst.Tests/uCodeFirst.Tests.csproj`). Consuming Umbraco's integration-test base classes from an xUnit project is not viable — NUnit attributes drive the lifecycle (`[SetUp]`, `[OneTimeTearDown]`, `[SingleThreaded]`) — so any SQLite-backed integration tests would need either a **second, NUnit-based test project**, or migrating `uCodeFirst.Tests` to NUnit wholesale.

## 2. Official documented patterns for lightweight-runtime integration tests

Umbraco does document this, but the URL structure has moved across versions — the correct v17 path is:
https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/testing-and-debugging/integration-testing
(older `.../implementation/integration-testing` paths 404 as of this writing; the docs site redirects/renamed the section under "Develop with Umbraco → Testing and debugging").

Per that page:
- Create a new test project, install `Umbraco.Cms.Tests.Integration` (their docs sample pins NUnit `3.14.0`), add an `appsettings.Tests.Local.json` and a `GlobalSetupTeardown` class.
- Derive from `UmbracoIntegrationTest` for service-level tests (`GetRequiredService<IContentTypeService>()` etc.), or from `UmbracoTestServerTestBase` for tests that go through an actual HTTP request into a controller.
- Decorate the fixture with `[TestFixture] [UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]`.

There is **no documented "no-runtime-at-all" pattern from Umbraco** for exercising `IContentTypeService`/`IDataTypeService` — Umbraco's own official story for testing against those services is exactly this NUnit + SQLite `UmbracoIntegrationTest` route. Nothing lighter is offered as a first-party alternative; the lighter option here is not to use Umbraco services at all in the test, which is why isolating uCodeFirst's own pure logic (section 3) is the primary lever, not something to look for further afield in Umbraco's docs.

## 3. Testing this repo's components in isolation

Source read directly (paths, this repo):
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Discovery/DocumentTypeScanner.cs`
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Validation/PreFlightValidator.cs`
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Sync/CodeFirstSyncService.cs`
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Sync/DataTypeSyncEngine.cs`
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Sync/ContentTypeSyncEngine.cs`
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Sync/MediaTypeSyncEngine.cs`
- `/Users/josefhardelin/Code/Consid/uCodeFirst/src/uCodeFirst/Sync/DictionaryItemSyncEngine.cs`

### DocumentTypeScanner — pure reflection, zero Umbraco dependency

`internal sealed class DocumentTypeScanner` (`Discovery/DocumentTypeScanner.cs`) exposes `Scan(IEnumerable<Assembly>)`, `ScanMediaTypes(...)`, `ScanDictionaryItems(...)`, all pure functions over `Assembly.GetTypes()` and attribute reflection (`[DocumentType]`, `[ElementType]`, `[CompositionType]`, `[MediaType]`, `[DictionaryItem]`, `[AllowedChildren]`, `[Group]`). It takes no constructor dependencies at all — it's `new DocumentTypeScanner()` and go. This is 100% testable by defining tiny fixture classes/interfaces *inside the test assembly itself*, calling `scanner.Scan(new[] { typeof(MyFixture).Assembly })`, and asserting on the returned `DocumentTypeDefinition`/`PropertyDefinition` records. No Umbraco runtime, no mocking, sub-millisecond per test.

### PreFlightValidator — pure validation over records, zero I/O

`internal sealed class PreFlightValidator` (`Validation/PreFlightValidator.cs`) exposes `Validate(IReadOnlyList<DocumentTypeDefinition>, IReadOnlyList<MediaTypeDefinition>?, IReadOnlyList<DictionaryItemDefinition>?)` returning `IReadOnlyList<string>` error messages. It only touches the definition records and `Type` reflection (`GetCustomAttribute<DocumentTypeAttribute>()` on referenced CLR types for `[AllowedChildren]`/block/composition checks) — no Umbraco services. Fully testable by hand-constructing `DocumentTypeDefinition` records (they're just C# records) and asserting on the error list — no scanner or Umbraco needed at all, though pairing it with `DocumentTypeScanner` output against small fixture assemblies gives more realistic coverage of the duplicate-alias/duplicate-GUID/dangling-reference paths.

### Sync engines — Umbraco-service dependent, but the dependencies are thin interfaces

Checked constructor signatures:
- `DataTypeSyncEngine(IDataTypeService, PropertyEditorCollection, IConfigurationEditorJsonSerializer, ILogger<...>)`
- `ContentTypeSyncEngine(IContentTypeService, ITemplateService, IShortStringHelper, ILogger<...>)`
- `MediaTypeSyncEngine(IMediaTypeService, IShortStringHelper, ILogger<...>)`
- `DictionaryItemSyncEngine(IDictionaryItemService, ILogger<...>)`

Verified against Umbraco source (`release-17.5.3`) that `IContentTypeService`, `IDataTypeService`, `IMediaTypeService`, `IDictionaryItemService`, `ITemplateService` are plain public interfaces (e.g. `IContentTypeService : IContentTypeBaseService<IContentType>`, https://github.com/umbraco/Umbraco-CMS/blob/release-17.5.3/src/Umbraco.Core/Services/IContentTypeService.cs) with no sealed/internal blockers — straightforward to mock with **NSubstitute** (not currently referenced by `tests/uCodeFirst.Tests.csproj`; would need to be added). `ContentType`, `DataType`, `MediaType` (the concrete Umbraco model types the engines `new` up directly, e.g. `new ContentType(_shortStringHelper, parentId: parentId)` in `ContentTypeSyncEngine.CreateAsync`) are **public, non-sealed classes** (confirmed: `public class ContentType : ContentTypeCompositionBase, IContentType`, `public class DataType : TreeEntityBase, IDataType` in Umbraco.Core), so they're directly constructible in a test without any Umbraco host — the only real friction is that `ContentType`/`PropertyType` need a real `IShortStringHelper` (cheap to construct: `new ShortStringHelper(new DefaultShortStringHelperConfig())`, no DB) and `DataType` needs an `IDataEditor` from `PropertyEditorCollection` (satisfiable with a fake/mock `IDataEditor`, or a real minimal one since `IDataEditor` is also just an interface).

**Conclusion:** all four sync engines are realistic to unit-test with mocked service interfaces + a real (cheap) `ShortStringHelper`. This covers create/update/skip-existing branching logic, folder path building, `AllowedChildren` wiring, composition add/remove logic, and error-logging paths — the bulk of the engines' cyclomatic complexity — with zero database.

What mocking **cannot** cover well: Umbraco's real validation/persistence rules inside `IContentTypeService.CreateAsync`/`UpdateAsync` (e.g. actual alias collision detection at the DB layer, actual container/folder ID semantics, actual composition-conflict detection Umbraco itself enforces). Those are the candidates for the thin SQLite integration slice in the recommendations below.

## 4. Community precedent: how other Umbraco-adjacent libraries test against content-type services

**uSync** (`KevinJump/uSync`, default branch `v17/main`) is the clearest, most current example — it's a "code-first-adjacent"/schema-sync library for Umbraco, actively maintained for Umbraco 17.

- `uSync.Tests.csproj` (https://github.com/KevinJump/uSync/blob/v17/main/uSync.Tests/uSync.Tests.csproj) targets `net10.0` and references **exactly** `Umbraco.Cms.Tests` and `Umbraco.Cms.Tests.Integration` as `<PackageReference>` (no version pinned inline — managed via central package management), plus `NUnit3TestAdapter` and `Microsoft.NET.Test.Sdk`. This is a direct, current, real-world confirmation that these two package IDs are the correct ones, are consumable by a third party, and do support net10.0/Umbraco 17.
- Its own test suite (`uSync.Tests/Migrations/*`, `uSync.Tests/Extensions/*`, https://github.com/KevinJump/uSync/tree/v17/main/uSync.Tests) is largely **pure unit tests with no Umbraco runtime** — e.g. `MigrationTestBase.cs` (https://github.com/KevinJump/uSync/blob/v17/main/uSync.Tests/Migrations/MigrationTestBase.cs) exercises `IConfigurationSerializer.GetConfigurationImportAsync` directly against hand-built JSON strings and asserts on the JSON output, no host, no DB, no mocking framework at all. This mirrors the recommendation here: even a project that *has* the SQLite integration packages available leans mostly on pure-logic tests for the bulk of its suite.

This is a real, current sibling project doing the same category of work (declarative/code-driven Umbraco schema management) validating both the package choice and the "mostly-unit, some-integration" shape of the strategy.

## What to add

### Packages

To `tests/uCodeFirst.Tests/uCodeFirst.Tests.csproj` (keep xUnit; add a mocking library — this project currently has none):

```xml
<PackageReference Include="NSubstitute" Version="5.3.0" />
```

(NSubstitute over Moq: cleaner API for mocking the plain-interface Umbraco services here; either works since nothing in these interfaces is sealed/non-virtual.)

If/when the SQLite integration slice (below) is added, put it in a **new project** `tests/uCodeFirst.IntegrationTests` (NUnit-based, mirroring uSync's approach) rather than mixing frameworks in the existing xUnit project:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="NUnit" Version="4.x" />
<PackageReference Include="NUnit3TestAdapter" Version="4.x" />
<PackageReference Include="Umbraco.Cms.Tests" Version="17.5.3" />
<PackageReference Include="Umbraco.Cms.Tests.Integration" Version="17.5.3" />
```

Pin the `Umbraco.Cms.Tests*` versions to match (or be within) the `Umbraco.Cms.Web.Common` range already pinned in `src/uCodeFirst/uCodeFirst.csproj` (`[17.4.2, 18.0.0)`) — `17.5.3` was the newest tag confirmed to exist at time of writing.

### Sketch: fast unit test for DocumentTypeScanner (no Umbraco runtime)

```
// tests/uCodeFirst.Tests/Discovery/DocumentTypeScannerTests.cs
[DocumentType(Alias: "testPage", Key: "...", Name: "Test Page")]
private class TestPageFixture
{
    [TextString(...)] public string? Title { get; set; }
}

[Fact]
public void Scan_finds_document_type_and_its_properties()
{
    var scanner = new DocumentTypeScanner();

    var result = scanner.Scan(new[] { typeof(TestPageFixture).Assembly });

    var def = Assert.Single(result, d => d.ClrType == typeof(TestPageFixture));
    Assert.Equal("testPage", def.Alias);
    Assert.Contains(def.Properties, p => p.Alias == "title");
}
```
Additional cases worth covering the same way: composition interfaces get excluded from the implementing class's own property list; `[AllowedChildren]` types flow through untouched into `AllowedChildTypes`; dictionary item nested-static-class parent chains resolve correctly (`GetParentChain`); alias defaulting (`ToAlias`) lower-cases the first letter only.

### Sketch: fast unit test for PreFlightValidator (no Umbraco runtime)

```
// tests/uCodeFirst.Tests/Validation/PreFlightValidatorTests.cs
[Fact]
public void Validate_reports_duplicate_alias()
{
    var validator = new PreFlightValidator();
    var defA = new DocumentTypeDefinition(ClrType: typeof(A), Alias: "same", Key: guid1, ...);
    var defB = new DocumentTypeDefinition(ClrType: typeof(B), Alias: "same", Key: guid2, ...);

    var errors = validator.Validate(new[] { defA, defB });

    Assert.Contains(errors, e => e.Contains("Duplicate alias"));
}
```
Same pattern for: duplicate GUID, duplicate property alias within one type, `[AllowedChildren]` referencing a type with no `[DocumentType]`, referencing a type not in the scanned set, block/element-type reference validation, dangling composition key, dictionary-item key collisions between two different owners.

### Sketch: mocked-service unit test for ContentTypeSyncEngine (no database)

```
// tests/uCodeFirst.Tests/Sync/ContentTypeSyncEngineTests.cs
[Fact]
public async Task SyncAsync_creates_new_content_type_when_none_exists()
{
    var contentTypeService = Substitute.For<IContentTypeService>();
    var templateService = Substitute.For<ITemplateService>();
    var shortStringHelper = new ShortStringHelper(new DefaultShortStringHelperConfig());
    var logger = Substitute.For<ILogger<ContentTypeSyncEngine>>();

    contentTypeService.GetAsync(Arg.Any<Guid>()).Returns((IContentType?)null);
    contentTypeService.CreateAsync(Arg.Any<IContentType>(), Arg.Any<Guid>())
        .Returns(Attempt.Succeed(OperationResultType.Success, new ContentTypeOperationStatus(...)));
        // (exact Attempt<> shape per Umbraco 17 API — confirm signature before writing)

    var engine = new ContentTypeSyncEngine(contentTypeService, templateService, shortStringHelper, logger);
    var def = /* build a DocumentTypeDefinition fixture */;

    await engine.SyncAsync(new[] { def }, new Dictionary<Guid, IDataType>());

    await contentTypeService.Received(1).CreateAsync(
        Arg.Is<IContentType>(ct => ct.Alias == def.Alias),
        Arg.Any<Guid>());
}
```
This same shape covers: update-vs-create branching (`GetAsync` returns existing vs. null), `AllowedChildren` wiring (assert `contentType.AllowedContentTypes` after pass 2), composition add/remove (assert `AddContentType`/`RemoveContentType` calls), and folder creation (`GetContainer`/`CreateContainer` mocked to return null then a stub `EntityContainer`). Note: confirm the exact `Attempt<T,TStatus>`/result-type shapes for `CreateAsync`/`UpdateAsync` against the pinned Umbraco 17.4.2+ version before writing — these have changed across Umbraco major versions.

### Sketch: SQLite-backed integration test for ContentTypeSyncEngine (if pursued)

Feasible as a **third-party consumer** — uSync proves it — but flag clearly: it requires a **separate NUnit test project**, cannot share the xUnit project, and Umbraco's own GitHub discussion/issue history (cited above) shows this package has had real breakage for external consumers across minor versions, so pin the version and re-verify on every Umbraco upgrade.

```
// tests/uCodeFirst.IntegrationTests/ContentTypeSyncEngineIntegrationTests.cs
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTypeSyncEngineIntegrationTests : UmbracoIntegrationTest
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();
    private IShortStringHelper ShortStringHelper => GetRequiredService<IShortStringHelper>();

    [Test]
    public async Task SyncAsync_persists_new_content_type_to_real_database()
    {
        var engine = new ContentTypeSyncEngine(
            ContentTypeService,
            GetRequiredService<ITemplateService>(),
            ShortStringHelper,
            NullLogger<ContentTypeSyncEngine>.Instance);

        var def = /* build a DocumentTypeDefinition fixture, or scan a tiny fixture assembly */;

        await engine.SyncAsync(new[] { def }, new Dictionary<Guid, IDataType>());

        var persisted = await ContentTypeService.GetAsync(def.Key);
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Alias, Is.EqualTo(def.Alias));
    }
}
```
Reserve this tier for the handful of behaviors mocks can't credibly verify: real folder/container creation semantics, real composition-conflict rules Umbraco enforces server-side, and an end-to-end "run the whole `CodeFirstSyncService.SyncAsync` twice and assert idempotency" smoke test — not a full port of every branch already covered by the mocked unit tests.

## Open questions / risks

- **Package immaturity / third-party friction is real, not hypothetical.** GitHub discussion https://github.com/umbraco/Umbraco-CMS/discussions/20968 documents that `TestOptionAttributeBase` "expects an NUnit test method to be running" and breaks under setup fixtures, and that `DefaultUmbracoAssemblyProvider` assumes physical file locations that break for dynamic-proxy fixtures — both filed by a third-party consumer, not Umbraco core. GitHub issue https://github.com/umbraco/Umbraco-CMS/issues/19076 reports a `NullReferenceException` in `ConfigureServices` for third-party consumers of the package introduced in v15.2.0 and still open as of v15.4.0-rc2 (fixed by pinning back to v15.1.2). Neither is confirmed fixed at 17.5.3 — **verify with a throwaway spike project before committing engineering time to the SQLite integration tier.**
- **Umbraco 17 is very new; the test-infra packages are correspondingly fresh.** `Umbraco.Cms.Tests.Integration` shows a 17.5.3 release from 2026-07-07 with only ~468 downloads at time of writing — low adoption relative to the historical 13.x/15.x lines, meaning fewer people have hit and reported edge cases at this exact version.
- **Framework mismatch is a real decision point, not a footnote.** Umbraco's test infra is NUnit; this repo's existing `tests/uCodeFirst.Tests` is xUnit. Either keep the SQLite tier in a second NUnit project (recommended, mirrors uSync) or defer that tier entirely and rely on unit + mocked-interface tests, which is achievable purely in the existing xUnit project today with no framework change.
- **`CreateAsync`/`UpdateAsync` result-type shapes were not fully hand-verified against the exact pinned Umbraco 17.4.2 version in this session** — the pseudocode above flags this explicitly; confirm exact `Attempt<...>` generic parameters before writing real mocked tests, since Umbraco has changed these operation-status shapes across major versions.
- **Docs URL churn:** the "integration testing" docs page has moved paths across Umbraco doc versions (`.../implementation/integration-testing` → `.../develop-with-umbraco/testing-and-debugging/integration-testing`); if revisiting this research later, re-resolve the current URL rather than trusting a cached link.
