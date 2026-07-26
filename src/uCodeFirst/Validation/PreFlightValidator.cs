using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;

namespace uCodeFirst.Validation;

internal sealed class PreFlightValidator
{
    public IReadOnlyList<string> Validate(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        IReadOnlyList<MediaTypeDefinition>? mediaDefinitions = null,
        IReadOnlyList<DictionaryItemDefinition>? dictionaryDefinitions = null,
        IReadOnlyList<LanguageSetDefinition>? languageSetDefinitions = null,
        IReadOnlyList<TemplateDefinition>? templateDefinitions = null,
        IReadOnlyList<SeedContentDefinition>? seedContentDefinitions = null)
    {
        var errors = new List<string>();
        var aliasByType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var keyByType = new Dictionary<Guid, Type>();

        foreach (var def in definitions)
        {
            if (aliasByType.TryGetValue(def.Alias, out var conflicting))
                errors.Add($"Duplicate alias '{def.Alias}': '{def.ClrType.FullName}' and '{conflicting.FullName}'.");
            else
                aliasByType[def.Alias] = def.ClrType;

            if (keyByType.TryGetValue(def.Key, out var conflictingKey))
                errors.Add($"Duplicate GUID '{def.Key}': '{def.ClrType.FullName}' and '{conflictingKey.FullName}'.");
            else
                keyByType[def.Key] = def.ClrType;

            var propAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in def.Properties)
            {
                if (!propAliases.Add(prop.Alias))
                    errors.Add($"Duplicate property alias '{prop.Alias}' on '{def.ClrType.FullName}'.");
            }
        }

        var scannedKeys = definitions.Select(d => d.Key).ToHashSet();

        // Validate AllowedChildren references
        foreach (var def in definitions)
        {
            foreach (var childType in def.AllowedChildTypes)
            {
                var childAttr = childType.GetCustomAttribute<DocumentTypeAttribute>();
                if (childAttr is null)
                    errors.Add($"[AllowedChildren] on '{def.ClrType.FullName}' references '{childType.FullName}' which has no [DocumentType] attribute.");
                else if (!scannedKeys.Contains(childAttr.Key))
                    errors.Add($"[AllowedChildren] on '{def.ClrType.FullName}' references '{childType.FullName}' which was not discovered in the scanned assembly set.");
            }
        }

        // Validate culture-varying properties only exist on culture-varying content/element types
        foreach (var def in definitions)
        {
            if (def.VariesByCulture)
                continue;

            foreach (var prop in def.Properties)
            {
                if (prop.VariesByCulture)
                    errors.Add($"Property '{prop.Alias}' on '{def.ClrType.FullName}' has VariesByCulture: true, but '{def.ClrType.FullName}' has VariesByCulture: false.");
            }
        }

        // Validate block types in [BlockListDataType] and [BlockGridDataType] properties
        foreach (var def in definitions)
        {
            foreach (var prop in def.Properties)
            {
                BlockDefinition[]? blocks = prop.DataType switch
                {
                    BlockListDataType bl => bl.Blocks,
                    BlockGridDataType bg => bg.Blocks,
                    _ => null
                };

                if (blocks is null)
                    continue;

                foreach (var block in blocks)
                {
                    foreach (var blockType in new[] { block.ContentType, block.SettingsType }.OfType<Type>())
                    {
                        var elemAttr = blockType.GetCustomAttribute<ElementTypeAttribute>();
                        if (elemAttr is null)
                            errors.Add($"[{prop.DataType.GetType().Name}] on '{def.ClrType.FullName}.{prop.Alias}' references '{blockType.FullName}' which has no [ElementType] attribute.");
                        else if (!scannedKeys.Contains(elemAttr.Key))
                            errors.Add($"[{prop.DataType.GetType().Name}] on '{def.ClrType.FullName}.{prop.Alias}' references '{blockType.FullName}' which was not discovered in the scanned assembly set.");
                    }
                }
            }
        }

        // Validate document-type references in [ContentPicker]/[MultiNodeTreePicker]-backed properties:
        // AllowedContentTypes filters on both, plus DynamicRoot query steps on MultiNodeTreePicker.
        foreach (var def in definitions)
        {
            foreach (var prop in def.Properties)
            {
                var referencedTypes = new List<Type>();

                switch (prop.DataType)
                {
                    case ContentPickerDataType cp:
                        referencedTypes.AddRange(cp.AllowedContentTypes);
                        break;
                    case MultiNodeTreePickerDataType mntp:
                        referencedTypes.AddRange(mntp.AllowedContentTypes);
                        if (mntp.DynamicRoot is not null)
                        {
                            foreach (var step in mntp.DynamicRoot.QuerySteps)
                                referencedTypes.AddRange(step.DocumentTypes);
                        }
                        break;
                }

                foreach (var referencedType in referencedTypes)
                {
                    var docTypeAttr = referencedType.GetCustomAttribute<DocumentTypeAttribute>();
                    if (docTypeAttr is null)
                        errors.Add($"[{prop.DataType.GetType().Name}] on '{def.ClrType.FullName}.{prop.Alias}' references '{referencedType.FullName}' which has no [DocumentType] attribute.");
                    else if (!scannedKeys.Contains(docTypeAttr.Key))
                        errors.Add($"[{prop.DataType.GetType().Name}] on '{def.ClrType.FullName}.{prop.Alias}' references '{referencedType.FullName}' which was not discovered in the scanned assembly set.");
                }
            }
        }

        // Validate composition keys
        foreach (var def in definitions)
        {
            foreach (var compKey in def.CompositionKeys)
            {
                if (!scannedKeys.Contains(compKey))
                {
                    var compInterface = def.ClrType.GetInterfaces()
                        .FirstOrDefault(i =>
                        {
                            var a = i.GetCustomAttribute<CompositionTypeAttribute>();
                            return a?.Key == compKey;
                        });
                    errors.Add($"'{def.ClrType.FullName}' uses composition '{compInterface?.FullName ?? compKey.ToString()}' which was not discovered in the scanned assembly set.");
                }
            }
        }

        if (mediaDefinitions is { Count: > 0 })
            ValidateMediaTypes(mediaDefinitions, errors);

        if (dictionaryDefinitions is { Count: > 0 })
            ValidateDictionaryItems(dictionaryDefinitions, errors);

        if (languageSetDefinitions is { Count: > 0 })
            ValidateLanguages(languageSetDefinitions, errors);

        if (templateDefinitions is { Count: > 0 })
            ValidateTemplates(templateDefinitions, errors);

        if (seedContentDefinitions is { Count: > 0 })
            ValidateSeedContent(seedContentDefinitions, scannedKeys, errors);

        return errors;
    }

    // Only one enum may carry [Languages]. Fallback/DefaultLanguage cross-references are boxed
    // `object` on the attributes (an enum type can't otherwise reference "itself" as a parameter
    // type), so they're validated here against the declaring enum rather than by the compiler.
    private static void ValidateLanguages(IReadOnlyList<LanguageSetDefinition> definitions, List<string> errors)
    {
        if (definitions.Count > 1)
        {
            var names = string.Join(", ", definitions.Select(d => d.EnumType.FullName));
            errors.Add($"[Languages] is declared on {definitions.Count} enums ({names}); only one is allowed.");
            return;
        }

        var def = definitions[0];

        if (def.DefaultMember is null)
        {
            var raw = def.EnumType.GetCustomAttribute<LanguagesAttribute>()!.DefaultLanguage;
            errors.Add($"[Languages] on '{def.EnumType.FullName}' has DefaultLanguage '{raw}' which is not a member of that enum.");
        }
        else if (!def.DefaultMember.IsDefined(typeof(LanguageAttribute)))
        {
            errors.Add($"[Languages] on '{def.EnumType.FullName}' has DefaultLanguage '{def.DefaultMember.Name}' which has no [Language] attribute.");
        }

        var memberByIsoCode = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var lang in def.Languages)
        {
            if (memberByIsoCode.TryGetValue(lang.IsoCode, out var conflicting))
                errors.Add($"Duplicate language ISO code '{lang.IsoCode}': '{def.EnumType.FullName}.{lang.Member.Name}' and '{def.EnumType.FullName}.{conflicting.Name}'.");
            else
                memberByIsoCode[lang.IsoCode] = lang.Member;

            var rawFallback = lang.Member.GetCustomAttribute<LanguageAttribute>()!.Fallback;
            if (rawFallback is not null && lang.Fallback is null)
                errors.Add($"[Language] on '{def.EnumType.FullName}.{lang.Member.Name}' has Fallback '{rawFallback}' which is not a member of the same enum.");
            else if (lang.Fallback is not null && !lang.Fallback.IsDefined(typeof(LanguageAttribute)))
                errors.Add($"[Language] on '{def.EnumType.FullName}.{lang.Member.Name}' has Fallback '{lang.Fallback.Name}' which has no [Language] attribute.");
        }

        var languageByMember = def.Languages.ToDictionary(l => l.Member);
        foreach (var lang in def.Languages)
        {
            var visited = new HashSet<FieldInfo> { lang.Member };
            var current = lang.Fallback;

            while (current is not null && languageByMember.TryGetValue(current, out var currentLang))
            {
                if (!visited.Add(current))
                {
                    errors.Add($"[Language] fallback chain starting at '{def.EnumType.FullName}.{lang.Member.Name}' contains a cycle.");
                    break;
                }

                current = currentLang.Fallback;
            }
        }
    }

    // Unlike [Languages], any number of enums may carry [Template]-decorated members, so there's
    // no "only one enum allowed" check here. Master cross-references are boxed `object` on the
    // attribute (an enum type can't otherwise reference "itself" as a parameter type), so they're
    // validated here against the declaring enum rather than by the compiler.
    private static void ValidateTemplates(IReadOnlyList<TemplateDefinition> definitions, List<string> errors)
    {
        var memberByAlias = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in definitions)
        {
            if (memberByAlias.TryGetValue(def.Alias, out var conflicting))
                errors.Add($"Duplicate template alias '{def.Alias}': '{def.Member.DeclaringType?.FullName}.{def.Member.Name}' and '{conflicting.DeclaringType?.FullName}.{conflicting.Name}'.");
            else
                memberByAlias[def.Alias] = def.Member;

            var rawMaster = def.Member.GetCustomAttribute<TemplateAttribute>()!.Master;
            if (rawMaster is not null && def.Master is null)
                errors.Add($"[Template] on '{def.Member.DeclaringType?.FullName}.{def.Member.Name}' has Master '{rawMaster}' which is not a member of the same enum.");
            else if (def.Master is not null && !def.Master.IsDefined(typeof(TemplateAttribute)))
                errors.Add($"[Template] on '{def.Member.DeclaringType?.FullName}.{def.Member.Name}' has Master '{def.Master.Name}' which has no [Template] attribute.");
        }

        var templateByMember = definitions.ToDictionary(d => d.Member);
        foreach (var def in definitions)
        {
            var visited = new HashSet<FieldInfo> { def.Member };
            var current = def.Master;

            while (current is not null && templateByMember.TryGetValue(current, out var currentDef))
            {
                if (!visited.Add(current))
                {
                    errors.Add($"[Template] master chain starting at '{def.Member.DeclaringType?.FullName}.{def.Member.Name}' contains a cycle.");
                    break;
                }

                current = currentDef.Master;
            }
        }
    }

    // Every node in Umbraco's dictionary tree — leaves and auto-created parents alike — shares one
    // flat ItemKey namespace, since lookups (GetDictionaryValue) are by key text with no path. Two
    // different fields or container classes producing the same key text would collide in Umbraco.
    private static void ValidateDictionaryItems(IReadOnlyList<DictionaryItemDefinition> definitions, List<string> errors)
    {
        var ownerByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var seenContainers = new HashSet<Type>();

        void Claim(string itemKey, string origin)
        {
            if (ownerByKey.TryGetValue(itemKey, out var existingOrigin))
                errors.Add($"Duplicate dictionary item key '{itemKey}': {origin} and {existingOrigin}.");
            else
                ownerByKey[itemKey] = origin;
        }

        foreach (var def in definitions)
        {
            foreach (var container in def.ParentChain)
            {
                if (seenContainers.Add(container))
                    Claim(container.Name, $"container class '{container.FullName}'");
            }

            Claim(def.ItemKey, $"field '{def.Field.DeclaringType?.FullName}.{def.Field.Name}'");
        }
    }

    // DocumentType/Parent cross-references are raw CLR Types (see SeedContentDefinition), so — like
    // [AllowedChildren] — they're validated here via reflection rather than by the compiler. Parent
    // chain cycle detection mirrors [Template]'s Master-chain check above.
    private static void ValidateSeedContent(IReadOnlyList<SeedContentDefinition> definitions, HashSet<Guid> scannedDocumentTypeKeys, List<string> errors)
    {
        var keyByType = new Dictionary<Guid, Type>();
        var defByType = definitions.ToDictionary(d => d.ClrType);

        foreach (var def in definitions)
        {
            if (keyByType.TryGetValue(def.Key, out var conflicting))
                errors.Add($"Duplicate seed content GUID '{def.Key}': '{def.ClrType.FullName}' and '{conflicting.FullName}'.");
            else
                keyByType[def.Key] = def.ClrType;

            var docTypeAttr = def.DocumentType.GetCustomAttribute<DocumentTypeAttribute>();
            if (docTypeAttr is null)
                errors.Add($"[SeedContent] on '{def.ClrType.FullName}' references '{def.DocumentType.FullName}' which has no [DocumentType] attribute.");
            else if (!scannedDocumentTypeKeys.Contains(docTypeAttr.Key))
                errors.Add($"[SeedContent] on '{def.ClrType.FullName}' references '{def.DocumentType.FullName}' which was not discovered in the scanned assembly set.");

            if (def.Parent is not null && !defByType.ContainsKey(def.Parent))
                errors.Add($"[SeedContent] on '{def.ClrType.FullName}' has Parent '{def.Parent.FullName}' which has no [SeedContent] attribute.");
        }

        foreach (var def in definitions)
        {
            var visited = new HashSet<Type> { def.ClrType };
            var current = def.Parent;

            while (current is not null && defByType.TryGetValue(current, out var currentDef))
            {
                if (!visited.Add(current))
                {
                    errors.Add($"[SeedContent] Parent chain starting at '{def.ClrType.FullName}' contains a cycle.");
                    break;
                }

                current = currentDef.Parent;
            }
        }
    }

    private static void ValidateMediaTypes(IReadOnlyList<MediaTypeDefinition> definitions, List<string> errors)
    {
        var aliasByType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var keyByType = new Dictionary<Guid, Type>();

        foreach (var def in definitions)
        {
            if (aliasByType.TryGetValue(def.Alias, out var conflicting))
                errors.Add($"Duplicate media type alias '{def.Alias}': '{def.ClrType.FullName}' and '{conflicting.FullName}'.");
            else
                aliasByType[def.Alias] = def.ClrType;

            if (keyByType.TryGetValue(def.Key, out var conflictingKey))
                errors.Add($"Duplicate media type GUID '{def.Key}': '{def.ClrType.FullName}' and '{conflictingKey.FullName}'.");
            else
                keyByType[def.Key] = def.ClrType;

            var propAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in def.Properties)
            {
                if (!propAliases.Add(prop.Alias))
                    errors.Add($"Duplicate property alias '{prop.Alias}' on media type '{def.ClrType.FullName}'.");
            }
        }

        var scannedKeys = definitions.Select(d => d.Key).ToHashSet();

        foreach (var def in definitions)
        {
            foreach (var childType in def.AllowedChildTypes)
            {
                var childAttr = childType.GetCustomAttribute<MediaTypeAttribute>();
                if (childAttr is null)
                    errors.Add($"[AllowedChildren] on media type '{def.ClrType.FullName}' references '{childType.FullName}' which has no [MediaType] attribute.");
                else if (!scannedKeys.Contains(childAttr.Key))
                    errors.Add($"[AllowedChildren] on media type '{def.ClrType.FullName}' references '{childType.FullName}' which was not discovered in the scanned assembly set.");
            }

            if (def.ParentKey is not null && def.Folder is not null)
                errors.Add($"Media type '{def.ClrType.FullName}' declares both a parent (via inheritance from '{def.ClrType.BaseType?.FullName}') and a Folder ('{def.Folder}') — a media type can only have one.");
        }
    }
}
