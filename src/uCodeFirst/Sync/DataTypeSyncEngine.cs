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

    public async Task<Dictionary<Guid, IDataType>> EnsureDataTypesAsync(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        CancellationToken ct = default)
    {
        var recipeByKey = new Dictionary<Guid, EditorRecipe>();
        foreach (var def in definitions)
        {
            foreach (var prop in def.Properties)
            {
                var descriptor = prop.DataType.GetDescriptor();
                var recipe = prop.DataType.BuildRecipe(descriptor.Key, descriptor.Name);
                recipeByKey[recipe.Key] = recipe;
            }
        }

        var dataTypeByKey = new Dictionary<Guid, IDataType>();

        foreach (var recipe in recipeByKey.Values)
        {
            var existing = await _dataTypeService.GetAsync(recipe.Key);
            if (existing is not null)
            {
                dataTypeByKey[recipe.Key] = existing;
                _logger.LogDebug("Code-first data type '{Name}' already exists.", recipe.Name);
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
}
