using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Rich Text" (<c>Umbraco.RichText</c>, Tiptap) editor.</summary>
public abstract class RichTextDataType : DataTypeBase
{
    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.RichText", "Umb.PropertyEditorUi.Tiptap", new Dictionary<string, object>(), ValueStorageType.Ntext);
}
