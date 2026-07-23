namespace uCodeFirst.Sync;

internal sealed class TypeSyncPlan
{
    public List<PlanItem> ToCreate { get; } = new();
    public List<PlanItem> ToUpdate { get; } = new();
    public List<PrunedProperty> PrunedProperties { get; } = new();
    public List<PrunedGroup> PrunedGroups { get; } = new();
}

internal sealed record PlanItem(string Alias, Guid Key);

internal sealed record PrunedProperty(string TypeAlias, string PropertyAlias);

internal sealed record PrunedGroup(string TypeAlias, string GroupAlias);
