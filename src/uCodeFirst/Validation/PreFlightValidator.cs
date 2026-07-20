using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;

namespace uCodeFirst.Validation;

internal sealed class PreFlightValidator
{
    public IReadOnlyList<string> Validate(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        IReadOnlyList<MediaTypeDefinition>? mediaDefinitions = null)
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

        return errors;
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
