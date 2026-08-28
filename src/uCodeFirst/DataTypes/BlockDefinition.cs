using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>
/// Declares one block available to a <see cref="BlockListDataType"/> or <see cref="BlockGridDataType"/>
/// property. <see cref="ContentType"/> must carry <see cref="Attributes.ElementTypeAttribute"/>. The
/// <c>BlockGrid</c>-only properties are ignored when used with <see cref="BlockListDataType"/>.
/// </summary>
public sealed record BlockDefinition
{
    /// <summary>Element type used as this block's content model. Must carry <see cref="Attributes.ElementTypeAttribute"/>.</summary>
    public required Type ContentType { get; init; }

    // Data models
    /// <summary>Optional element type used as this block's settings model. Must carry <see cref="Attributes.ElementTypeAttribute"/>.</summary>
    public Type? SettingsType { get; init; }

    // Block appearance
    /// <summary>Label template shown for block instances in the editor, e.g. "{{title}}".</summary>
    public string? Label { get; init; }
    /// <summary>Size of the content-editing overlay: "Small", "Medium", "Large" or "Full".</summary>
    public string? OverlayEditorSize { get; init; }  // "Small" | "Medium" | "Large" | "Full"

    // Catalogue appearance
    /// <summary>Background color for the block in the block catalogue.</summary>
    public string? BackgroundColor { get; init; }
    /// <summary>Icon color for the block in the block catalogue.</summary>
    public string? IconColor { get; init; }
    /// <summary>Thumbnail image path shown for the block in the block catalogue.</summary>
    public string? Thumbnail { get; init; }

    // Advanced
    /// <summary>When true, hides the content editor and forces the block straight into the settings overlay.</summary>
    public bool HideContentEditor { get; init; }

    // BlockGrid-only
    /// <summary>BlockGrid only. Whether the block may be placed directly at the grid root, outside any area.</summary>
    public bool AllowAtRoot { get; init; } = true;
    /// <summary>BlockGrid only. Whether the block may be placed inside a nested area of another block.</summary>
    public bool AllowInAreas { get; init; } = false;
    /// <summary>BlockGrid only. Column-span choices offered to editors, out of <see cref="BlockGridDataType.GridColumns"/>.</summary>
    public int[]? ColumnSpanOptions { get; init; }
    /// <summary>BlockGrid only. Minimum number of rows the block may span.</summary>
    public int RowMinSpan { get; init; } = 1;
    /// <summary>BlockGrid only. Maximum number of rows the block may span.</summary>
    public int RowMaxSpan { get; init; } = 1;
}
