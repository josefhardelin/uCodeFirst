using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Discovery;

namespace uCodeFirst.Validation;

internal sealed class PreFlightValidator
{
    public IReadOnlyList<string> Validate(IReadOnlyList<DocumentTypeDefinition> definitions)
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

        // Validate block types in [BlockList] and [BlockGrid] properties
        foreach (var def in definitions)
        {
            foreach (var prop in def.Properties)
            {
                Type[]? blockTypes = prop.EditorAttribute switch
                {
                    BlockListAttribute bl => bl.BlockTypes,
                    BlockGridAttribute bg => bg.BlockTypes,
                    _ => null
                };

                if (blockTypes is null)
                    continue;

                foreach (var blockType in blockTypes)
                {
                    var elemAttr = blockType.GetCustomAttribute<ElementTypeAttribute>();
                    if (elemAttr is null)
                        errors.Add($"[{prop.EditorAttribute.GetType().Name}] on '{def.ClrType.FullName}.{prop.Name}' references '{blockType.FullName}' which has no [ElementType] attribute.");
                    else if (!scannedKeys.Contains(elemAttr.Key))
                        errors.Add($"[{prop.EditorAttribute.GetType().Name}] on '{def.ClrType.FullName}.{prop.Name}' references '{blockType.FullName}' which was not discovered in the scanned assembly set.");
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

        return errors;
    }
}
