using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

/// <summary>
/// Base for property editors backed by Umbraco's "Block List" (<c>Umbraco.BlockList</c>) editor.
/// Unlike most other data types, subclass this directly on a per-property basis (no shared concrete
/// wrapper) since <see cref="Blocks"/> is specific to each property's content model — see
/// <see cref="BlockDefinition"/>.
/// </summary>
public abstract class BlockListDataType : DataTypeBase
{
    /// <summary>The block types available to editors, each pointing at an element type via <see cref="BlockDefinition.ContentType"/>.</summary>
    public virtual BlockDefinition[] Blocks { get; } = [];

    // Amount
    /// <summary>Minimum number of blocks required. Zero means no minimum.</summary>
    public virtual int MinAmount { get; } = 0;
    /// <summary>Maximum number of blocks allowed. Zero means unlimited.</summary>
    public virtual int MaxAmount { get; } = 0;

    // Editing modes
    /// <summary>Whether block content updates live as the editor types, without an explicit save step.</summary>
    public virtual bool LiveEditingMode { get; } = false;
    /// <summary>Whether blocks are edited inline in the list rather than in an overlay.</summary>
    public virtual bool InlineEditingMode { get; } = false;
    /// <summary>When exactly one block type is configured, whether to skip the block-type picker and go straight to editing.</summary>
    public virtual bool UseSingleBlockMode { get; } = false;

    // Editor appearance
    /// <summary>Maximum width of the property editor, e.g. "800px". Null uses Umbraco's default.</summary>
    public virtual string? EditorWidth { get; } = null;

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        var blocks = Blocks.Select(BuildBlockConfig).Cast<object>().ToList();

        IDictionary<string, object> config = new Dictionary<string, object> { ["blocks"] = blocks };

        if (MinAmount > 0 || MaxAmount > 0)
            config["validationLimit"] = new Dictionary<string, object?>
            {
                ["min"] = MinAmount > 0 ? MinAmount : null,
                ["max"] = MaxAmount > 0 ? MaxAmount : null
            };

        if (LiveEditingMode) config["liveEditingMode"] = true;
        if (InlineEditingMode) config["inlineEditingMode"] = true;
        if (UseSingleBlockMode) config["useSingleBlockMode"] = true;
        if (EditorWidth is not null) config["maxPropertyWidth"] = EditorWidth;

        return new EditorRecipe(key, name, "Umbraco.BlockList", "Umb.PropertyEditorUi.BlockList", config, ValueStorageType.Ntext);
    }

    private static Dictionary<string, object> BuildBlockConfig(BlockDefinition block)
    {
        var elemAttr = block.ContentType.GetCustomAttribute<ElementTypeAttribute>()
            ?? throw new InvalidOperationException($"BlockList block type '{block.ContentType.Name}' has no [ElementType] attribute.");

        var cfg = new Dictionary<string, object> { ["contentElementTypeKey"] = elemAttr.Key };

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

        return cfg;
    }
}
