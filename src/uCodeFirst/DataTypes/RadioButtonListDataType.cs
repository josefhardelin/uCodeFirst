using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class RadioButtonListDataType : DataTypeBase
{
    public virtual string[] Options { get; } = [];

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object> { ["items"] = Options.ToList() };
        return new EditorRecipe(key, name, "Umbraco.RadioButtonList", "Umb.PropertyEditorUi.RadioButtonList", config);
    }
}
