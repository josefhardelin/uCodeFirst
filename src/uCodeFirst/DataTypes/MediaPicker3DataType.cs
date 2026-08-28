using uCodeFirst.DataTypes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Media Picker" (<c>Umbraco.MediaPicker3</c>) editor.</summary>
public abstract class MediaPicker3DataType : DataTypeBase
{
    /// <summary>Whether more than one media item may be picked.</summary>
    public virtual bool AllowMultiple { get; } = false;
    /// <summary>Whether editors can adjust the local focal point/crop for each picked item.</summary>
    public virtual bool EnableCrops { get; } = false;
    /// <summary>Minimum number of items required. Zero means no minimum.</summary>
    public virtual int MinItems { get; } = 0;
    /// <summary>Maximum number of items allowed. Zero means unlimited.</summary>
    public virtual int MaxItems { get; } = 0;

    /// <inheritdoc/>
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
