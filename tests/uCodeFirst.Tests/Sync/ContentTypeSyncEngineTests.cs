using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using uCodeFirst.Configuration;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

namespace uCodeFirst.Tests.Sync;

[TestFixture]
public class ContentTypeSyncEngineTests
{
    private static readonly Guid TypeKey = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly IShortStringHelper Helper = new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));

    private static IDataType MakeDataType() => new DataType(new FakeDataEditor(), new FakeJsonSerializer());

    private static Dictionary<Guid, IDataType> DataTypeByKey() =>
        new() { [new TextString().GetDescriptor().Key] = MakeDataType() };

    private static PropertyDefinition Property(string alias, string group = "Content") =>
        new(Alias: alias, Name: alias, GroupName: group, SortOrder: 0, Mandatory: false, Description: null, DataType: new TextString(), VariesByCulture: false);

    private static DocumentTypeDefinition Definition(
        IReadOnlyList<PropertyDefinition> properties,
        bool preventCleanup = false,
        int? keepAllVersionsNewerThanDays = null,
        int? keepLatestVersionPerDayForDays = null) =>
        new(
            ClrType: typeof(ContentTypeSyncEngineTests),
            IsElement: false,
            Key: TypeKey,
            Alias: "article",
            Name: "Article",
            Icon: null,
            Color: null,
            Description: null,
            AllowedAtRoot: true,
            Folder: null,
            DefaultTemplate: null,
            AllowedChildTypes: Array.Empty<Type>(),
            Properties: properties,
            CompositionKeys: Array.Empty<Guid>(),
            VariesByCulture: false,
            IsContainer: false,
            PreventCleanup: preventCleanup,
            KeepAllVersionsNewerThanDays: keepAllVersionsNewerThanDays,
            KeepLatestVersionPerDayForDays: keepLatestVersionPerDayForDays);

    // Existing content type in the "database": group "content" has properties [headline, legacyField];
    // group "extra" has only [obsoleteProp]. Current C# definitions in these tests only declare "headline",
    // so "legacyField" and "obsoleteProp" are stale, and "extra" would end up empty once obsoleteProp is pruned.
    private static ContentType BuildExistingContentType()
    {
        var contentType = new ContentType(Helper, parentId: -1) { Key = TypeKey, Alias = "article", Name = "Article" };
        var dataType = MakeDataType();

        var contentGroup = new PropertyGroup(isPublishing: true) { Alias = "content", Name = "Content", Type = PropertyGroupType.Tab, SortOrder = 0 };
        contentType.PropertyGroups.Add(contentGroup);
        contentType.AddPropertyType(new PropertyType(Helper, dataType, "headline") { Name = "Headline" }, "content", "Content");
        contentType.AddPropertyType(new PropertyType(Helper, dataType, "legacyField") { Name = "Legacy" }, "content", "Content");

        var extraGroup = new PropertyGroup(isPublishing: true) { Alias = "extra", Name = "Extra", Type = PropertyGroupType.Tab, SortOrder = 1 };
        contentType.PropertyGroups.Add(extraGroup);
        contentType.AddPropertyType(new PropertyType(Helper, dataType, "obsoleteProp") { Name = "Obsolete" }, "extra", "Extra");

        return contentType;
    }

    [Test]
    public async Task PlanAsync_NewContentType_IsToCreate()
    {
        var service = new FakeContentTypeService();
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        var plan = await engine.PlanAsync(new[] { Definition(new[] { Property("headline") }) }, CodeFirstStrategy.NonDestructive);

        Assert.That(plan.ToCreate, Has.Count.EqualTo(1));
        Assert.That(plan.ToCreate[0].Alias, Is.EqualTo("article"));
        Assert.That(plan.ToUpdate, Is.Empty);
    }

    [Test]
    public async Task PlanAsync_ExistingContentType_NonDestructive_IsUpdateWithNoPruning()
    {
        var service = new FakeContentTypeService(BuildExistingContentType());
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        var plan = await engine.PlanAsync(new[] { Definition(new[] { Property("headline") }) }, CodeFirstStrategy.NonDestructive);

        Assert.That(plan.ToUpdate, Has.Count.EqualTo(1));
        Assert.That(plan.PrunedProperties, Is.Empty);
        Assert.That(plan.PrunedGroups, Is.Empty);
    }

    [Test]
    public async Task PlanAsync_ExistingContentType_Destructive_ComputesStalePropertiesAndEmptyGroup()
    {
        var service = new FakeContentTypeService(BuildExistingContentType());
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        var plan = await engine.PlanAsync(new[] { Definition(new[] { Property("headline") }) }, CodeFirstStrategy.Destructive);

        Assert.That(plan.PrunedProperties.Count, Is.EqualTo(2));
        Assert.That(plan.PrunedProperties, Has.Some.Matches<PrunedProperty>(p => p.PropertyAlias == "legacyField"));
        Assert.That(plan.PrunedProperties, Has.Some.Matches<PrunedProperty>(p => p.PropertyAlias == "obsoleteProp"));

        Assert.That(plan.PrunedGroups, Has.Count.EqualTo(1));
        Assert.That(plan.PrunedGroups[0].GroupAlias, Is.EqualTo("extra"));
    }

    [Test]
    public async Task SyncAsync_CreatesNewContentType()
    {
        var service = new FakeContentTypeService();
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("headline") }) }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        Assert.That(service.CreateCallCount, Is.EqualTo(1));
        var created = await service.GetAsync(TypeKey);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.PropertyTypes, Has.Some.Matches<IPropertyType>(pt => pt.Alias == "headline"));
    }

    [Test]
    public async Task SyncAsync_NonDestructive_NeverRemovesStaleProperties()
    {
        var service = new FakeContentTypeService(BuildExistingContentType());
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("headline") }) }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        var updated = await service.GetAsync(TypeKey);
        Assert.That(updated!.PropertyTypes, Has.Some.Matches<IPropertyType>(pt => pt.Alias == "legacyField"));
        Assert.That(updated.PropertyTypes, Has.Some.Matches<IPropertyType>(pt => pt.Alias == "obsoleteProp"));
        Assert.That(updated.PropertyGroups, Has.Some.Matches<PropertyGroup>(g => g.Alias == "extra"));
    }

    [Test]
    public async Task SyncAsync_Destructive_RemovesStalePropertiesAndEmptyGroups()
    {
        var service = new FakeContentTypeService(BuildExistingContentType());
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("headline") }) }, DataTypeByKey(), CodeFirstStrategy.Destructive);

        var updated = await service.GetAsync(TypeKey);
        Assert.That(updated!.PropertyTypes, Has.Some.Matches<IPropertyType>(pt => pt.Alias == "headline"));
        Assert.That(updated.PropertyTypes, Has.None.Matches<IPropertyType>(pt => pt.Alias == "legacyField"));
        Assert.That(updated.PropertyTypes, Has.None.Matches<IPropertyType>(pt => pt.Alias == "obsoleteProp"));
        Assert.That(updated.PropertyGroups, Has.None.Matches<PropertyGroup>(g => g.Alias == "extra"));
        Assert.That(updated.PropertyGroups, Has.Some.Matches<PropertyGroup>(g => g.Alias == "content"));
    }

    // --- HistoryCleanup -------------------------------------------------------------------------

    [Test]
    public async Task SyncAsync_CreatesNewContentType_SetsHistoryCleanup()
    {
        var service = new FakeContentTypeService();
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        var definition = Definition(new[] { Property("headline") }, preventCleanup: true, keepAllVersionsNewerThanDays: 30, keepLatestVersionPerDayForDays: 90);
        await engine.SyncAsync(new[] { definition }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        var created = await service.GetAsync(TypeKey);
        Assert.That(created!.HistoryCleanup, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(created.HistoryCleanup!.PreventCleanup, Is.True);
            Assert.That(created.HistoryCleanup!.KeepAllVersionsNewerThanDays, Is.EqualTo(30));
            Assert.That(created.HistoryCleanup!.KeepLatestVersionPerDayForDays, Is.EqualTo(90));
        });
    }

    [Test]
    public async Task SyncAsync_CreatesNewContentType_WithoutHistoryCleanupParams_LeavesUmbracoDefaults()
    {
        var service = new FakeContentTypeService();
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("headline") }) }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        var created = await service.GetAsync(TypeKey);
        Assert.That(created!.HistoryCleanup, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(created.HistoryCleanup!.PreventCleanup, Is.False);
            Assert.That(created.HistoryCleanup!.KeepAllVersionsNewerThanDays, Is.Null);
            Assert.That(created.HistoryCleanup!.KeepLatestVersionPerDayForDays, Is.Null);
        });
    }

    [Test]
    public async Task SyncAsync_UpdatesExistingContentType_ChangesHistoryCleanupWhenValuesDiffer()
    {
        var existing = BuildExistingContentType();
        Assert.That(existing.HistoryCleanup, Is.Not.Null); // Umbraco default: PreventCleanup false, both day-counts null.

        var service = new FakeContentTypeService(existing);
        var engine = new ContentTypeSyncEngine(service, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);

        var definition = Definition(new[] { Property("headline") }, preventCleanup: true, keepAllVersionsNewerThanDays: 14, keepLatestVersionPerDayForDays: 60);
        await engine.SyncAsync(new[] { definition }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        var updated = await service.GetAsync(TypeKey);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.HistoryCleanup!.PreventCleanup, Is.True);
            Assert.That(updated.HistoryCleanup!.KeepAllVersionsNewerThanDays, Is.EqualTo(14));
            Assert.That(updated.HistoryCleanup!.KeepLatestVersionPerDayForDays, Is.EqualTo(60));
        });
    }

    // Minimal editor fake, only used to satisfy the DataType constructor — Alias is read via
    // PropertyType's constructor (dataType.EditorAlias); nothing else is exercised.
    private sealed class FakeDataEditor : IDataEditor
    {
        public string Alias => "test.editor";
        public bool IsDeprecated => false;
        public IDictionary<string, object>? DefaultConfiguration => null;
        public IPropertyIndexValueFactory PropertyIndexValueFactory => throw new NotImplementedException();
        public IDataValueEditor GetValueEditor() => throw new NotImplementedException();
        public IDataValueEditor GetValueEditor(object? configurationObject) => throw new NotImplementedException();
        public IConfigurationEditor GetConfigurationEditor() => new FakeConfigurationEditor();
    }

    // DataType's constructor eagerly reads GetConfigurationEditor().DefaultConfiguration — only member exercised.
    private sealed class FakeConfigurationEditor : IConfigurationEditor
    {
        public List<ConfigurationField> Fields => throw new NotImplementedException();
        public IDictionary<string, object> DefaultConfiguration { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> ToConfigurationEditor(IDictionary<string, object> configuration) => throw new NotImplementedException();
        public IDictionary<string, object> FromConfigurationEditor(IDictionary<string, object> configuration) => throw new NotImplementedException();
        public IDictionary<string, object> ToValueEditor(IDictionary<string, object> configuration) => throw new NotImplementedException();
        public object ToConfigurationObject(IDictionary<string, object> configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => throw new NotImplementedException();
        public IDictionary<string, object> FromConfigurationObject(object configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => throw new NotImplementedException();
        public string ToDatabase(IDictionary<string, object> configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => throw new NotImplementedException();
        public IDictionary<string, object> FromDatabase(string? configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => throw new NotImplementedException();
        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(IDictionary<string, object> configuration) => throw new NotImplementedException();
    }

    // Minimal JSON serializer fake, only used to satisfy the DataType constructor — never exercised.
    private sealed class FakeJsonSerializer : IConfigurationEditorJsonSerializer
    {
        public string Serialize(object? input) => System.Text.Json.JsonSerializer.Serialize(input);
        public T? Deserialize<T>(string input) => System.Text.Json.JsonSerializer.Deserialize<T>(input);
        public bool TryDeserialize<T>(object input, out T? value) where T : class
        {
            value = input as T;
            return value is not null;
        }
    }

    // Minimal fake covering only what ContentTypeSyncEngine calls in these tests (no folders, allowed
    // children, or compositions are exercised). Other members throw if ever hit.
    private sealed class FakeContentTypeService : IContentTypeService
    {
        private readonly Dictionary<Guid, IContentType> _byKey = new();

        public FakeContentTypeService(params IContentType[] existing)
        {
            foreach (var ct in existing)
                _byKey[ct.Key] = ct;
        }

        public int CreateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<IContentType?> GetAsync(Guid guid) => Task.FromResult(_byKey.GetValueOrDefault(guid));

        public Task<Attempt<ContentTypeOperationStatus>> CreateAsync(IContentType item, Guid performingUserKey)
        {
            CreateCallCount++;
            _byKey[item.Key] = item;
            return Task.FromResult(Attempt.Succeed(ContentTypeOperationStatus.Success));
        }

        public Task<Attempt<ContentTypeOperationStatus>> UpdateAsync(IContentType item, Guid performingUserKey)
        {
            UpdateCallCount++;
            _byKey[item.Key] = item;
            return Task.FromResult(Attempt.Succeed(ContentTypeOperationStatus.Success));
        }

        public IContentType? Get(int id) => throw new NotImplementedException();
        public IContentType? Get(Guid key) => throw new NotImplementedException();
        public IContentType? Get(string alias) => throw new NotImplementedException();
        public int Count() => throw new NotImplementedException();
        public bool HasContentNodes(int id) => throw new NotImplementedException();
        public IEnumerable<IContentType> GetAll() => throw new NotImplementedException();
        public IEnumerable<IContentType> GetMany(params int[] ids) => throw new NotImplementedException();
        public IEnumerable<IContentType> GetMany(IEnumerable<Guid>? ids) => throw new NotImplementedException();
        public IEnumerable<IContentType> GetDescendants(int id, bool andSelf) => throw new NotImplementedException();
        public IEnumerable<IContentType> GetComposedOf(int id) => throw new NotImplementedException();
        public IEnumerable<IContentType> GetChildren(int id) => throw new NotImplementedException();
        public IEnumerable<IContentType> GetChildren(Guid id) => throw new NotImplementedException();
        public bool HasChildren(int id) => throw new NotImplementedException();
        public bool HasChildren(Guid id) => throw new NotImplementedException();
        public void Save(IContentType? item, int userId = -1) => throw new NotImplementedException();
        public void Save(IEnumerable<IContentType> items, int userId = -1) => throw new NotImplementedException();
        public void Delete(IContentType item, int userId = -1) => throw new NotImplementedException();
        public Task<ContentTypeOperationStatus> DeleteAsync(Guid key, Guid performingUserKey) => throw new NotImplementedException();
        public void Delete(IEnumerable<IContentType> item, int userId = -1) => throw new NotImplementedException();
        public Attempt<string[]?> ValidateComposition(IContentType? compo) => throw new NotImplementedException();
        public bool HasContainerInPath(string contentPath) => throw new NotImplementedException();
        public bool HasContainerInPath(params int[] ids) => throw new NotImplementedException();
        public Attempt<OperationResult<OperationResultType, EntityContainer>?> CreateContainer(int parentContainerId, Guid key, string name, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult?> SaveContainer(EntityContainer container, int userId = -1) => throw new NotImplementedException();
        public EntityContainer? GetContainer(int containerId) => throw new NotImplementedException();
        public EntityContainer? GetContainer(Guid containerId) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(int[] containerIds) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(IContentType contentType) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(string folderName, int level) => throw new NotImplementedException();
        public Attempt<OperationResult?> DeleteContainer(int containerId, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult<OperationResultType, EntityContainer>?> RenameContainer(int id, string name, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult<MoveOperationStatusType>?> Move(IContentType moving, int containerId) => throw new NotImplementedException();
        public Attempt<OperationResult<MoveOperationStatusType, IContentType>?> Copy(IContentType copying, int containerId) => throw new NotImplementedException();
        public IContentType Copy(IContentType original, string alias, string name, int parentId = -1) => throw new NotImplementedException();
        public IContentType Copy(IContentType original, string alias, string name, IContentType parent) => throw new NotImplementedException();
        public Task<Attempt<IContentType?, ContentTypeStructureOperationStatus>> CopyAsync(Guid key, Guid? containerKey) => throw new NotImplementedException();
        public Task<Attempt<IContentType?, ContentTypeStructureOperationStatus>> MoveAsync(Guid key, Guid? containerKey) => throw new NotImplementedException();
        public Task<PagedModel<IContentType>> GetAllAllowedAsRootAsync(int skip, int take) => throw new NotImplementedException();
        public Task<Attempt<PagedModel<IContentType>?, ContentTypeOperationStatus>> GetAllowedChildrenAsync(Guid key, int skip, int take) => throw new NotImplementedException();
        public IEnumerable<string> GetAllPropertyTypeAliases() => throw new NotImplementedException();
        public IEnumerable<string> GetAllContentTypeAliases(params Guid[] objectTypes) => throw new NotImplementedException();
        public IEnumerable<int> GetAllContentTypeIds(string[] aliases) => throw new NotImplementedException();
        IContentTypeComposition? IContentTypeBaseService.Get(int id) => throw new NotImplementedException();
    }

    // Never exercised in these tests (no [DefaultTemplate] used).
    private sealed class FakeTemplateService : ITemplateService
    {
        public Task<IEnumerable<ITemplate>> GetAllAsync(params string[] aliases) => throw new NotImplementedException();
        public Task<IEnumerable<ITemplate>> GetAllAsync(Guid[] keys) => throw new NotImplementedException();
        public Task<IEnumerable<ITemplate>> GetChildrenAsync(int masterTemplateId) => throw new NotImplementedException();
        public Task<ITemplate?> GetAsync(string? alias) => throw new NotImplementedException();
        public Task<ITemplate?> GetAsync(int id) => throw new NotImplementedException();
        public Task<ITemplate?> GetAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<ITemplate>> GetDescendantsAsync(int masterTemplateId) => throw new NotImplementedException();
        public Task<Attempt<ITemplate, TemplateOperationStatus>> UpdateAsync(ITemplate template, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate, TemplateOperationStatus>> CreateForContentTypeAsync(string contentTypeAlias, string? contentTypeName, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(string name, string alias, string? content, Guid userKey, Guid? templateKey = null) => throw new NotImplementedException();
        public Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(ITemplate template, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate?, TemplateOperationStatus>> DeleteAsync(string alias, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate?, TemplateOperationStatus>> DeleteAsync(Guid key, Guid userKey) => throw new NotImplementedException();
        public Task<Stream> GetFileContentStreamAsync(string filepath) => throw new NotImplementedException();
        public Task SetFileContentAsync(string filepath, Stream content) => throw new NotImplementedException();
        public Task<long> GetFileSizeAsync(string filepath) => throw new NotImplementedException();
    }
}
