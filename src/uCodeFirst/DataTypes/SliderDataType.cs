using uCodeFirst.DataTypes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Slider" (<c>Umbraco.Slider</c>) editor.</summary>
public abstract class SliderDataType : DataTypeBase
{
    /// <summary>Minimum value on the slider.</summary>
    public virtual int MinValue { get; } = 0;
    /// <summary>Maximum value on the slider.</summary>
    public virtual int MaxValue { get; } = 100;
    /// <summary>Increment between selectable values.</summary>
    public virtual int Step { get; } = 1;
    /// <summary>Whether the slider selects a range (two values) instead of a single value.</summary>
    public virtual bool EnableRange { get; } = false;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["minVal"] = MinValue,
            ["maxVal"] = MaxValue,
            ["step"] = Step,
            ["enableRange"] = EnableRange,
            ["initVal1"] = MinValue,
            ["initVal2"] = MaxValue
        };
        return new EditorRecipe(key, name, "Umbraco.Slider", "Umb.PropertyEditorUi.Slider", config);
    }
}
