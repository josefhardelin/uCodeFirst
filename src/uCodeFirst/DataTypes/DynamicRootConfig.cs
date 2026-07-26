namespace uCodeFirst.DataTypes;

/// <summary>
/// Structural origin point for a <see cref="DynamicRootConfig"/>, mapped to Umbraco's own dynamic-root
/// origin-finder aliases (<c>Umbraco.Cms.Core.DynamicRoot.Origin.*DynamicRootOriginFinder</c>).
/// Deliberately excludes Umbraco's "ByKey" origin (a fixed reference to one specific content
/// instance) — there is no seeded content for C# attributes to point at yet. That case is deferred
/// to the roadmap's "Content seeding" item.
/// </summary>
public enum DynamicRootOrigin
{
    /// <summary>The content tree root: the first content item below the system root, found by walking up from the current or parent content.</summary>
    Root,
    /// <summary>The nearest ancestor-or-self content item with an assigned domain (the site root), falling back to <see cref="Root"/> if none is found.</summary>
    Site,
    /// <summary>The content item currently being edited.</summary>
    Current,
    /// <summary>The parent of the content item currently being edited.</summary>
    Parent,
    /// <summary>The system root itself (Umbraco's content root node), not a content item below it.</summary>
    ContentRoot
}

/// <summary>
/// Traversal direction for a <see cref="DynamicRootQueryStep"/>, mapped to Umbraco's own query-step
/// aliases (<c>Umbraco.Cms.Core.DynamicRoot.QuerySteps.*DynamicRootQueryStep</c>).
/// </summary>
public enum DynamicRootQueryStepDirection
{
    /// <summary>The closest matching ancestor of, or the origin itself.</summary>
    NearestAncestorOrSelf,
    /// <summary>The closest matching descendant of, or the origin itself.</summary>
    NearestDescendantOrSelf,
    /// <summary>The topmost matching ancestor of, or the origin itself.</summary>
    FurthestAncestorOrSelf,
    /// <summary>The deepest matching descendant(s) of, or the origin itself.</summary>
    FurthestDescendantOrSelf
}

/// <summary>
/// One step of a <see cref="DynamicRootConfig"/>'s traversal from its <see cref="DynamicRootConfig.Origin"/>,
/// narrowing the result set to nodes of the given document types.
/// </summary>
public sealed record DynamicRootQueryStep
{
    /// <summary>Traversal direction applied at this step.</summary>
    public required DynamicRootQueryStepDirection Direction { get; init; }

    /// <summary>Document types the traversal may match at this step. Each must carry <see cref="Attributes.DocumentTypeAttribute"/>.</summary>
    public required Type[] DocumentTypes { get; init; }
}

/// <summary>
/// Configures a <see cref="MultiNodeTreePickerDataType"/>'s start node as an Umbraco "dynamic root" —
/// a start node computed at render time relative to the current content, instead of a fixed node id.
/// </summary>
public sealed record DynamicRootConfig
{
    /// <summary>Structural starting point the traversal begins from.</summary>
    public required DynamicRootOrigin Origin { get; init; }

    /// <summary>Ordered traversal steps narrowing the result set from <see cref="Origin"/>. Empty means the origin itself is the root.</summary>
    public DynamicRootQueryStep[] QuerySteps { get; init; } = [];
}
