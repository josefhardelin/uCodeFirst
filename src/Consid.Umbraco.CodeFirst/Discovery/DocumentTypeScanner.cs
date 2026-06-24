using System.Reflection;
using Consid.Umbraco.CodeFirst.Attributes;

namespace Consid.Umbraco.CodeFirst.Discovery;

internal sealed class DocumentTypeScanner
{
    public IReadOnlyList<DocumentTypeDefinition> Scan(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<DocumentTypeDefinition>();

        var types = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsDefined(typeof(DocumentTypeAttribute)));

        foreach (var type in types)
        {
            var docTypeAttr = type.GetCustomAttribute<DocumentTypeAttribute>()!;
            var alias = docTypeAttr.Alias ?? ToAlias(type.Name);
            var allowedChildren = type.GetCustomAttribute<AllowedChildrenAttribute>()?.Types
                ?? Array.Empty<Type>();

            definitions.Add(new DocumentTypeDefinition(
                ClrType: type,
                Key: docTypeAttr.Key,
                Alias: alias,
                Name: docTypeAttr.Name,
                Icon: docTypeAttr.Icon,
                Description: docTypeAttr.Description,
                AllowedAtRoot: docTypeAttr.AllowedAtRoot,
                AllowedChildTypes: allowedChildren,
                Properties: ScanProperties(type)));
        }

        return definitions;
    }

    private static IReadOnlyList<PropertyDefinition> ScanProperties(Type type)
    {
        var result = new List<PropertyDefinition>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var editorAttr = prop.GetCustomAttribute<PropertyEditorAttribute>();
            if (editorAttr is null)
                continue;

            var groupAttr = prop.GetCustomAttribute<GroupAttribute>();

            result.Add(new PropertyDefinition(
                Alias: editorAttr.Alias ?? ToAlias(prop.Name),
                Name: editorAttr.Name ?? prop.Name,
                GroupName: groupAttr?.Name ?? Groups.Content,
                SortOrder: groupAttr?.SortOrder ?? 0,
                Mandatory: editorAttr.Mandatory,
                Description: editorAttr.Description,
                EditorAttribute: editorAttr));
        }

        return result;
    }

    internal static string ToAlias(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
