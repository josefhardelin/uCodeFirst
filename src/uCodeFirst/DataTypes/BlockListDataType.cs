using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Sync;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.DataTypes;

public abstract class BlockListDataType : DataTypeBase
{
    public virtual BlockDefinition[] Blocks { get; } = [];

    // Amount
    public virtual int MinAmount { get; } = 0;
    public virtual int MaxAmount { get; } = 0;

    // Editing modes
    public virtual bool LiveEditingMode { get; } = false;
    public virtual bool InlineEditingMode { get; } = false;
    public virtual bool UseSingleBlockMode { get; } = false;

    // Editor appearance
    public virtual string? EditorWidth { get; } = null;

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
