using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes;

public abstract class SliderDataType : DataTypeBase
{
    public virtual int MinValue { get; } = 0;
    public virtual int MaxValue { get; } = 100;
    public virtual int Step { get; } = 1;
    public virtual bool EnableRange { get; } = false;

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
