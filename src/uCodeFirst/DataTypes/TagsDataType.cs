using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class TagsDataType : DataTypeBase
{
    public virtual string Group { get; } = "default";
    /// <summary>"Json" or "Csv".</summary>
    public virtual string StorageType { get; } = "Json";

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["group"] = Group,
            ["storageType"] = StorageType
        };
        return new EditorRecipe(key, name, "Umbraco.Tags", "Umb.PropertyEditorUi.Tags", config, ValueStorageType.Ntext);
    }
}
