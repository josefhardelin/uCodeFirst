using uCodeFirst.Attributes;

namespace Basicv17.Models.Dictionary;

// Code owns dictionary keys and hierarchy only — never translation values. Sync creates any
// missing key with empty translations and never touches an existing item; content editors own
// the actual translated text via the backoffice. Nested static classes create real parent
// dictionary items in the tree; top-level fields become root items. A C# identifier can't hold
// spaces, so use [DictionaryItem(Alias = "...")] — on a field to override the key that would
// otherwise come from the const's nameof(...) value, or on a static class to override the parent
// item's key that would otherwise come from the class name — when the real Umbraco key needs them.
public static class DictionaryKeys
{
    public static class Buttons
    {
        [DictionaryItem]
        public const string Submit = nameof(Submit);

        [DictionaryItem]
        public const string Cancel = nameof(Cancel);

        [DictionaryItem(Alias = "Button Text")]
        public const string ButtonText = nameof(ButtonText);
    }

    [DictionaryItem(Alias = "Site Footer")]
    public static class SiteFooter
    {
        [DictionaryItem]
        public const string Copyright = nameof(Copyright);
    }

    [DictionaryItem]
    public const string SiteTitle = nameof(SiteTitle);
}
