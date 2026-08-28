using uCodeFirst.DataTypes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Numeric" (<c>Umbraco.Integer</c>) editor.</summary>
public abstract class NumericDataType : DataTypeBase
{
    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.Integer", "Umb.PropertyEditorUi.Integer", new Dictionary<string, object>(), ValueStorageType.Integer);
}
