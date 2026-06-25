using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class DatePickerDataType : DataTypeBase
{
    internal override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.DateTime", "Umb.PropertyEditorUi.DatePicker", new Dictionary<string, object>(), ValueStorageType.Date);
}
