using System.Reflection;

namespace uCodeFirst.Discovery;

// Master is null when a [Template]'s Master value didn't resolve to a member of the same enum
// (wrong enum type, or some other reflection mismatch) — PreFlightValidator turns that into a
// proper error instead of the scanner throwing.
internal sealed record TemplateDefinition(
    FieldInfo Member,
    string Alias,
    FieldInfo? Master);
