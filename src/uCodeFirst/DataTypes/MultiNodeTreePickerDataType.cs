using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class MultiNodeTreePickerDataType : DataTypeBase
{
    /// <summary>Root object type to pick from: "content", "media" or "member".</summary>
    public virtual string StartNodeType { get; } = "content";
    public virtual int MinItems { get; } = 0;
    public virtual int MaxItems { get; } = 0;

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
