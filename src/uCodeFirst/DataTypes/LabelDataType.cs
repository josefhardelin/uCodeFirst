using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class LabelDataType : DataTypeBase
{
    /// <summary>Underlying value type: STRING, INT, BIGINT, DATETIME, TIME or DECIMAL.</summary>
    public virtual string ValueType { get; } = "STRING";

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object> { ["umbracoDataValueType"] = ValueType };

        var storageType = ValueType switch
        {
            "INT" or "BIGINT" => ValueStorageType.Integer,
            "DATETIME" or "TIME" => ValueStorageType.Date,
            "DECIMAL" => ValueStorageType.Decimal,
            _ => ValueStorageType.Nvarchar
        };

        return new EditorRecipe(key, name, "Umbraco.Label", "Umb.PropertyEditorUi.Label", config, storageType);
    }
}
