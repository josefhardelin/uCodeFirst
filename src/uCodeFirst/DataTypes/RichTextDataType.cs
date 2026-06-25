using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class RichTextDataType : DataTypeBase
{
    internal override EditorRecipe BuildRecipe(Guid key, string name) =>
        new(key, name, "Umbraco.RichText", "Umb.PropertyEditorUi.Tiptap", new Dictionary<string, object>(), ValueStorageType.Ntext);
}
