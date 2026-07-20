using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;

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
                Color: attr.Color,
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
                Color: attr.Color,
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
                Color: attr.Color,
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
            var dataType = prop.GetCustomAttribute<DataTypeBase>();
            if (dataType is null)
                continue;

            var groupAttr = prop.GetCustomAttribute<GroupAttribute>();

            result.Add(new PropertyDefinition(
                Alias: dataType.Alias ?? ToAlias(prop.Name),
                Name: dataType.Name ?? prop.Name,
                GroupName: groupAttr?.Name ?? Groups.Content,
                SortOrder: groupAttr?.SortOrder ?? 0,
                Mandatory: dataType.Mandatory,
                Description: dataType.Description,
                DataType: dataType));
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

            var dataType = prop.GetCustomAttribute<DataTypeBase>();
            if (dataType is null)
                continue;

            var groupAttr = prop.GetCustomAttribute<GroupAttribute>();

            result.Add(new PropertyDefinition(
                Alias: dataType.Alias ?? ToAlias(prop.Name),
                Name: dataType.Name ?? prop.Name,
                GroupName: groupAttr?.Name ?? Groups.Content,
                SortOrder: groupAttr?.SortOrder ?? 0,
                Mandatory: dataType.Mandatory,
                Description: dataType.Description,
                DataType: dataType));
        }

        return result;
    }

    public IReadOnlyList<MediaTypeDefinition> ScanMediaTypes(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<MediaTypeDefinition>();

        var allTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .ToList();

        foreach (var type in allTypes.Where(t => t is { IsClass: true, IsAbstract: false } && t.IsDefined(typeof(MediaTypeAttribute))))
        {
            var attr = type.GetCustomAttribute<MediaTypeAttribute>()!;
            var alias = attr.Alias ?? ToAlias(type.Name);
            var allowedChildren = type.GetCustomAttribute<AllowedChildrenAttribute>()?.Types ?? Array.Empty<Type>();

            var compositionKeys = attr.Compositions
                .Select(g => System.Guid.Parse(g))
                .ToList();

            definitions.Add(new MediaTypeDefinition(
                ClrType: type,
                Key: attr.Key,
                Alias: alias,
                Name: attr.Name,
                Icon: attr.Icon,
                Color: attr.Color,
                Description: attr.Description,
                AllowedAtRoot: attr.AllowedAtRoot,
                Folder: attr.Folder,
                AllowedChildTypes: allowedChildren,
                Properties: ScanClassProperties(type, []),
                CompositionKeys: compositionKeys));
        }

        return definitions;
    }

    internal static string ToAlias(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
