using System.Text.Json.Nodes;
using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst.Sync;

internal sealed class DataTypeSyncEngine
{
    private readonly IDataTypeService _dataTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _serializer;
    private readonly ILogger<DataTypeSyncEngine> _logger;

    public DataTypeSyncEngine(
        IDataTypeService dataTypeService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer serializer,
        ILogger<DataTypeSyncEngine> logger)
    {
        _dataTypeService = dataTypeService;
        _propertyEditors = propertyEditors;
        _serializer = serializer;
        _logger = logger;
    }

    public Task<Dictionary<Guid, IDataType>> EnsureDataTypesAsync(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        CancellationToken ct = default) =>
        EnsureFromPropertiesAsync(definitions.SelectMany(d => d.Properties), ct);

    public Task<Dictionary<Guid, IDataType>> EnsureMediaDataTypesAsync(
        IReadOnlyList<MediaTypeDefinition> definitions,
        CancellationToken ct = default)
    {
        var allProperties = definitions.SelectMany(d => d.Properties).ToList();
        return EnsureFromPropertiesAsync(allProperties, ct);
    }

    private async Task<Dictionary<Guid, IDataType>> EnsureFromPropertiesAsync(
        IEnumerable<PropertyDefinition> properties,
        CancellationToken ct)
    {
        var recipeByKey = new Dictionary<Guid, EditorRecipe>();
        foreach (var prop in properties)
        {
            var descriptor = prop.DataType.GetDescriptor();
            var recipe = prop.DataType.BuildRecipe(descriptor.Key, descriptor.Name);
            recipeByKey[recipe.Key] = recipe;
        }

        var dataTypeByKey = new Dictionary<Guid, IDataType>();

        foreach (var recipe in recipeByKey.Values)
        {
            var existing = await _dataTypeService.GetAsync(recipe.Key);
            if (existing is not null)
            {
                dataTypeByKey[recipe.Key] = existing;
                await UpdateIfChangedAsync(existing, recipe, ct);
                continue;
            }

            if (!_propertyEditors.TryGet(recipe.EditorAlias, out var editor))
            {
                _logger.LogWarning("Property editor '{Alias}' not registered — cannot create data type '{Name}'.", recipe.EditorAlias, recipe.Name);
                continue;
            }

            var dataType = new DataType(editor, _serializer)
            {
                Key = recipe.Key,
                Name = recipe.Name,
                EditorUiAlias = recipe.EditorUiAlias,
                DatabaseType = recipe.DatabaseType
            };

            if (recipe.ConfigData.Count > 0)
                dataType.ConfigurationData = recipe.ConfigData;

            var result = await _dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
            if (result.Success)
            {
                dataTypeByKey[recipe.Key] = result.Result;
                _logger.LogInformation("Created code-first data type '{Name}' ({Key}).", recipe.Name, recipe.Key);
            }
            else
            {
                _logger.LogError("Failed to create data type '{Name}' ({Key}): {Status}.", recipe.Name, recipe.Key, result.Status);
            }
        }

        return dataTypeByKey;
    }

    // A data type's GUID key is fixed on its [DataType] attribute and does not encode its config,
    // so the same key can legitimately carry different config across scans (e.g. a BlockGrid data
    // type gaining a new block, or a Dropdown's Options list changing). Compare semantically via
    // JSON — dictionary key order/number formatting differ across a DB round-trip even when the
    // config is unchanged, so a raw string/reference comparison would false-positive on every run.
    private async Task UpdateIfChangedAsync(IDataType existing, EditorRecipe recipe, CancellationToken ct)
    {
        var existingConfigJson = JsonNode.Parse(_serializer.Serialize(existing.ConfigurationData));
        var recipeConfigJson = JsonNode.Parse(_serializer.Serialize(recipe.ConfigData));

        if (JsonNode.DeepEquals(existingConfigJson, recipeConfigJson))
        {
            _logger.LogDebug("Code-first data type '{Name}' already exists and is unchanged.", recipe.Name);
            return;
        }

        existing.ConfigurationData = recipe.ConfigData;

        var result = await _dataTypeService.UpdateAsync(existing, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            _logger.LogError("Failed to update data type '{Name}' ({Key}): {Status}.", recipe.Name, recipe.Key, result.Status);
            return;
        }

        _logger.LogInformation("Updated code-first data type '{Name}' ({Key}) — configuration changed.", recipe.Name, recipe.Key);
    }
}
