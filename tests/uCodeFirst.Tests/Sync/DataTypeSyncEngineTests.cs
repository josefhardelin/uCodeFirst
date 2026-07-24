using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using DataType = Umbraco.Cms.Core.Models.DataType;

namespace uCodeFirst.Tests.Sync;

[TestFixture]
public class DataTypeSyncEngineTests
{
    [uCodeFirst.Attributes.DataType("Colours", Guid = "3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    private sealed class ColoursDropdown : DropdownDataType
    {
        public override string[] Options { get; } = ["Red", "Blue"];
    }

    [DocumentType("Test Doc", Guid = "8f14e45f-ceea-467e-add2-1c9c56f1a17f")]
    private sealed class TestDoc
    {
        [ColoursDropdown]
        public string? Colour { get; set; }
    }

    private static IReadOnlyList<DocumentTypeDefinition> Scan() =>
        new DocumentTypeScanner().Scan(new[] { typeof(TestDoc).Assembly });

    private static Guid ColoursKey => Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    [Test]
    public async Task EnsureDataTypesAsync_UpdatesExistingDataType_WhenConfigDiffers()
    {
        var editor = new FakeDataEditor("Umbraco.DropDown.Flexible");
        var serializer = new FakeJsonSerializer();

        // Stale config from a previous run — code now declares ["Red", "Blue"] via ColoursDropdown.
        var existing = new DataType(editor, serializer) { Key = ColoursKey, Name = "Colours" };
        existing.SetConfigurationData(new Dictionary<string, object> { ["multiple"] = false, ["items"] = new List<object> { "Red" } });

        var propertyEditors = new PropertyEditorCollection(new DataEditorCollection(() => [editor]));
        var service = new FakeDataTypeService(existing);
        var engine = new DataTypeSyncEngine(service, propertyEditors, serializer, NullLogger<DataTypeSyncEngine>.Instance);

        var result = await engine.EnsureDataTypesAsync(Scan());

        Assert.That(service.UpdateCallCount, Is.EqualTo(1));
        Assert.That(service.CreateCallCount, Is.EqualTo(0));
        Assert.That(result[ColoursKey], Is.SameAs(existing));
        Assert.That(existing.ConfigurationData["items"], Is.EqualTo(new List<object> { "Red", "Blue" }));
    }

    [Test]
    public async Task EnsureDataTypesAsync_DoesNotUpdate_WhenConfigUnchanged()
    {
        var editor = new FakeDataEditor("Umbraco.DropDown.Flexible");
        var serializer = new FakeJsonSerializer();

        // Deliberately different key order than BuildRecipe produces — JSON-equal but not
        // string-equal, mirroring what a DB round-trip does to dictionary ordering.
        var existing = new DataType(editor, serializer) { Key = ColoursKey, Name = "Colours" };
        existing.SetConfigurationData(new Dictionary<string, object>
        {
            ["items"] = new List<object> { "Red", "Blue" },
            ["multiple"] = false,
        });

        var propertyEditors = new PropertyEditorCollection(new DataEditorCollection(() => [editor]));
        var service = new FakeDataTypeService(existing);
        var engine = new DataTypeSyncEngine(service, propertyEditors, serializer, NullLogger<DataTypeSyncEngine>.Instance);

        await engine.EnsureDataTypesAsync(Scan());

        Assert.That(service.UpdateCallCount, Is.EqualTo(0));
        Assert.That(service.CreateCallCount, Is.EqualTo(0));
    }

    private sealed class FakeDataEditor(string alias) : IDataEditor
    {
        public string Alias { get; } = alias;
        public bool IsDeprecated => false;
        public IDictionary<string, object>? DefaultConfiguration => null;
        public IPropertyIndexValueFactory PropertyIndexValueFactory => throw new NotImplementedException();
        public IDataValueEditor GetValueEditor() => throw new NotImplementedException();
        public IDataValueEditor GetValueEditor(object? configurationObject) => throw new NotImplementedException();
        public IConfigurationEditor GetConfigurationEditor() => new FakeConfigurationEditor();
    }

    private sealed class FakeConfigurationEditor : IConfigurationEditor
    {
        public List<ConfigurationField> Fields { get; } = [];
        public IDictionary<string, object> DefaultConfiguration { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> ToConfigurationEditor(IDictionary<string, object> configuration) => configuration;
        public IDictionary<string, object> FromConfigurationEditor(IDictionary<string, object> configuration) => configuration;
        public IDictionary<string, object> ToValueEditor(IDictionary<string, object> configuration) => configuration;
        public object ToConfigurationObject(IDictionary<string, object> configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => configuration;
        public IDictionary<string, object> FromConfigurationObject(object configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => (IDictionary<string, object>)configuration;
        public string ToDatabase(IDictionary<string, object> configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => configurationEditorJsonSerializer.Serialize(configuration);
        public IDictionary<string, object> FromDatabase(string? configuration, IConfigurationEditorJsonSerializer configurationEditorJsonSerializer) => new Dictionary<string, object>();
        public IEnumerable<ValidationResult> Validate(IDictionary<string, object> configuration) => [];
    }

    private sealed class FakeJsonSerializer : IConfigurationEditorJsonSerializer
    {
        public string Serialize(object? input) => System.Text.Json.JsonSerializer.Serialize(input);
        public T? Deserialize<T>(string input) => System.Text.Json.JsonSerializer.Deserialize<T>(input);
        public bool TryDeserialize<T>(object input, out T? value) where T : class => throw new NotImplementedException();
    }

    // Minimal fake covering only what DataTypeSyncEngine calls (GetAsync/CreateAsync/UpdateAsync).
    // Other IDataTypeService members are unused by the engine and throw if ever exercised.
    private sealed class FakeDataTypeService(params IDataType[] existing) : IDataTypeService
    {
        private readonly Dictionary<Guid, IDataType> _byKey = existing.ToDictionary(d => d.Key);

        public int CreateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<IDataType?> GetAsync(Guid id) => Task.FromResult(_byKey.GetValueOrDefault(id));

        public Task<Attempt<IDataType, DataTypeOperationStatus>> CreateAsync(IDataType dataType, Guid userKey)
        {
            CreateCallCount++;
            _byKey[dataType.Key] = dataType;
            return Task.FromResult(Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, dataType));
        }

        public Task<Attempt<IDataType, DataTypeOperationStatus>> UpdateAsync(IDataType dataType, Guid userKey)
        {
            UpdateCallCount++;
            _byKey[dataType.Key] = dataType;
            return Task.FromResult(Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, dataType));
        }

        public Task<IDataType?> GetAsync(string name) => throw new NotImplementedException();
        public Task<IEnumerable<IDataType>> GetAllAsync(params Guid[] keys) => throw new NotImplementedException();
        public Task<PagedModel<IDataType>> FilterAsync(string? name = null, string? editorUiAlias = null, string? editorAlias = null, int skip = 0, int take = 100) => throw new NotImplementedException();
        public IEnumerable<IDataType> GetAll(params int[] ids) => throw new NotImplementedException();
        public void Save(IDataType dataType, int userId = -1) => throw new NotImplementedException();
        public void Save(IEnumerable<IDataType> dataTypeDefinitions, int userId = -1) => throw new NotImplementedException();
        public void Delete(IDataType dataType, int userId = -1) => throw new NotImplementedException();
        public Task<Attempt<IDataType?, DataTypeOperationStatus>> DeleteAsync(Guid id, Guid userKey) => throw new NotImplementedException();
        public IEnumerable<IDataType> GetByEditorAlias(string propertyEditorAlias) => throw new NotImplementedException();
        public Task<IEnumerable<IDataType>> GetByEditorUiAlias(string editorUiAlias) => throw new NotImplementedException();
        public Attempt<OperationResult<MoveOperationStatusType>?> Move(IDataType toMove, int parentId) => throw new NotImplementedException();
        public Task<Attempt<IDataType, DataTypeOperationStatus>> MoveAsync(IDataType toMove, Guid? containerKey, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<IDataType, DataTypeOperationStatus>> CopyAsync(IDataType toCopy, Guid? containerKey, Guid userKey) => throw new NotImplementedException();
        public IEnumerable<ValidationResult> ValidateConfigurationData(IDataType dataType) => throw new NotImplementedException();

        [Obsolete] public Attempt<OperationResult<OperationResultType, EntityContainer>?> CreateContainer(int parentId, Guid key, string name, int userId = -1) => throw new NotImplementedException();
        [Obsolete] public Attempt<OperationResult?> SaveContainer(EntityContainer container, int userId = -1) => throw new NotImplementedException();
        [Obsolete] public EntityContainer? GetContainer(int containerId) => throw new NotImplementedException();
        [Obsolete] public EntityContainer? GetContainer(Guid containerId) => throw new NotImplementedException();
        [Obsolete] public IEnumerable<EntityContainer> GetContainers(string folderName, int level) => throw new NotImplementedException();
        [Obsolete] public IEnumerable<EntityContainer> GetContainers(IDataType dataType) => throw new NotImplementedException();
        [Obsolete] public IEnumerable<EntityContainer> GetContainers(int[] containerIds) => throw new NotImplementedException();
        [Obsolete] public Attempt<OperationResult?> DeleteContainer(int containerId, int userId = -1) => throw new NotImplementedException();
        [Obsolete] public Attempt<OperationResult<OperationResultType, EntityContainer>?> RenameContainer(int id, string name, int userId = -1) => throw new NotImplementedException();
        [Obsolete] public IDataType? GetDataType(string name) => throw new NotImplementedException();
        [Obsolete] public IDataType? GetDataType(int id) => throw new NotImplementedException();
    }
}
