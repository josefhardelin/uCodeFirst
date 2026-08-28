using System.Reflection;

namespace uCodeFirst.Discovery;

// ParentChain is ordered outermost-to-innermost, excluding the top-level (non-nested) declaring
// type — each entry is the resolved key of a real parent DictionaryItem (the class's
// [DictionaryItem(Alias = ...)] override if set, else its C# name).
internal sealed record DictionaryItemDefinition(
    FieldInfo Field,
    string ItemKey,
    IReadOnlyList<string> ParentChain);
