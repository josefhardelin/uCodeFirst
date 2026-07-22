using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Checkbox List" (<c>Umbraco.CheckBoxList</c>) editor.</summary>
public abstract class CheckBoxListDataType : DataTypeBase
{
    /// <summary>The selectable options shown as checkboxes.</summary>
    public virtual string[] Options { get; } = [];

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object> { ["items"] = Options.ToList() };
        return new EditorRecipe(key, name, "Umbraco.CheckBoxList", "Umb.PropertyEditorUi.CheckBoxList", config);
    }
}
