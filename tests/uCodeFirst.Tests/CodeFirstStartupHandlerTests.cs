using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using uCodeFirst.Attributes;
using uCodeFirst.Configuration;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using uCodeFirst.Validation;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

namespace uCodeFirst.Tests;

// Exercises the Enabled x validation-outcome matrix through the real CodeFirstStartupHandler ->
// CodeFirstSyncService pipeline. HandleAsync always scans the *whole* AppDomain (it calls
// AppDomain.CurrentDomain.GetAssemblies() internally, with no way to scope it to a subset), so any
// [DocumentType] fixture declared anywhere in this test assembly is visible to every test here. That
// rules out also covering the "valid scan applies/plans" routing through this class in the same
// assembly as a permanent duplicate-alias fixture — a class with an always-colliding alias would fail
// validation for every test, including ones that need a clean scan. That "valid" routing (Enabled
// applies via CreateAsync, Disabled never calls it) is already covered directly at the engine level by
// ContentTypeSyncEngineTests. This file only needs a permanent validation failure, so it sticks to that.
public class CodeFirstStartupHandlerTests
{
    private static readonly IShortStringHelper Helper = new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));

    [DocumentType("Article A", "article", Guid = "44444444-4444-4444-4444-444444444444")]
    private sealed class DuplicateAliasDocA { }

    [DocumentType("Article B", "article", Guid = "55555555-5555-5555-5555-555555555555")]
    private sealed class DuplicateAliasDocB { }

    private static CodeFirstStartupHandler BuildHandler(FakeContentTypeService contentTypeService, IRuntimeState runtimeState, CodeFirstOptions options)
    {
        var propertyEditors = new PropertyEditorCollection(new DataEditorCollection(() => Enumerable.Empty<IDataEditor>()));

        var scanner = new DocumentTypeScanner();
        var validator = new PreFlightValidator();
        var dataTypeSyncEngine = new DataTypeSyncEngine(new FakeDataTypeService(), propertyEditors, new FakeJsonSerializer(), NullLogger<DataTypeSyncEngine>.Instance);
        var contentTypeSyncEngine = new ContentTypeSyncEngine(contentTypeService, new FakeTemplateService(), Helper, NullLogger<ContentTypeSyncEngine>.Instance);
        var mediaTypeSyncEngine = new MediaTypeSyncEngine(new FakeMediaTypeService(), Helper, NullLogger<MediaTypeSyncEngine>.Instance);
        var dictionaryItemSyncEngine = new DictionaryItemSyncEngine(new FakeDictionaryItemService(), NullLogger<DictionaryItemSyncEngine>.Instance);
        var languageSyncEngine = new LanguageSyncEngine(new FakeLanguageService(), NullLogger<LanguageSyncEngine>.Instance);
        var templateSyncEngine = new TemplateSyncEngine(new FakeTemplateService(), NullLogger<TemplateSyncEngine>.Instance);

        var syncService = new CodeFirstSyncService(
            scanner, validator, dataTypeSyncEngine, contentTypeSyncEngine, mediaTypeSyncEngine,
            dictionaryItemSyncEngine, languageSyncEngine, templateSyncEngine, NullLogger<CodeFirstSyncService>.Instance);

        return new CodeFirstStartupHandler(syncService, runtimeState, Options.Create(options), NullLogger<CodeFirstStartupHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_RuntimeLevelNotRun_SkipsEntirely()
    {
        var contentTypeService = new FakeContentTypeService();
        var handler = BuildHandler(contentTypeService, new FakeRuntimeState(RuntimeLevel.Install), new CodeFirstOptions { Enabled = true });

        await handler.HandleAsync(new UmbracoApplicationStartedNotification(isRestarting: false), CancellationToken.None);

        Assert.Equal(0, contentTypeService.CreateCallCount);
    }

    [Fact]
    public async Task HandleAsync_Enabled_ValidationFails_Throws()
    {
        var contentTypeService = new FakeContentTypeService();
        var handler = BuildHandler(contentTypeService, new FakeRuntimeState(RuntimeLevel.Run), new CodeFirstOptions { Enabled = true });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new UmbracoApplicationStartedNotification(isRestarting: false), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Disabled_ValidationFails_DoesNotThrow()
    {
        var contentTypeService = new FakeContentTypeService();
        var handler = BuildHandler(contentTypeService, new FakeRuntimeState(RuntimeLevel.Run), new CodeFirstOptions { Enabled = false });

        var exception = await Record.ExceptionAsync(() =>
            handler.HandleAsync(new UmbracoApplicationStartedNotification(isRestarting: false), CancellationToken.None));

        Assert.Null(exception);
        Assert.Equal(0, contentTypeService.CreateCallCount);
    }

    private sealed class FakeRuntimeState : IRuntimeState
    {
        public FakeRuntimeState(RuntimeLevel level) => Level = level;
        public RuntimeLevel Level { get; }
        public Version Version => throw new NotImplementedException();
        public string VersionComment => throw new NotImplementedException();
        public Umbraco.Cms.Core.Semver.SemVersion SemanticVersion => throw new NotImplementedException();
        public RuntimeLevelReason Reason => RuntimeLevelReason.Run;
        public string? CurrentMigrationState => null;
        public string? FinalMigrationState => null;
        public Umbraco.Cms.Core.Exceptions.BootFailedException? BootFailedException => null;
        public IReadOnlyDictionary<string, object> StartupState => new Dictionary<string, object>();
        public void DetermineRuntimeLevel() => throw new NotImplementedException();
        public void Configure(RuntimeLevel level, RuntimeLevelReason reason, Exception? bootFailedException = null) => throw new NotImplementedException();
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

    // Never creates a data type in these tests (PropertyEditorCollection is empty, so TryGet always
    // fails and property creation is skipped) — only GetAsync needs to work.
    private sealed class FakeDataTypeService : IDataTypeService
    {
        public Task<IDataType?> GetAsync(Guid id) => Task.FromResult<IDataType?>(null);
        public Task<IDataType?> GetAsync(string name) => throw new NotImplementedException();
        public Task<IEnumerable<IDataType>> GetAllAsync(params Guid[] keys) => throw new NotImplementedException();
        public Task<PagedModel<IDataType>> FilterAsync(string? name = null, string? editorUiAlias = null, string? editorAlias = null, int skip = 0, int take = 100) => throw new NotImplementedException();
        public IEnumerable<IDataType> GetAll(params int[] ids) => throw new NotImplementedException();
        public void Save(IDataType dataType, int userId = -1) => throw new NotImplementedException();
        public void Save(IEnumerable<IDataType> dataTypeDefinitions, int userId = -1) => throw new NotImplementedException();
        public Task<Attempt<IDataType, DataTypeOperationStatus>> CreateAsync(IDataType dataType, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<IDataType, DataTypeOperationStatus>> UpdateAsync(IDataType dataType, Guid userKey) => throw new NotImplementedException();
        public void Delete(IDataType dataType, int userId = -1) => throw new NotImplementedException();
        public Task<Attempt<IDataType?, DataTypeOperationStatus>> DeleteAsync(Guid id, Guid userKey) => throw new NotImplementedException();
        public IEnumerable<IDataType> GetByEditorAlias(string propertyEditorAlias) => throw new NotImplementedException();
        public Task<IEnumerable<IDataType>> GetByEditorUiAlias(string editorUiAlias) => throw new NotImplementedException();
        public Attempt<OperationResult<MoveOperationStatusType>?> Move(IDataType toMove, int parentId) => throw new NotImplementedException();
        public Task<Attempt<IDataType, DataTypeOperationStatus>> MoveAsync(IDataType toMove, Guid? containerKey, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<IDataType, DataTypeOperationStatus>> CopyAsync(IDataType toCopy, Guid? containerKey, Guid userKey) => throw new NotImplementedException();
        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> ValidateConfigurationData(IDataType dataType) => throw new NotImplementedException();
        public IDataType? GetDataType(string name) => throw new NotImplementedException();
        public IDataType? GetDataType(int id) => throw new NotImplementedException();
        public Attempt<OperationResult<OperationResultType, EntityContainer>?> CreateContainer(int parentId, Guid key, string name, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult?> SaveContainer(EntityContainer container, int userId = -1) => throw new NotImplementedException();
        public EntityContainer? GetContainer(int containerId) => throw new NotImplementedException();
        public EntityContainer? GetContainer(Guid containerId) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(string folderName, int level) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(IDataType dataType) => throw new NotImplementedException();
        public IEnumerable<EntityContainer> GetContainers(int[] containerIds) => throw new NotImplementedException();
        public Attempt<OperationResult?> DeleteContainer(int containerId, int userId = -1) => throw new NotImplementedException();
        public Attempt<OperationResult<OperationResultType, EntityContainer>?> RenameContainer(int id, string name, int userId = -1) => throw new NotImplementedException();
    }

    private sealed class FakeContentTypeService : IContentTypeService
    {
        private readonly Dictionary<Guid, IContentType> _byKey = new();
        public int CreateCallCount { get; private set; }

        public Task<IContentType?> GetAsync(Guid guid) => Task.FromResult(_byKey.GetValueOrDefault(guid));

        public Task<Attempt<ContentTypeOperationStatus>> CreateAsync(IContentType item, Guid performingUserKey)
        {
            CreateCallCount++;
            _byKey[item.Key] = item;
            return Task.FromResult(Attempt.Succeed(ContentTypeOperationStatus.Success));
        }

        public Task<Attempt<ContentTypeOperationStatus>> UpdateAsync(IContentType item, Guid performingUserKey)
        {
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

    private sealed class FakeMediaTypeService : IMediaTypeService
    {
        public Task<IMediaType?> GetAsync(Guid guid) => throw new NotImplementedException();
        public Task<Attempt<ContentTypeOperationStatus>> CreateAsync(IMediaType item, Guid performingUserKey) => throw new NotImplementedException();
        public Task<Attempt<ContentTypeOperationStatus>> UpdateAsync(IMediaType item, Guid performingUserKey) => throw new NotImplementedException();
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

    private sealed class FakeDictionaryItemService : IDictionaryItemService
    {
        public Task<IDictionaryItem?> GetAsync(Guid id) => throw new NotImplementedException();
        public Task<IDictionaryItem?> GetAsync(string key) => throw new NotImplementedException();
        public Task<IEnumerable<IDictionaryItem>> GetManyAsync(params Guid[] ids) => throw new NotImplementedException();
        public Task<IEnumerable<IDictionaryItem>> GetManyAsync(params string[] keys) => throw new NotImplementedException();
        public Task<IEnumerable<IDictionaryItem>> GetChildrenAsync(Guid parentId) => throw new NotImplementedException();
        public Task<IEnumerable<IDictionaryItem>> GetDescendantsAsync(Guid? parentId, string? filter = null) => throw new NotImplementedException();
        public Task<IEnumerable<IDictionaryItem>> GetAtRootAsync() => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string key) => throw new NotImplementedException();
        public Task<Attempt<IDictionaryItem, DictionaryItemOperationStatus>> CreateAsync(IDictionaryItem dictionaryItem, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<IDictionaryItem, DictionaryItemOperationStatus>> UpdateAsync(IDictionaryItem dictionaryItem, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<IDictionaryItem?, DictionaryItemOperationStatus>> DeleteAsync(Guid id, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<IDictionaryItem, DictionaryItemOperationStatus>> MoveAsync(IDictionaryItem dictionaryItem, Guid? parentId, Guid userKey) => throw new NotImplementedException();
        public Task<int> CountChildrenAsync(Guid parentId) => throw new NotImplementedException();
        public Task<int> CountRootAsync() => throw new NotImplementedException();
        public Task<PagedModel<IDictionaryItem>> GetPagedAsync(Guid? parentId, int skip, int take) => throw new NotImplementedException();
    }

    private sealed class FakeLanguageService : ILanguageService
    {
        public Task<ILanguage?> GetAsync(string isoCode) => throw new NotImplementedException();
        public Task<Attempt<ILanguage, LanguageOperationStatus>> CreateAsync(ILanguage language, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ILanguage, LanguageOperationStatus>> UpdateAsync(ILanguage language, Guid userKey) => throw new NotImplementedException();
        public Task<ILanguage?> GetDefaultLanguageAsync() => throw new NotImplementedException();
        public Task<string> GetDefaultIsoCodeAsync() => throw new NotImplementedException();
        public Task<IEnumerable<ILanguage>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<ILanguage>> GetMultipleAsync(IEnumerable<string> isoCodes) => throw new NotImplementedException();
        public Task<Attempt<ILanguage?, LanguageOperationStatus>> DeleteAsync(string isoCode, Guid userKey) => throw new NotImplementedException();
        public Task<string[]> GetIsoCodesByIdsAsync(ICollection<int> ids) => throw new NotImplementedException();
    }
}
