using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Tags" (<c>Umbraco.Tags</c>) editor.</summary>
public abstract class TagsDataType : DataTypeBase
{
    /// <summary>Tag group tags are stored under, allowing separate tag pools per group.</summary>
    public virtual string Group { get; } = "default";
    /// <summary>"Json" or "Csv".</summary>
    public virtual string StorageType { get; } = "Json";

    /// <inheritdoc/>
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
