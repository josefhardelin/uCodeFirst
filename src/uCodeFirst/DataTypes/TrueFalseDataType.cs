using uCodeFirst.DataTypes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "True/False" (<c>Umbraco.TrueFalse</c>) toggle editor.</summary>
public abstract class TrueFalseDataType : DataTypeBase
{
    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.TrueFalse", "Umb.PropertyEditorUi.Toggle", new Dictionary<string, object>(), ValueStorageType.Integer);
}
