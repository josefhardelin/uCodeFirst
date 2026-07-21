using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class NumericDataType : DataTypeBase
{
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.Integer", "Umb.PropertyEditorUi.Integer", new Dictionary<string, object>(), ValueStorageType.Integer);
}
