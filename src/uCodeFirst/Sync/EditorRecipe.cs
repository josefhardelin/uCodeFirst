using Umbraco.Cms.Core.Models;

namespace uCodeFirst.Sync;

public sealed record EditorRecipe(
    Guid Key,
    string Name,
    string EditorAlias,
    string EditorUiAlias,
    IDictionary<string, object> ConfigData,
    ValueStorageType DatabaseType = ValueStorageType.Nvarchar);
