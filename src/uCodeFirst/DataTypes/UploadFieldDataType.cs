using uCodeFirst.DataTypes;
using uCodeFirst.Sync;

namespace uCodeFirst.DataTypes.Bases;

/// <summary>Base for property editors backed by Umbraco's "Upload Field" (<c>Umbraco.UploadField</c>) editor.</summary>
public abstract class UploadFieldDataType : DataTypeBase
{
    /// <summary>Allowed file extensions, e.g. "pdf,docx". Empty means any file type is allowed.</summary>
    public virtual string[] FileExtensions { get; } = [];

    /// <inheritdoc/>
    public override EditorRecipe BuildRecipe(Guid key, string name)
    {
        IDictionary<string, object> config = FileExtensions.Length > 0
            ? new Dictionary<string, object> { ["fileExtensions"] = FileExtensions.Select(e => (object)new Dictionary<string, object> { ["value"] = e }).ToList() }
            : new Dictionary<string, object>();

        return new EditorRecipe(key, name, "Umbraco.UploadField", "Umb.PropertyEditorUi.UploadField", config);
    }
}
