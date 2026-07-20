using uCodeFirst.Attributes;

namespace Basicv17.Models.Dictionary;

// Code owns dictionary keys and hierarchy only — never translation values. Sync creates any
// missing key with empty translations and never touches an existing item; content editors own
// the actual translated text via the backoffice. Nested static classes create real parent
// dictionary items in the tree; top-level fields become root items.
public static class DictionaryKeys
{
    public static class Buttons
    {
        [DictionaryItem]
        public const string Submit = nameof(Submit);

        [DictionaryItem]
        public const string Cancel = nameof(Cancel);
    }

    [DictionaryItem]
    public const string SiteTitle = nameof(SiteTitle);
}
