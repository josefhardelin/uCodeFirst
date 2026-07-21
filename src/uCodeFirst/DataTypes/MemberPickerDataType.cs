using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class MemberPickerDataType : DataTypeBase
{
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.MemberPicker", "Umb.PropertyEditorUi.MemberPicker", new Dictionary<string, object>());
}
