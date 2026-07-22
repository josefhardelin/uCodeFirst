using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>Base for property editors backed by Umbraco's "Image Cropper" (<c>Umbraco.ImageCropper</c>) editor.</summary>
public abstract class ImageCropperDataType : DataTypeBase
{
    /// <summary>Named crops made available to editors, in addition to the default crop.</summary>
    public virtual CropDefinition[] Crops { get; } = [];

    /// <inheritdoc/>
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
/// <param name="Alias">Alias of the crop, used to retrieve it via Umbraco's cropping APIs.</param>
/// <param name="Width">Crop width in pixels.</param>
/// <param name="Height">Crop height in pixels.</param>
public sealed record CropDefinition(string Alias, int Width, int Height);
