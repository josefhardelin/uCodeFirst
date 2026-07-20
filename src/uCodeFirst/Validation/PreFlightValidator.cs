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
        IReadOnlyList<LanguageSetDefinition>? languageSetDefinitions = null)
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
        }
    }
}
