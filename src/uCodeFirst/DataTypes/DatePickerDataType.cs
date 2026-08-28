using uCodeFirst.DataTypes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Date Picker" (<c>Umbraco.DateTime</c>) editor.</summary>
public abstract class DatePickerDataType : DataTypeBase
{
    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.DateTime", "Umb.PropertyEditorUi.DatePicker", new Dictionary<string, object>(), ValueStorageType.Date);
}
