namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a <c>const string</c> field as a code-first Umbraco dictionary item. Use
/// <c>nameof(FieldName)</c> as the field's value so the C# identifier and the Umbraco
/// dictionary <c>ItemKey</c> are always the same text — renames stay in sync automatically.
/// Nest the field inside a static class to create a real parent dictionary item named after
/// that class (nesting can be arbitrarily deep); fields declared at the top level become
/// root dictionary items. Set <see cref="Alias"/> when the Umbraco item key needs characters a
/// C# identifier can't hold (e.g. spaces); it can also be placed on the static class itself to
/// override a parent item's key the same way.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DictionaryItemAttribute : Attribute
{
    /// <summary>
    /// Overrides the Umbraco dictionary item key. Defaults to the const field's value (field
    /// target) or the class name (class target) when left unset.
    /// </summary>
    public string? Alias { get; set; }
}
