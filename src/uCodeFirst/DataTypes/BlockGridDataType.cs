using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class BlockGridDataType : DataTypeBase
{
    public virtual Type[] BlockTypes { get; } = [];
    public virtual int GridColumns { get; } = 12;

    internal override EditorRecipe BuildRecipe(Guid key, string name)
    {
        var elementKeys = ResolveElementTypeKeys(BlockTypes);
        var blocks = elementKeys
            .Select(k => (object)new Dictionary<string, object>
            {
                ["contentElementTypeKey"] = k,
                ["allowAtRoot"] = true,
                ["allowInAreas"] = false,
                ["areas"] = new List<object>()
            })
            .ToList();

        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["blocks"] = blocks,
            ["gridColumns"] = GridColumns
        };

        return new EditorRecipe(key, name, "Umbraco.BlockGrid", "Umb.PropertyEditorUi.BlockGrid", config, ValueStorageType.Ntext);
    }

    private static List<Guid> ResolveElementTypeKeys(Type[] blockTypes) =>
        blockTypes
            .Select(t => t.GetCustomAttribute<ElementTypeAttribute>())
            .Where(a => a is not null)
            .Select(a => a!.Key)
            .OrderBy(k => k)
            .ToList();
}
