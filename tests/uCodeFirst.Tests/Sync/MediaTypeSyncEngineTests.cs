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

public class MediaTypeSyncEngineTests
{
    private static readonly Guid TypeKey = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly IShortStringHelper Helper = new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));

    private static IDataType MakeDataType() => new DataType(new FakeDataEditor(), new FakeJsonSerializer());

    private static Dictionary<Guid, IDataType> DataTypeByKey() =>
        new() { [new TextString().GetDescriptor().Key] = MakeDataType() };

    private static PropertyDefinition Property(string alias, string group = "Content") =>
        new(Alias: alias, Name: alias, GroupName: group, SortOrder: 0, Mandatory: false, Description: null, DataType: new TextString(), VariesByCulture: false);

    private static MediaTypeDefinition Definition(IReadOnlyList<PropertyDefinition> properties) =>
        new(
            ClrType: typeof(MediaTypeSyncEngineTests),
            Key: TypeKey,
            Alias: "image",
            Name: "Image",
            Icon: null,
            Color: null,
            Description: null,
            AllowedAtRoot: true,
            Folder: null,
            AllowedChildTypes: Array.Empty<Type>(),
            Properties: properties,
            CompositionKeys: Array.Empty<Guid>(),
            ParentKey: null,
            IsContainer: false);

    // Existing media type: group "content" has [caption, legacyField]; group "extra" has only
    // [obsoleteProp]. Current C# definitions in these tests only declare "caption".
    private static MediaType BuildExistingMediaType()
    {
        var mediaType = new MediaType(Helper, parentId: -1) { Key = TypeKey, Alias = "image", Name = "Image" };
        var dataType = MakeDataType();

        var contentGroup = new PropertyGroup(isPublishing: true) { Alias = "content", Name = "Content", Type = PropertyGroupType.Tab, SortOrder = 0 };
        mediaType.PropertyGroups.Add(contentGroup);
        mediaType.AddPropertyType(new PropertyType(Helper, dataType, "caption") { Name = "Caption" }, "content", "Content");
        mediaType.AddPropertyType(new PropertyType(Helper, dataType, "legacyField") { Name = "Legacy" }, "content", "Content");

        var extraGroup = new PropertyGroup(isPublishing: true) { Alias = "extra", Name = "Extra", Type = PropertyGroupType.Tab, SortOrder = 1 };
        mediaType.PropertyGroups.Add(extraGroup);
        mediaType.AddPropertyType(new PropertyType(Helper, dataType, "obsoleteProp") { Name = "Obsolete" }, "extra", "Extra");

        return mediaType;
    }

    [Fact]
    public async Task PlanAsync_NewMediaType_IsToCreate()
    {
        var service = new FakeMediaTypeService();
        var engine = new MediaTypeSyncEngine(service, Helper, NullLogger<MediaTypeSyncEngine>.Instance);

        var plan = await engine.PlanAsync(new[] { Definition(new[] { Property("caption") }) }, CodeFirstStrategy.NonDestructive);

        Assert.Single(plan.ToCreate);
        Assert.Equal("image", plan.ToCreate[0].Alias);
        Assert.Empty(plan.ToUpdate);
    }

    [Fact]
    public async Task PlanAsync_ExistingMediaType_Destructive_ComputesStalePropertiesAndEmptyGroup()
    {
        var service = new FakeMediaTypeService(BuildExistingMediaType());
        var engine = new MediaTypeSyncEngine(service, Helper, NullLogger<MediaTypeSyncEngine>.Instance);

        var plan = await engine.PlanAsync(new[] { Definition(new[] { Property("caption") }) }, CodeFirstStrategy.Destructive);

        Assert.Equal(2, plan.PrunedProperties.Count);
        Assert.Contains(plan.PrunedProperties, p => p.PropertyAlias == "legacyField");
        Assert.Contains(plan.PrunedProperties, p => p.PropertyAlias == "obsoleteProp");
        Assert.Single(plan.PrunedGroups);
        Assert.Equal("extra", plan.PrunedGroups[0].GroupAlias);
    }

    [Fact]
    public async Task SyncAsync_CreatesNewMediaType()
    {
        var service = new FakeMediaTypeService();
        var engine = new MediaTypeSyncEngine(service, Helper, NullLogger<MediaTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("caption") }) }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        Assert.Equal(1, service.CreateCallCount);
        var created = await service.GetAsync(TypeKey);
        Assert.NotNull(created);
        Assert.Contains(created!.PropertyTypes, pt => pt.Alias == "caption");
    }

    [Fact]
    public async Task SyncAsync_NonDestructive_NeverRemovesStaleProperties()
    {
        var service = new FakeMediaTypeService(BuildExistingMediaType());
        var engine = new MediaTypeSyncEngine(service, Helper, NullLogger<MediaTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("caption") }) }, DataTypeByKey(), CodeFirstStrategy.NonDestructive);

        var updated = await service.GetAsync(TypeKey);
        Assert.Contains(updated!.PropertyTypes, pt => pt.Alias == "legacyField");
        Assert.Contains(updated.PropertyTypes, pt => pt.Alias == "obsoleteProp");
        Assert.Contains(updated.PropertyGroups, g => g.Alias == "extra");
    }

    [Fact]
    public async Task SyncAsync_Destructive_RemovesStalePropertiesAndEmptyGroups()
    {
        var service = new FakeMediaTypeService(BuildExistingMediaType());
        var engine = new MediaTypeSyncEngine(service, Helper, NullLogger<MediaTypeSyncEngine>.Instance);

        await engine.SyncAsync(new[] { Definition(new[] { Property("caption") }) }, DataTypeByKey(), CodeFirstStrategy.Destructive);

        var updated = await service.GetAsync(TypeKey);
        Assert.Contains(updated!.PropertyTypes, pt => pt.Alias == "caption");
        Assert.DoesNotContain(updated.PropertyTypes, pt => pt.Alias == "legacyField");
        Assert.DoesNotContain(updated.PropertyTypes, pt => pt.Alias == "obsoleteProp");
        Assert.DoesNotContain(updated.PropertyGroups, g => g.Alias == "extra");
        Assert.Contains(updated.PropertyGroups, g => g.Alias == "content");
    }

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

    // Minimal fake covering only what MediaTypeSyncEngine calls in these tests (no folders, allowed
    // children, parent-key inheritance, or compositions are exercised). Other members throw if ever hit.
    private sealed class FakeMediaTypeService : IMediaTypeService
    {
        private readonly Dictionary<Guid, IMediaType> _byKey = new();

        public FakeMediaTypeService(params IMediaType[] existing)
        {
            foreach (var mt in existing)
                _byKey[mt.Key] = mt;
        }

        public int CreateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<IMediaType?> GetAsync(Guid guid) => Task.FromResult(_byKey.GetValueOrDefault(guid));

        public Task<Attempt<ContentTypeOperationStatus>> CreateAsync(IMediaType item, Guid performingUserKey)
        {
            CreateCallCount++;
            _byKey[item.Key] = item;
            return Task.FromResult(Attempt.Succeed(ContentTypeOperationStatus.Success));
        }

        public Task<Attempt<ContentTypeOperationStatus>> UpdateAsync(IMediaType item, Guid performingUserKey)
        {
            UpdateCallCount++;
            _byKey[item.Key] = item;
            return Task.FromResult(Attempt.Succeed(ContentTypeOperationStatus.Success));
        }

        public IMediaType? Get(int id) => throw new NotImplementedException();
        public IMediaType? Get(Guid key) => throw new NotImplementedException();
        public IMediaType? Get(string alias) => throw new NotImplementedException();
        public int Count() => throw new NotImplementedException();
        public bool HasContentNodes(int id) => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetAll() => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetMany(params int[] ids) => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetMany(IEnumerable<Guid>? ids) => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetDescendants(int id, bool andSelf) => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetComposedOf(int id) => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetChildren(int id) => throw new NotImplementedException();
        public IEnumerable<IMediaType> GetChildren(Guid id) => throw new NotImplementedException();
        public bool HasChildren(int id) => throw new NotImplementedException();
        public bool HasChildren(Guid id) => throw new NotImplementedException();
        public void Save(IMediaType? item, int userId = -1) => throw new NotImplementedException();
        public void Save(IEnumerable<IMediaType> items, int userId = -1) => throw new NotImplementedException();
        public void Delete(IMediaType item, int userId = -1) => throw new NotImplementedException();
        public Task<ContentTypeOperationStatus> DeleteAsync(Guid key, Guid performingUserKey) => throw new NotImplementedException();
        public void Delete(IEnumerable<IMediaType> item, int userId = -1) => throw new NotImplementedException();
        public Attempt<string[]?> ValidateComposition(IMediaType? compo) => throw new NotImplementedException();
        public bool HasContainerInPath(string contentPath) => throw new NotImplementedException();
        public bool HasContainerInPath(params int[] ids) => throw new NotImplementedException();
        public Attempt<OperationResult<OperationResultType, EntityContainer>?> CreateContainer(int parentContainerId, Guid key, string name, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult?> SaveContainer(EntityContainer container, int userId = -1) => throw new NotImplementedException();
        public EntityContainer? GetContainer(int containerId) => throw new NotImplementedException();
        public EntityContainer? GetContainer(Guid containerId) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(int[] containerIds) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(IMediaType contentType) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(string folderName, int level) => throw new NotImplementedException();
        public Attempt<OperationResult?> DeleteContainer(int containerId, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult<OperationResultType, EntityContainer>?> RenameContainer(int id, string name, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult<MoveOperationStatusType>?> Move(IMediaType moving, int containerId) => throw new NotImplementedException();
        public Attempt<OperationResult<MoveOperationStatusType, IMediaType>?> Copy(IMediaType copying, int containerId) => throw new NotImplementedException();
        public IMediaType Copy(IMediaType original, string alias, string name, int parentId = -1) => throw new NotImplementedException();
        public IMediaType Copy(IMediaType original, string alias, string name, IMediaType parent) => throw new NotImplementedException();
        public Task<Attempt<IMediaType?, ContentTypeStructureOperationStatus>> CopyAsync(Guid key, Guid? containerKey) => throw new NotImplementedException();
        public Task<Attempt<IMediaType?, ContentTypeStructureOperationStatus>> MoveAsync(Guid key, Guid? containerKey) => throw new NotImplementedException();
        public Task<PagedModel<IMediaType>> GetAllAllowedAsRootAsync(int skip, int take) => throw new NotImplementedException();
        public Task<Attempt<PagedModel<IMediaType>?, ContentTypeOperationStatus>> GetAllowedChildrenAsync(Guid key, int skip, int take) => throw new NotImplementedException();
        IContentTypeComposition? IContentTypeBaseService.Get(int id) => throw new NotImplementedException();
    }
}
