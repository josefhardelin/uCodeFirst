using System.Reflection;
using Consid.Umbraco.CodeFirst.Attributes;
using Consid.Umbraco.CodeFirst.Discovery;

namespace Consid.Umbraco.CodeFirst.Validation;

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
                errors.Add($"Duplicate document type alias '{def.Alias}': '{def.ClrType.FullName}' and '{conflicting.FullName}'.");
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

        return errors;
    }
}
