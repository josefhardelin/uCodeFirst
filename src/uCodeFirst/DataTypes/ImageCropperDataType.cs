using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class ImageCropperDataType : DataTypeBase
{
    public virtual CropDefinition[] Crops { get; } = [];

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = Crops.Length > 0
            ? new Dictionary<string, object>
            {
                ["crops"] = Crops.Select(c => (object)new Dictionary<string, object>
                {
                    ["alias"] = c.Alias,
                    ["width"] = c.Width,
                    ["height"] = c.Height
                }).ToList()
            }
            : new Dictionary<string, object>();

        return new EditorRecipe(key, name, "Umbraco.ImageCropper", "Umb.PropertyEditorUi.ImageCropper", config, ValueStorageType.Ntext);
    }
}

/// <summary>A named crop for an <see cref="ImageCropperDataType"/> configuration.</summary>
public sealed record CropDefinition(string Alias, int Width, int Height);
