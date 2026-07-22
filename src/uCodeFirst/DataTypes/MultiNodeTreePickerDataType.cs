using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Multinode Treepicker" (<c>Umbraco.MultiNodeTreePicker</c>) editor.</summary>
public abstract class MultiNodeTreePickerDataType : DataTypeBase
{
    /// <summary>Root object type to pick from: "content", "media" or "member".</summary>
    public virtual string StartNodeType { get; } = "content";
    /// <summary>Minimum number of items required. Zero means no minimum.</summary>
    public virtual int MinItems { get; } = 0;
    /// <summary>Maximum number of items allowed. Zero means unlimited.</summary>
    public virtual int MaxItems { get; } = 0;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["startNode"] = new Dictionary<string, object?>
            {
                ["type"] = StartNodeType,
                ["id"] = null,
                ["dynamicRoot"] = null
            },
            ["minNumber"] = MinItems,
            ["maxNumber"] = MaxItems,
            ["ignoreUserStartNodes"] = false,
            ["showOpenButton"] = false
        };
        return new EditorRecipe(key, name, "Umbraco.MultiNodeTreePicker", "Umb.PropertyEditorUi.ContentPicker", config, ValueStorageType.Ntext);
    }
}
