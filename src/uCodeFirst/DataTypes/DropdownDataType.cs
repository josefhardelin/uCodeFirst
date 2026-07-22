using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Dropdown" (<c>Umbraco.DropDown.Flexible</c>) editor.</summary>
public abstract class DropdownDataType : DataTypeBase
{
    /// <summary>Whether more than one option may be selected.</summary>
    public virtual bool AllowMultiple { get; } = false;
    /// <summary>The selectable options shown in the dropdown.</summary>
    public virtual string[] Options { get; } = [];

    /// <inheritdoc/>
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
