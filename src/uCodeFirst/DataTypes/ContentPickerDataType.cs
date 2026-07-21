using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class ContentPickerDataType : DataTypeBase
{
    /// <summary>Optional document-type alias filter, e.g. "article,newsItem".</summary>
    public virtual string? Filter { get; } = null;

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = Filter is not null
            ? new Dictionary<string, object> { ["filter"] = Filter }
            : new Dictionary<string, object>();

        return new EditorRecipe(key, name, "Umbraco.ContentPicker", "Umb.PropertyEditorUi.DocumentPicker", config);
    }
}
