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
                CompositionKeys: Array.Empty<Guid>(),
                VariesByCulture: false,
                IsContainer: false));
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
                CompositionKeys: compositionKeys,
                VariesByCulture: attr.VariesByCulture,
                IsContainer: attr.IsContainer,
                PreventCleanup: attr.PreventCleanup,
                KeepAllVersionsNewerThanDays: attr.KeepAllVersionsNewerThanDays,
                KeepLatestVersionPerDayForDays: attr.KeepLatestVersionPerDayForDays));
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
                CompositionKeys: compositionKeys,
                VariesByCulture: attr.VariesByCulture,
                // Element types are used as Block List/Grid items, never as tree nodes with children,
                // so [ElementType]'s IsContainer (kept for API symmetry) is intentionally ignored here.
                IsContainer: false));
        }

        return definitions;
    }

    // A [MediaType] class may only inherit from PublishedContentModel or from a library-provided
    // External stub (e.g. the built-in Image type) — never from another regular code-first media
    // type. This keeps the whole parent graph resolvable in a single sync pass, since every valid
    // parent already exists in Umbraco before sync ever runs.
    private static Guid? GetMediaTypeParentKey(Type type)
    {
        var baseAttr = type.BaseType?.GetCustomAttribute<MediaTypeAttribute>();
        if (baseAttr is null)
            return null;

        if (!baseAttr.External)
            throw new InvalidOperationException(
                $"'{type.FullName}' inherits from '{type.BaseType!.FullName}', but that class is not marked External on [MediaType]. " +
                "MediaType classes may only inherit from PublishedContentModel or a library-provided built-in base type marked External = true.");

        return baseAttr.Key;
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
                DataType: dataType,
                VariesByCulture: dataType.VariesByCulture));
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
                DataType: dataType,
                VariesByCulture: dataType.VariesByCulture));
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
            if (attr.External)
                continue; // stub for a type that already exists in Umbraco — never synced

            var alias = attr.Alias ?? ToAlias(type.Name);
            var allowedChildren = type.GetCustomAttribute<AllowedChildrenAttribute>()?.Types ?? Array.Empty<Type>();

            var compositionKeys = attr.Compositions
                .Select(g => System.Guid.Parse(g))
                .ToList();

            var parentKey = GetMediaTypeParentKey(type);

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
                CompositionKeys: compositionKeys,
                ParentKey: parentKey,
                IsContainer: attr.IsContainer));
        }

        return definitions;
    }

    public IReadOnlyList<DictionaryItemDefinition> ScanDictionaryItems(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<DictionaryItemDefinition>();

        var allTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .ToList();

        foreach (var type in allTypes.Where(t => t.IsClass))
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (!field.IsLiteral || field.FieldType != typeof(string))
                    continue;

                var attribute = field.GetCustomAttribute<DictionaryItemAttribute>();
                if (attribute is null)
                    continue;

                var itemKey = attribute.Alias ?? (string)field.GetRawConstantValue()!;
                definitions.Add(new DictionaryItemDefinition(field, itemKey, GetParentChain(type)));
            }
        }

        return definitions;
    }

    // Nested static classes become real parent dictionary items; the outermost (non-nested)
    // declaring type is treated as pure C#-side grouping and never becomes an item itself. A
    // [DictionaryItem(Alias = "...")] on the class overrides its key (needed for keys a C#
    // identifier can't spell, e.g. containing spaces); otherwise the class name is used verbatim.
    private static IReadOnlyList<string> GetParentChain(Type declaringType)
    {
        var chain = new List<string>();
        var current = declaringType;

        while (current.IsNested)
        {
            var alias = current.GetCustomAttribute<DictionaryItemAttribute>()?.Alias ?? current.Name;
            chain.Insert(0, alias);
            current = current.DeclaringType!;
        }

        return chain;
    }

    public IReadOnlyList<LanguageSetDefinition> ScanLanguages(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<LanguageSetDefinition>();

        var allTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .ToList();

        foreach (var enumType in allTypes.Where(t => t.IsEnum && t.IsDefined(typeof(LanguagesAttribute))))
        {
            var setAttr = enumType.GetCustomAttribute<LanguagesAttribute>()!;
            var defaultMember = ResolveEnumMember(enumType, setAttr.DefaultLanguage);

            var languages = new List<LanguageDefinition>();
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var langAttr = field.GetCustomAttribute<LanguageAttribute>();
                if (langAttr is null)
                    continue;

                var fallback = ResolveEnumMember(enumType, langAttr.Fallback);
                languages.Add(new LanguageDefinition(field, langAttr.IsoCode, fallback, langAttr.IsMandatory));
            }

            definitions.Add(new LanguageSetDefinition(enumType, defaultMember, languages));
        }

        return definitions;
    }

    // Unlike ScanLanguages, no enum-level marker attribute is required — [Template] isn't as
    // singular a concept as "the language set", so any number of enums may carry
    // [Template]-decorated members alongside unrelated values.
    public IReadOnlyList<TemplateDefinition> ScanTemplates(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<TemplateDefinition>();

        var allTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .ToList();

        foreach (var enumType in allTypes.Where(t => t.IsEnum))
        {
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var templateAttr = field.GetCustomAttribute<TemplateAttribute>();
                if (templateAttr is null)
                    continue;

                var master = ResolveEnumMember(enumType, templateAttr.Master);
                definitions.Add(new TemplateDefinition(field, templateAttr.Alias, master));
            }
        }

        return definitions;
    }

    public IReadOnlyList<SeedContentDefinition> ScanSeedContent(IEnumerable<Assembly> assemblies)
    {
        var definitions = new List<SeedContentDefinition>();

        var allTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .ToList();

        foreach (var type in allTypes.Where(t => t is { IsClass: true, IsAbstract: false } && t.IsDefined(typeof(SeedContentAttribute))))
        {
            var attr = type.GetCustomAttribute<SeedContentAttribute>()!;

            definitions.Add(new SeedContentDefinition(
                ClrType: type,
                Key: attr.Key,
                DocumentType: attr.DocumentType,
                Name: attr.Name,
                Parent: attr.Parent));
        }

        return definitions;
    }

    private static FieldInfo? ResolveEnumMember(Type enumType, object? boxedValue)
    {
        if (boxedValue is null || boxedValue.GetType() != enumType)
            return null;

        var targetValue = Convert.ToInt64(boxedValue);
        return enumType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(f => Convert.ToInt64(f.GetRawConstantValue()) == targetValue);
    }

    internal static string ToAlias(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
