using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's multi-line "Text Area" (<c>Umbraco.TextArea</c>) editor.</summary>
public abstract class TextAreaDataType : DataTypeBase
{
    /// <summary>Maximum number of characters allowed. Zero means unlimited.</summary>
    public virtual int MaxLength { get; } = 0;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = MaxLength > 0
            ? new Dictionary<string, object> { ["maxChars"] = MaxLength }
            : new Dictionary<string, object>();
        return new EditorRecipe(key, name, "Umbraco.TextArea", "Umb.PropertyEditorUi.TextArea", config, ValueStorageType.Ntext);
    }
}
