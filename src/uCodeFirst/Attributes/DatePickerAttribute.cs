namespace uCodeFirst.Attributes;

public sealed class DatePickerAttribute : PropertyEditorAttribute
{
    public DatePickerAttribute(
        string? Name = null,
        string? Alias = null,
        bool Mandatory = false,
        string? Description = null)
        : base(Name, Alias, Mandatory, Description)
    {
    }
}
