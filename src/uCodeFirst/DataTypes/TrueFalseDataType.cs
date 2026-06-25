using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class TrueFalseDataType : DataTypeBase
{
    internal override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.TrueFalse", "Umb.PropertyEditorUi.Toggle", new Dictionary<string, object>(), ValueStorageType.Integer);
}
