using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Multinode Treepicker" (<c>Umbraco.MultiNodeTreePicker</c>) editor.</summary>
public abstract class MultiNodeTreePickerDataType : DataTypeBase
{
    /// <summary>Root object type to pick from: "content", "media" or "member".</summary>
    public virtual string StartNodeType { get; } = "content";
    /// <summary>Minimum number of items required. Zero means no minimum.</summary>
    public virtual int MinItems { get; } = 0;
    /// <summary>Maximum number of items allowed. Zero means unlimited.</summary>
    public virtual int MaxItems { get; } = 0;

    /// <summary>
    /// Optional dynamic root: computes the start node relative to the current content at render time
    /// instead of a fixed node. Mutually exclusive with a fixed start node id — Umbraco's own start
    /// node model only supports one or the other, and this library never sets a fixed id, so this is
    /// the only way to configure the picker's root beyond the default (<see cref="StartNodeType"/> alone).
    /// </summary>
    public virtual DynamicRootConfig? DynamicRoot { get; } = null;

    /// <summary>Optional document-type filter restricting which content types may be picked. Empty means no restriction.</summary>
    public virtual Type[] AllowedContentTypes { get; } = [];

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        var startNode = new Dictionary<string, object?>
        {
            ["type"] = StartNodeType,
            ["id"] = null,
            ["dynamicRoot"] = DynamicRoot is not null ? BuildDynamicRootConfig(DynamicRoot) : null
        };

        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["startNode"] = startNode,
            ["minNumber"] = MinItems,
            ["maxNumber"] = MaxItems,
            ["ignoreUserStartNodes"] = false,
            ["showOpenButton"] = false
        };

        if (AllowedContentTypes.Length > 0)
            config["filter"] = string.Join(',', AllowedContentTypes.Select(ResolveAlias));

        return new EditorRecipe(key, name, "Umbraco.MultiNodeTreePicker", "Umb.PropertyEditorUi.ContentPicker", config, ValueStorageType.Ntext);
    }

    private static Dictionary<string, object?> BuildDynamicRootConfig(DynamicRootConfig dynamicRoot) =>
        new()
        {
            ["originAlias"] = OriginAlias(dynamicRoot.Origin),
            ["originKey"] = null,
            ["querySteps"] = dynamicRoot.QuerySteps.Select(BuildQueryStepConfig).Cast<object>().ToList()
        };

    private static Dictionary<string, object> BuildQueryStepConfig(DynamicRootQueryStep step) =>
        new()
        {
            ["alias"] = DirectionAlias(step.Direction),
            ["anyOfDocTypeKeys"] = step.DocumentTypes.Select(ResolveKey).ToList()
        };

    // Literal alias strings confirmed against Umbraco.Cms.Core 17.4.2 (Umbraco.Cms.Core.DynamicRoot.Origin.*DynamicRootOriginFinder).
    private static string OriginAlias(DynamicRootOrigin origin) => origin switch
    {
        DynamicRootOrigin.Root => "Root",
        DynamicRootOrigin.Site => "Site",
        DynamicRootOrigin.Current => "Current",
        DynamicRootOrigin.Parent => "Parent",
        DynamicRootOrigin.ContentRoot => "ContentRoot",
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
    };

    // Literal alias strings confirmed against Umbraco.Cms.Core 17.4.2 (Umbraco.Cms.Core.DynamicRoot.QuerySteps.*DynamicRootQueryStep).
    private static string DirectionAlias(DynamicRootQueryStepDirection direction) => direction switch
    {
        DynamicRootQueryStepDirection.NearestAncestorOrSelf => "NearestAncestorOrSelf",
        DynamicRootQueryStepDirection.NearestDescendantOrSelf => "NearestDescendantOrSelf",
        DynamicRootQueryStepDirection.FurthestAncestorOrSelf => "FurthestAncestorOrSelf",
        DynamicRootQueryStepDirection.FurthestDescendantOrSelf => "FurthestDescendantOrSelf",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };

    private static Guid ResolveKey(Type documentType) =>
        (documentType.GetCustomAttribute<DocumentTypeAttribute>()
            ?? throw new InvalidOperationException($"MultiNodeTreePicker dynamic root query step references '{documentType.FullName}' which has no [DocumentType] attribute.")).Key;

    private static string ResolveAlias(Type documentType)
    {
        var attr = documentType.GetCustomAttribute<DocumentTypeAttribute>()
            ?? throw new InvalidOperationException($"MultiNodeTreePicker AllowedContentTypes references '{documentType.FullName}' which has no [DocumentType] attribute.");
        return attr.Alias ?? DocumentTypeScanner.ToAlias(documentType.Name);
    }
}
