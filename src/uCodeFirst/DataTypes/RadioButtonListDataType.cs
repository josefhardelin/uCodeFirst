using uCodeFirst.DataTypes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Radio Button List" (<c>Umbraco.RadioButtonList</c>) editor.</summary>
public abstract class RadioButtonListDataType : DataTypeBase
{
    /// <summary>The selectable options shown as radio buttons.</summary>
    public virtual string[] Options { get; } = [];

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object> { ["items"] = Options.ToList() };
        return new EditorRecipe(key, name, "Umbraco.RadioButtonList", "Umb.PropertyEditorUi.RadioButtonList", config);
    }
}
