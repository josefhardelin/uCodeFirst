namespace uCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class BlockListAttribute : PropertyEditorAttribute
{
    public BlockListAttribute(params Type[] blockTypes) : base()
    {
        BlockTypes = blockTypes;
    }

    public Type[] BlockTypes { get; }
}
