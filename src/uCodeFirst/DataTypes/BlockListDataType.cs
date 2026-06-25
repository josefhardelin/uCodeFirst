using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class BlockListDataType : DataTypeBase
{
    public virtual Type[] BlockTypes { get; } = [];

    internal override EditorRecipe BuildRecipe(Guid key, string name)
    {
        var elementKeys = ResolveElementTypeKeys(BlockTypes);
        var blocks = elementKeys
            .Select(k => (object)new Dictionary<string, object> { ["contentElementTypeKey"] = k })
            .ToList();

        IDictionary<string, object> config = new Dictionary<string, object> { ["blocks"] = blocks };
        return new EditorRecipe(key, name, "Umbraco.BlockList", "Umb.PropertyEditorUi.BlockList", config, ValueStorageType.Ntext);
    }

    private static List<Guid> ResolveElementTypeKeys(Type[] blockTypes) =>
        blockTypes
            .Select(t => t.GetCustomAttribute<ElementTypeAttribute>())
            .Where(a => a is not null)
            .Select(a => a!.Key)
            .OrderBy(k => k)
            .ToList();
}
