using System.Reflection;
using uCodeFirst.Attributes;

namespace uCodeFirst.Discovery;

internal sealed class DocumentTypeScanner
{
    public IReadOnlyList<DocumentTypeDefinition> Scan(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<DocumentTypeDefinition>();

        var allTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .ToList();

        // Composition types from interfaces
        foreach (var iface in allTypes.Where(t => t.IsInterface && t.IsDefined(typeof(CompositionTypeAttribute))))
        {
            var attr = iface.GetCustomAttribute<CompositionTypeAttribute>()!;
            var alias = attr.Alias ?? ToAlias(iface.Name.TrimStart('I'));

            definitions.Add(new DocumentTypeDefinition(
                ClrType: iface,
                IsElement: false,
                Key: attr.Key,
                Alias: alias,
                Name: attr.Name,
                Icon: attr.Icon,
                Description: attr.Description,
                AllowedAtRoot: false,
                Folder: attr.Folder,
                DefaultTemplate: null,
                AllowedChildTypes: Array.Empty<Type>(),
                Properties: ScanInterfaceProperties(iface),
                CompositionKeys: Array.Empty<Guid>()));
        }

        // Document types from classes
        foreach (var type in allTypes.Where(t => t is { IsClass: true, IsAbstract: false } && t.IsDefined(typeof(DocumentTypeAttribute))))
        {
            var attr = type.GetCustomAttribute<DocumentTypeAttribute>()!;
            var alias = attr.Alias ?? ToAlias(type.Name);
            var allowedChildren = type.GetCustomAttribute<AllowedChildrenAttribute>()?.Types ?? Array.Empty<Type>();
            var compositionKeys = GetCompositionKeys(type);
            var compositionPropNames = GetCompositionPropertyNames(type);

            definitions.Add(new DocumentTypeDefinition(
                ClrType: type,
                IsElement: false,
                Key: attr.Key,
                Alias: alias,
                Name: attr.Name,
                Icon: attr.Icon,
                Description: attr.Description,
                AllowedAtRoot: attr.AllowedAtRoot,
                Folder: attr.Folder,
                DefaultTemplate: attr.DefaultTemplate,
                AllowedChildTypes: allowedChildren,
                Properties: ScanClassProperties(type, compositionPropNames),
                CompositionKeys: compositionKeys));
        }

        // Element types from classes
        foreach (var type in allTypes.Where(t => t is { IsClass: true, IsAbstract: false } && t.IsDefined(typeof(ElementTypeAttribute))))
        {
            var attr = type.GetCustomAttribute<ElementTypeAttribute>()!;
            var alias = attr.Alias ?? ToAlias(type.Name);
            var compositionKeys = GetCompositionKeys(type);
            var compositionPropNames = GetCompositionPropertyNames(type);

            definitions.Add(new DocumentTypeDefinition(
                ClrType: type,
                IsElement: true,
                Key: attr.Key,
                Alias: alias,
                Name: attr.Name,
                Icon: attr.Icon,
                Description: attr.Description,
                AllowedAtRoot: false,
                Folder: attr.Folder,
                DefaultTemplate: null,
                AllowedChildTypes: Array.Empty<Type>(),
                Properties: ScanClassProperties(type, compositionPropNames),
                CompositionKeys: compositionKeys));
        }

        return definitions;
    }

    private static IReadOnlyList<Guid> GetCompositionKeys(Type type) =>
        type.GetInterfaces()
            .Select(i => i.GetCustomAttribute<CompositionTypeAttribute>())
            .Where(a => a is not null)
            .Select(a => a!.Key)
            .ToList();

    private static HashSet<string> GetCompositionPropertyNames(Type type) =>
        new(
            type.GetInterfaces()
                .Where(i => i.IsDefined(typeof(CompositionTypeAttribute)))
                .SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Select(p => p.Name),
            StringComparer.Ordinal);

    private static IReadOnlyList<PropertyDefinition> ScanInterfaceProperties(Type iface)
    {
        var result = new List<PropertyDefinition>();

        foreach (var prop in iface.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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

    private static IReadOnlyList<PropertyDefinition> ScanClassProperties(Type type, HashSet<string> excludeNames)
    {
        var result = new List<PropertyDefinition>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (excludeNames.Contains(prop.Name))
                continue;

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
