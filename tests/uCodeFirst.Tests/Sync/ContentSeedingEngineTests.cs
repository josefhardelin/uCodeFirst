using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using uCodeFirst.Attributes;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace uCodeFirst.Tests.Sync;

[TestFixture]
public class ContentSeedingEngineTests
{
    private static readonly IShortStringHelper Helper = new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));

    [DocumentType(Name: "Site Settings Page", Alias: "siteSettingsPage", Guid = "30000000-0000-0000-0000-000000000001")]
    private sealed class SiteSettingsPageFixture { }

    private static IContentType MakeContentType(string alias) =>
        new ContentType(Helper, parentId: -1) { Alias = alias, Name = alias };

    private static SeedContentDefinition Seed(Type clrType, Guid key, string name, Type? parent = null) =>
        new(ClrType: clrType, Key: key, DocumentType: typeof(SiteSettingsPageFixture), Name: name, Parent: parent);

    [Test]
    public async Task SyncAsync_CreatesNewStub_WhenAbsent()
    {
        var service = new FakeContentService();
        var engine = new ContentSeedingEngine(service, NullLogger<ContentSeedingEngine>.Instance);
        var key = Guid.Parse("40000000-0000-0000-0000-000000000001");

        await engine.SyncAsync(new[] { Seed(typeof(int), key, "Site Settings") });

        Assert.That(service.CreateCallCount, Is.EqualTo(1));
        Assert.That(service.SaveCallCount, Is.EqualTo(1));
        Assert.That(service.PublishCallCount, Is.EqualTo(1));

        var created = service.GetById(key);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Name, Is.EqualTo("Site Settings"));
        Assert.That(created.ParentId, Is.EqualTo(Constants.System.Root));
    }

    [Test]
    public async Task SyncAsync_SkipsCreate_WhenNodeWithKeyAlreadyExists()
    {
        var existingKey = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var existing = new Content("Existing Node", -1, MakeContentType("siteSettingsPage")) { Key = existingKey };
        var service = new FakeContentService(existing);
        var engine = new ContentSeedingEngine(service, NullLogger<ContentSeedingEngine>.Instance);

        await engine.SyncAsync(new[] { Seed(typeof(int), existingKey, "Site Settings") });

        Assert.That(service.CreateCallCount, Is.EqualTo(0));
        Assert.That(service.SaveCallCount, Is.EqualTo(0));
        Assert.That(service.PublishCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SyncAsync_ResolvesParentSeedOrdering_ForThreeLevelChain()
    {
        var service = new FakeContentService();
        var engine = new ContentSeedingEngine(service, NullLogger<ContentSeedingEngine>.Instance);

        var grandparentKey = Guid.Parse("40000000-0000-0000-0000-000000000010");
        var parentKey = Guid.Parse("40000000-0000-0000-0000-000000000011");
        var childKey = Guid.Parse("40000000-0000-0000-0000-000000000012");

        var grandparent = Seed(typeof(GrandparentMarker), grandparentKey, "Grandparent");
        var parent = Seed(typeof(ParentMarker), parentKey, "Parent", parent: typeof(GrandparentMarker));
        var child = Seed(typeof(ChildMarker), childKey, "Child", parent: typeof(ParentMarker));

        // Deliberately out of dependency order — the engine must still resolve parent-before-child.
        await engine.SyncAsync(new[] { child, parent, grandparent });

        Assert.That(service.CreateCallCount, Is.EqualTo(3));

        var grandparentContent = service.GetById(grandparentKey)!;
        var parentContent = service.GetById(parentKey)!;
        var childContent = service.GetById(childKey)!;

        Assert.That(grandparentContent.ParentId, Is.EqualTo(Constants.System.Root));
        Assert.That(parentContent.ParentId, Is.EqualTo(grandparentContent.Id));
        Assert.That(childContent.ParentId, Is.EqualTo(parentContent.Id));
    }

    private sealed class GrandparentMarker { }
    private sealed class ParentMarker { }
    private sealed class ChildMarker { }

    // Minimal fake covering only what ContentSeedingEngine calls (GetById(Guid)/Create/Save/Publish).
    // Everything else throws if ever exercised — see the same convention in
    // Sync/MediaTypeSyncEngineTests.cs's FakeMediaTypeService and Sync/DataTypeSyncEngineTests.cs's
    // FakeDataTypeService.
    private sealed class FakeContentService : IContentService
    {
        private readonly Dictionary<Guid, IContent> _byKey;
        private int _nextId = 1;

        public FakeContentService(params IContent[] existing) => _byKey = existing.ToDictionary(c => c.Key);

        public int CreateCallCount { get; private set; }
        public int SaveCallCount { get; private set; }
        public int PublishCallCount { get; private set; }

        public IContent Create(string name, int parentId, string contentTypeAlias, int userId = -1)
        {
            CreateCallCount++;
            return new Content(name, parentId, MakeContentType(contentTypeAlias));
        }

        public OperationResult Save(IContent content, int? userId = null, ContentScheduleCollection? contentSchedule = null)
        {
            SaveCallCount++;
            content.Id = _nextId++;
            _byKey[content.Key] = content;
            return new OperationResult(OperationResultType.Success, null);
        }

        public PublishResult Publish(IContent content, string[] cultures, int userId = -1)
        {
            PublishCallCount++;
            content.Published = true;
            return new PublishResult(PublishResultType.SuccessPublish, null, content);
        }

        public IContent? GetById(Guid key) => _byKey.GetValueOrDefault(key);

        // --- Everything below is unused by ContentSeedingEngine; throws if ever exercised. -------

        public OperationResult Rollback(int id, int versionId, string culture = "*", int userId = -1) => throw new NotImplementedException();
        public IContent? GetBlueprintById(int id) => throw new NotImplementedException();
        public IContent? GetBlueprintById(Guid id) => throw new NotImplementedException();
        public IEnumerable<IContent> GetBlueprintsForContentTypes(params int[] documentTypeId) => throw new NotImplementedException();
        [Obsolete] public void SaveBlueprint(IContent content, int userId = -1) => throw new NotImplementedException();
        public void DeleteBlueprint(IContent content, int userId = -1) => throw new NotImplementedException();
        [Obsolete] public IContent CreateContentFromBlueprint(IContent blueprint, string name, int userId = -1) => throw new NotImplementedException();
        public void DeleteBlueprintsOfType(int contentTypeId, int userId = -1) => throw new NotImplementedException();
        public void DeleteBlueprintsOfTypes(IEnumerable<int> contentTypeIds, int userId = -1) => throw new NotImplementedException();
        public IContent? GetById(int id) => throw new NotImplementedException();
        public ContentScheduleCollection GetContentScheduleByContentId(int contentId) => throw new NotImplementedException();
        public void PersistContentSchedule(IContent content, ContentScheduleCollection contentSchedule) => throw new NotImplementedException();
        public IEnumerable<IContent> GetByIds(IEnumerable<int> ids) => throw new NotImplementedException();
        public IEnumerable<IContent> GetByIds(IEnumerable<Guid> ids) => throw new NotImplementedException();
        public IEnumerable<IContent> GetByLevel(int level) => throw new NotImplementedException();
        public IContent? GetParent(int id) => throw new NotImplementedException();
        public IContent? GetParent(IContent content) => throw new NotImplementedException();
        public IEnumerable<IContent> GetAncestors(int id) => throw new NotImplementedException();
        public IEnumerable<IContent> GetAncestors(IContent content) => throw new NotImplementedException();
        public IEnumerable<IContent> GetVersions(int id) => throw new NotImplementedException();
        public IEnumerable<IContent> GetVersionsSlim(int id, int skip, int take) => throw new NotImplementedException();
        public IEnumerable<int> GetVersionIds(int id, int topRows) => throw new NotImplementedException();
        public IContent? GetVersion(int versionId) => throw new NotImplementedException();
        public IEnumerable<IContent> GetRootContent() => throw new NotImplementedException();
        public IEnumerable<IContent> GetContentForExpiration(DateTime date) => throw new NotImplementedException();
        public IEnumerable<IContent> GetContentForRelease(DateTime date) => throw new NotImplementedException();
        public IEnumerable<IContent> GetPagedContentInRecycleBin(long pageIndex, int pageSize, out long totalRecords, IQuery<IContent>? filter = null, Ordering? ordering = null) => throw new NotImplementedException();
        [Obsolete] public IEnumerable<IContent> GetPagedChildren(int id, long pageIndex, int pageSize, out long totalRecords, IQuery<IContent>? filter = null, Ordering? ordering = null) => throw new NotImplementedException();
        public IEnumerable<IContent> GetPagedDescendants(int id, long pageIndex, int pageSize, out long totalRecords, IQuery<IContent>? filter = null, Ordering? ordering = null) => throw new NotImplementedException();
        public IEnumerable<IContent> GetPagedOfType(int contentTypeId, long pageIndex, int pageSize, out long totalRecords, IQuery<IContent> filter, Ordering? ordering = null) => throw new NotImplementedException();
        public IEnumerable<IContent> GetPagedOfTypes(int[] contentTypeIds, long pageIndex, int pageSize, out long totalRecords, IQuery<IContent>? filter, Ordering? ordering = null) => throw new NotImplementedException();
        public int Count(string? documentTypeAlias = null) => throw new NotImplementedException();
        public int CountPublished(string? documentTypeAlias = null) => throw new NotImplementedException();
        public int CountChildren(int parentId, string? documentTypeAlias = null) => throw new NotImplementedException();
        public int CountDescendants(int parentId, string? documentTypeAlias = null) => throw new NotImplementedException();
        public bool HasChildren(int id) => throw new NotImplementedException();
        public OperationResult Save(IEnumerable<IContent> contents, int userId = -1) => throw new NotImplementedException();
        Attempt<OperationResult?> IContentServiceBase<IContent>.Save(IEnumerable<IContent> contents, int userId) => throw new NotImplementedException();
        public OperationResult Delete(IContent content, int userId = -1) => throw new NotImplementedException();
        public void DeleteOfType(int documentTypeId, int userId = -1) => throw new NotImplementedException();
        public void DeleteOfTypes(IEnumerable<int> contentTypeIds, int userId = -1) => throw new NotImplementedException();
        public void DeleteVersions(int id, DateTime date, int userId = -1) => throw new NotImplementedException();
        public void DeleteVersion(int id, int versionId, bool deletePriorVersions, int userId = -1) => throw new NotImplementedException();
        public OperationResult Move(IContent content, int parentId, int userId = -1) => throw new NotImplementedException();
        public IContent? Copy(IContent content, int parentId, bool relateToOriginal, int userId = -1) => throw new NotImplementedException();
        public IContent? Copy(IContent content, int parentId, bool relateToOriginal, bool recursive, int userId = -1) => throw new NotImplementedException();
        public OperationResult MoveToRecycleBin(IContent content, int userId = -1) => throw new NotImplementedException();
        public OperationResult EmptyRecycleBin(int userId = -1) => throw new NotImplementedException();
        public bool RecycleBinSmells() => throw new NotImplementedException();
        public OperationResult Sort(IEnumerable<IContent> items, int userId = -1) => throw new NotImplementedException();
        public OperationResult Sort(IEnumerable<int>? ids, int userId = -1) => throw new NotImplementedException();
        public IEnumerable<PublishResult> PublishBranch(IContent content, PublishBranchFilter publishBranchFilter, string[] cultures, int userId = -1) => throw new NotImplementedException();
        public PublishResult Unpublish(IContent content, string? culture = "*", int userId = -1) => throw new NotImplementedException();
        public bool IsPathPublishable(IContent content) => throw new NotImplementedException();
        public bool IsPathPublished(IContent content) => throw new NotImplementedException();
        public bool SendToPublication(IContent? content, int userId = -1) => throw new NotImplementedException();
        public IEnumerable<PublishResult> PerformScheduledPublish(DateTime date) => throw new NotImplementedException();
        public EntityPermissionCollection GetPermissions(IContent content) => throw new NotImplementedException();
        public void SetPermissions(EntityPermissionSet permissionSet) => throw new NotImplementedException();
        public void SetPermission(IContent entity, string permission, IEnumerable<int> groupIds) => throw new NotImplementedException();
        public IContent Create(string name, Guid parentId, string documentTypeAlias, int userId = -1) => throw new NotImplementedException();
        public IContent Create(string name, int parentId, IContentType contentType, int userId = -1) => throw new NotImplementedException();
        public IContent Create(string name, IContent? parent, string documentTypeAlias, int userId = -1) => throw new NotImplementedException();
        public IContent CreateAndSave(string name, int parentId, string contentTypeAlias, int userId = -1) => throw new NotImplementedException();
        public IContent CreateAndSave(string name, IContent parent, string contentTypeAlias, int userId = -1) => throw new NotImplementedException();
        public Task<OperationResult> EmptyRecycleBinAsync(Guid userId) => throw new NotImplementedException();
        public ContentDataIntegrityReport CheckDataIntegrity(ContentDataIntegrityReportOptions options) => throw new NotImplementedException();
    }
}
