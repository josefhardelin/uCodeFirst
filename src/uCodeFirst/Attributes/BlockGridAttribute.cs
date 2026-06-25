namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class BlockGridAttribute : PropertyEditorAttribute
{
    public BlockGridAttribute(params Type[] blockTypes) : base()
    {
        BlockTypes = blockTypes;
    }

    public Type[] BlockTypes { get; }
    public int GridColumns { get; init; } = 12;
}
