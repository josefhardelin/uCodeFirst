using uCodeFirst.DataTypes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's single-line "Text String" (<c>Umbraco.TextBox</c>) editor.</summary>
public abstract class TextStringDataType : DataTypeBase
{
    /// <summary>Maximum number of characters allowed. Zero means unlimited.</summary>
    public virtual int MaxLength { get; } = 0;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = MaxLength > 0
            ? new Dictionary<string, object> { ["maxChars"] = MaxLength }
            : new Dictionary<string, object>();
        return new EditorRecipe(key, name, "Umbraco.TextBox", "Umb.PropertyEditorUi.TextBox", config);
    }
}
