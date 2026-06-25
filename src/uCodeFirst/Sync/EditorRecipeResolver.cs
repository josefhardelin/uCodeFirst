using System.Security.Cryptography;
using System.Text;
using uCodeFirst.Attributes;
using Umbraco.Cms.Core.Models;

namespace uCodeFirst.Sync;

internal static class EditorRecipeResolver
{
    private const string KeyNamespace = "consid.codefirst:";

    public static EditorRecipe Resolve(PropertyEditorAttribute attr) => attr switch
    {
        TextStringAttribute ts => TextStringRecipe(ts),
        TextAreaAttribute ta => TextAreaRecipe(ta),
        RichTextAttribute => Simple("Umbraco.RichText", "Umb.PropertyEditorUi.Tiptap", "Code First: Rich Text", ValueStorageType.Ntext),
        NumericAttribute => Simple("Umbraco.Integer", "Umb.PropertyEditorUi.Integer", "Code First: Numeric", ValueStorageType.Integer),
        TrueFalseAttribute => Simple("Umbraco.TrueFalse", "Umb.PropertyEditorUi.Toggle", "Code First: True/False", ValueStorageType.Integer),
        DatePickerAttribute => Simple("Umbraco.DateTime", "Umb.PropertyEditorUi.DatePicker", "Code First: Date Picker", ValueStorageType.Date),
        DropdownAttribute dd => DropdownRecipe(dd),
        BlockListAttribute bl => BlockListRecipe(bl),
        BlockGridAttribute bg => BlockGridRecipe(bg),
        _ => throw new InvalidOperationException($"No recipe registered for attribute type '{attr.GetType().Name}'.")
    };

    private static EditorRecipe Simple(string editorAlias, string uiAlias, string name, ValueStorageType dbType)
        => new(DeriveKey(editorAlias, "default"), name, editorAlias, uiAlias, new Dictionary<string, object>(), dbType);

    private static EditorRecipe TextStringRecipe(TextStringAttribute ts)
    {
        var configFingerprint = ts.MaxLength > 0 ? $"maxChars={ts.MaxLength}" : "default";
        var name = ts.MaxLength > 0 ? $"Code First: Text String (max {ts.MaxLength})" : "Code First: Text String";
        IDictionary<string, object> config = ts.MaxLength > 0
            ? new Dictionary<string, object> { ["maxChars"] = ts.MaxLength }
            : new Dictionary<string, object>();
        return new(DeriveKey("Umbraco.TextBox", configFingerprint), name, "Umbraco.TextBox", "Umb.PropertyEditorUi.TextBox", config);
    }

    private static EditorRecipe TextAreaRecipe(TextAreaAttribute ta)
    {
        var configFingerprint = ta.MaxLength > 0 ? $"maxChars={ta.MaxLength}" : "default";
        var name = ta.MaxLength > 0 ? $"Code First: Text Area (max {ta.MaxLength})" : "Code First: Text Area";
        IDictionary<string, object> config = ta.MaxLength > 0
            ? new Dictionary<string, object> { ["maxChars"] = ta.MaxLength }
            : new Dictionary<string, object>();
        return new(DeriveKey("Umbraco.TextArea", configFingerprint), name, "Umbraco.TextArea", "Umb.PropertyEditorUi.TextArea", config, ValueStorageType.Ntext);
    }

    private static EditorRecipe DropdownRecipe(DropdownAttribute dd)
    {
        var sorted = dd.Options.OrderBy(o => o, StringComparer.Ordinal).ToArray();
        var fingerprint = $"multiple={dd.AllowMultiple},items={string.Join(",", sorted)}";
        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["multiple"] = dd.AllowMultiple,
            ["items"] = dd.Options.ToList()
        };
        return new(DeriveKey("Umbraco.DropDown.Flexible", fingerprint), "Code First: Dropdown", "Umbraco.DropDown.Flexible", "Umb.PropertyEditorUi.Dropdown", config);
    }

    private static EditorRecipe BlockListRecipe(BlockListAttribute bl)
    {
        var elementKeys = ResolveElementTypeKeys(bl.BlockTypes);
        var fingerprint = $"blocklist:{string.Join(",", elementKeys.Select(k => k.ToString()))}";
        var name = $"Code First: Block List ({string.Join(", ", bl.BlockTypes.Select(t => t.Name))})";

        var blocks = elementKeys
            .Select(k => (object)new Dictionary<string, object> { ["contentElementTypeKey"] = k })
            .ToList();

        IDictionary<string, object> config = new Dictionary<string, object> { ["blocks"] = blocks };

        return new(DeriveKey("Umbraco.BlockList", fingerprint), name, "Umbraco.BlockList", "Umb.PropertyEditorUi.BlockList", config, ValueStorageType.Ntext);
    }

    private static EditorRecipe BlockGridRecipe(BlockGridAttribute bg)
    {
        var elementKeys = ResolveElementTypeKeys(bg.BlockTypes);
        var fingerprint = $"blockgrid:cols={bg.GridColumns}:{string.Join(",", elementKeys.Select(k => k.ToString()))}";
        var name = $"Code First: Block Grid ({string.Join(", ", bg.BlockTypes.Select(t => t.Name))})";

        var blocks = elementKeys
            .Select(k => (object)new Dictionary<string, object>
            {
                ["contentElementTypeKey"] = k,
                ["allowAtRoot"] = true,
                ["allowInAreas"] = false,
                ["areas"] = new List<object>()
            })
            .ToList();

        IDictionary<string, object> config = new Dictionary<string, object>
        {
            ["blocks"] = blocks,
            ["gridColumns"] = bg.GridColumns
        };

        return new(DeriveKey("Umbraco.BlockGrid", fingerprint), name, "Umbraco.BlockGrid", "Umb.PropertyEditorUi.BlockGrid", config, ValueStorageType.Ntext);
    }

    private static List<Guid> ResolveElementTypeKeys(Type[] blockTypes) =>
        blockTypes
            .Select(t => (ElementTypeAttribute?)Attribute.GetCustomAttribute(t, typeof(ElementTypeAttribute)))
            .Where(a => a is not null)
            .Select(a => a!.Key)
            .OrderBy(k => k)
            .ToList();

    private static Guid DeriveKey(string editorAlias, string configFingerprint)
    {
        var input = $"{KeyNamespace}{editorAlias}:{configFingerprint}";
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }
}
