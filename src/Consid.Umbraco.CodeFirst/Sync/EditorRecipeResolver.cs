using System.Security.Cryptography;
using System.Text;
using Consid.Umbraco.CodeFirst.Attributes;
using Umbraco.Cms.Core.Models;

namespace Consid.Umbraco.CodeFirst.Sync;

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

    private static Guid DeriveKey(string editorAlias, string configFingerprint)
    {
        var input = $"{KeyNamespace}{editorAlias}:{configFingerprint}";
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }
}
