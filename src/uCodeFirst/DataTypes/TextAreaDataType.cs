using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class TextAreaDataType : DataTypeBase
{
    public virtual int MaxLength { get; } = 0;

    internal override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = MaxLength > 0
            ? new Dictionary<string, object> { ["maxChars"] = MaxLength }
            : new Dictionary<string, object>();
        return new EditorRecipe(key, name, "Umbraco.TextArea", "Umb.PropertyEditorUi.TextArea", config, ValueStorageType.Ntext);
    }
}
