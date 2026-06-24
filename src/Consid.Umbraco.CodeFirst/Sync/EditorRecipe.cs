using Umbraco.Cms.Core.Models;

namespace Consid.Umbraco.CodeFirst.Sync;

internal sealed record EditorRecipe(
    Guid Key,
    string Name,
    string EditorAlias,
    string EditorUiAlias,
    IDictionary<string, object> ConfigData,
    ValueStorageType DatabaseType = ValueStorageType.Nvarchar);
