using System.Reflection;

namespace uCodeFirst.Discovery;

internal sealed record LanguageDefinition(
    FieldInfo Member,
    string IsoCode,
    FieldInfo? Fallback,
    bool IsMandatory);

// DefaultMember is null when [Languages]'s DefaultLanguage value didn't resolve to a member of
// EnumType (wrong enum type, or some other reflection mismatch) — PreFlightValidator turns that
// into a proper error instead of the scanner throwing.
internal sealed record LanguageSetDefinition(
    Type EnumType,
    FieldInfo? DefaultMember,
    IReadOnlyList<LanguageDefinition> Languages);
