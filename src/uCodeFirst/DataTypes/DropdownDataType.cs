using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class DropdownDataType : DataTypeBase
{
    public virtual bool AllowMultiple { get; } = false;
    public virtual string[] Options { get; } = [];

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["multiple"] = AllowMultiple,
            ["items"] = Options.ToList()
        };
        return new EditorRecipe(key, name, "Umbraco.DropDown.Flexible", "Umb.PropertyEditorUi.Dropdown", config);
    }
}
