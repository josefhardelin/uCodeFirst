using uCodeFirst.DataTypes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Member Picker" (<c>Umbraco.MemberPicker</c>) editor.</summary>
public abstract class MemberPickerDataType : DataTypeBase
{
    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.MemberPicker", "Umb.PropertyEditorUi.MemberPicker", new Dictionary<string, object>());
}
