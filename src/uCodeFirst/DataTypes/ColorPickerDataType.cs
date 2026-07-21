using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class ColorPickerDataType : DataTypeBase
{
    /// <summary>Approved palette as "label:#hex" pairs. Empty allows any color.</summary>
    public virtual string[] Palette { get; } = [];
    public virtual bool ShowLabels { get; } = false;

    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object> { ["useLabel"] = ShowLabels };

        if (Palette.Length > 0)
            config["items"] = Palette.Select(entry =>
            {
                var parts = entry.Split(':', 2);
                var label = parts.Length == 2 ? parts[0] : parts[0];
                var value = parts.Length == 2 ? parts[1] : parts[0];
                return (object)new Dictionary<string, object> { ["label"] = label, ["value"] = value };
            }).ToList();

        return new EditorRecipe(key, name, "Umbraco.ColorPicker", "Umb.PropertyEditorUi.ColorPicker", config);
    }
}
