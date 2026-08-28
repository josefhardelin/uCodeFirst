using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Content Picker" (<c>Umbraco.ContentPicker</c>) editor.</summary>
public abstract class ContentPickerDataType : DataTypeBase
{
    /// <summary>Optional document-type filter restricting which content types may be picked. Empty means no restriction.</summary>
    public virtual Type[] AllowedContentTypes { get; } = [];

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = AllowedContentTypes.Length > 0
            ? new Dictionary<string, object> { ["filter"] = string.Join(',', AllowedContentTypes.Select(ResolveAlias)) }
            : new Dictionary<string, object>();

        return new EditorRecipe(key, name, "Umbraco.ContentPicker", "Umb.PropertyEditorUi.DocumentPicker", config);
    }

    private static string ResolveAlias(Type documentType)
    {
        var attr = documentType.GetCustomAttribute<DocumentTypeAttribute>()
            ?? throw new InvalidOperationException($"ContentPicker AllowedContentTypes references '{documentType.FullName}' which has no [DocumentType] attribute.");
        return attr.Alias ?? DocumentTypeScanner.ToAlias(documentType.Name);
    }
}
