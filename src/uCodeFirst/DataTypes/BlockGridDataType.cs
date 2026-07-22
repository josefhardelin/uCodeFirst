using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>
/// Base for property editors backed by Umbraco's "Block Grid" (<c>Umbraco.BlockGrid</c>) editor.
/// Unlike most other data types, subclass this directly on a per-property basis (no shared concrete
/// wrapper) since <see cref="Blocks"/> is specific to each property's content model — see
/// <see cref="BlockDefinition"/>.
/// </summary>
public abstract class BlockGridDataType : DataTypeBase
{
    /// <summary>The block types available to editors, each pointing at an element type via <see cref="BlockDefinition.ContentType"/>.</summary>
    public virtual BlockDefinition[] Blocks { get; } = [];
    /// <summary>Total number of columns in the grid's layout.</summary>
    public virtual int GridColumns { get; } = 12;
    /// <summary>Path to a stylesheet defining custom layout classes for the grid. Null uses Umbraco's default layout.</summary>
    public virtual string? LayoutStylesheet { get; } = null;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        var blocks = Blocks.Select(BuildBlockConfig).Cast<object>().ToList();

        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["blocks"] = blocks,
            ["gridColumns"] = GridColumns
        };

        if (LayoutStylesheet is not null) config["layoutStylesheet"] = LayoutStylesheet;

        return new EditorRecipe(key, name, "Umbraco.BlockGrid", "Umb.PropertyEditorUi.BlockGrid", config, ValueStorageType.Ntext);
    }

    private static Dictionary<string, object> BuildBlockConfig(BlockDefinition block)
    {
        var elemAttr = block.ContentType.GetCustomAttribute<ElementTypeAttribute>()
            ?? throw new InvalidOperationException($"BlockGrid block type '{block.ContentType.Name}' has no [ElementType] attribute.");

        var cfg = new Dictionary<string, object>
        {
            ["contentElementTypeKey"] = elemAttr.Key,
            ["allowAtRoot"] = block.AllowAtRoot,
            ["allowInAreas"] = block.AllowInAreas,
            ["areas"] = new List<object>()
        };

        if (block.SettingsType is not null)
        {
            var settingsAttr = block.SettingsType.GetCustomAttribute<ElementTypeAttribute>();
            if (settingsAttr is not null)
                cfg["settingsElementTypeKey"] = settingsAttr.Key;
        }

        if (block.Label is not null) cfg["label"] = block.Label;
        if (block.OverlayEditorSize is not null) cfg["editorSize"] = block.OverlayEditorSize;
        if (block.HideContentEditor) cfg["forceHideContentEditorInOverlay"] = true;
        if (block.BackgroundColor is not null) cfg["backgroundColor"] = block.BackgroundColor;
        if (block.IconColor is not null) cfg["iconColor"] = block.IconColor;
        if (block.Thumbnail is not null) cfg["thumbnail"] = block.Thumbnail;

        if (block.ColumnSpanOptions is { Length: > 0 })
            cfg["columnSpanOptions"] = block.ColumnSpanOptions
                .Select(c => (object)new Dictionary<string, object> { ["columnSpan"] = c })
                .ToList();

        if (block.RowMinSpan != 1) cfg["rowMinSpan"] = block.RowMinSpan;
        if (block.RowMaxSpan != 1) cfg["rowMaxSpan"] = block.RowMaxSpan;

        return cfg;
    }
}
