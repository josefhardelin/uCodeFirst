namespace uCodeFirst.Discovery;

// DocumentType and Parent are kept as raw CLR Type references (not yet resolved to a Key/alias) so
// PreFlightValidator can report a full type name in dangling-reference/cycle errors, matching how
// AllowedChildTypes is carried on DocumentTypeDefinition. Resolution to an actual created content id
// happens at sync time in ContentSeedingEngine.
internal sealed record SeedContentDefinition(
    Type ClrType,
    Guid Key,
    Type DocumentType,
    string Name,
    Type? Parent);
