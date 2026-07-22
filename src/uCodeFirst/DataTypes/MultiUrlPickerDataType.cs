using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Multi URL Picker" (<c>Umbraco.MultiUrlPicker</c>) editor.</summary>
public abstract class MultiUrlPickerDataType : DataTypeBase
{
    /// <summary>Minimum number of links required. Zero means no minimum.</summary>
    public virtual int MinItems { get; } = 0;
    /// <summary>Maximum number of links allowed. Zero means unlimited.</summary>
    public virtual int MaxItems { get; } = 0;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>();

        if (MinItems > 0) config["minNumber"] = MinItems;
        if (MaxItems > 0) config["maxNumber"] = MaxItems;

        return new EditorRecipe(key, name, "Umbraco.MultiUrlPicker", "Umb.PropertyEditorUi.MultiUrlPicker", config, ValueStorageType.Ntext);
    }
}
