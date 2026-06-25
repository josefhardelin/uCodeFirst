namespace uCodeFirst.DataTypes;

public sealed record BlockDefinition
{
    public required Type ContentType { get; init; }

    // Data models
    public Type? SettingsType { get; init; }

    // Block appearance
    public string? Label { get; init; }
    public string? OverlayEditorSize { get; init; }  // "Small" | "Medium" | "Large" | "Full"

    // Catalogue appearance
    public string? BackgroundColor { get; init; }
    public string? IconColor { get; init; }
    public string? Thumbnail { get; init; }

    // Advanced
    public bool HideContentEditor { get; init; }

    // BlockGrid-only
    public bool AllowAtRoot { get; init; } = true;
    public bool AllowInAreas { get; init; } = false;
    public int[]? ColumnSpanOptions { get; init; }
    public int RowMinSpan { get; init; } = 1;
    public int RowMaxSpan { get; init; } = 1;
}
