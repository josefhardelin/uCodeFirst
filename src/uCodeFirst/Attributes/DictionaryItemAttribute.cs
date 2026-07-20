namespace uCodeFirst.Attributes;

/// <summary>
/// Marks a <c>const string</c> field as a code-first Umbraco dictionary item. Use
/// <c>nameof(FieldName)</c> as the field's value so the C# identifier and the Umbraco
/// dictionary <c>ItemKey</c> are always the same text — renames stay in sync automatically.
/// Nest the field inside a static class to create a real parent dictionary item named after
/// that class (nesting can be arbitrarily deep); fields declared at the top level become
/// root dictionary items.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DictionaryItemAttribute : Attribute
{
}
