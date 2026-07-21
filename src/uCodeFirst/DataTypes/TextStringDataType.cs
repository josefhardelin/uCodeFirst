using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class TextStringDataType : DataTypeBase
{
    public virtual int MaxLength { get; } = 0;

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = MaxLength > 0
            ? new Dictionary<string, object> { ["maxChars"] = MaxLength }
            : new Dictionary<string, object>();
        return new EditorRecipe(key, name, "Umbraco.TextBox", "Umb.PropertyEditorUi.TextBox", config);
    }
}
