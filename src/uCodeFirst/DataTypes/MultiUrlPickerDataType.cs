using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class MultiUrlPickerDataType : DataTypeBase
{
    public virtual int MinItems { get; } = 0;
    public virtual int MaxItems { get; } = 0;

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>();

        if (MinItems > 0) config["minNumber"] = MinItems;
        if (MaxItems > 0) config["maxNumber"] = MaxItems;

        return new EditorRecipe(key, name, "Umbraco.MultiUrlPicker", "Umb.PropertyEditorUi.MultiUrlPicker", config, ValueStorageType.Ntext);
    }
}
