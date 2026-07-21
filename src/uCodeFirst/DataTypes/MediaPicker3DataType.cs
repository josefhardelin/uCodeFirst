using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class MediaPicker3DataType : DataTypeBase
{
    public virtual bool AllowMultiple { get; } = false;
    public virtual bool EnableCrops { get; } = false;
    public virtual int MinItems { get; } = 0;
    public virtual int MaxItems { get; } = 0;

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["multiple"] = AllowMultiple,
            ["enableLocalFocalPoint"] = EnableCrops,
            ["ignoreUserStartNodes"] = false
        };

        if (MinItems > 0 || MaxItems > 0)
            config["validationLimit"] = new Dictionary<string, object?>
            {
                ["min"] = MinItems > 0 ? MinItems : null,
                ["max"] = MaxItems > 0 ? MaxItems : null
            };

        return new EditorRecipe(key, name, "Umbraco.MediaPicker3", "Umb.PropertyEditorUi.MediaPicker", config, ValueStorageType.Ntext);
    }
}
