using System.Reflection;

namespace uCodeFirst.Discovery;

// ParentChain is ordered outermost-to-innermost, excluding the top-level (non-nested) declaring
// type — each entry becomes a real parent DictionaryItem named after that class.
internal sealed record DictionaryItemDefinition(
    FieldInfo Field,
    string ItemKey,
    IReadOnlyList<Type> ParentChain);
