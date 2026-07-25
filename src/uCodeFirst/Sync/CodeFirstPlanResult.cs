namespace uCodeFirst.Sync;

// Serializable snapshot of a dry-run plan — the data behind both the startup dry-run log
// (CodeFirstSyncService.PlanAsync) and the on-demand backoffice dashboard endpoint
// (uCodeFirst.Api.PlanCodeFirstController). Scoped to content types and media types only, same as
// TypeSyncPlan — data types, dictionary items, languages, and templates aren't previewed yet.
internal sealed class CodeFirstPlanResult
{
    public required bool Enabled { get; init; }

    public required string Strategy { get; init; }

    public required DateTime GeneratedAtUtc { get; init; }

    public required IReadOnlyList<string> ToCreate { get; init; }

    public required IReadOnlyList<string> ToUpdate { get; init; }

    public required IReadOnlyList<PrunedProperty> PrunedProperties { get; init; }

    public required IReadOnlyList<PrunedGroup> PrunedGroups { get; init; }
}
