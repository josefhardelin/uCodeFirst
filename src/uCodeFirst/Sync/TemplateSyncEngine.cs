using System.Reflection;
using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst.Sync;

// Code owns which [Template]-registered templates exist and their master/parent relationship at
// creation time only. Additive-only, like the language and dictionary item engines: an
// already-existing template is never rewritten, only ensured to exist — hand-authored view
// content is never overwritten by a later sync.
//
// Umbraco 17 has no direct API to set a template's master — ITemplate.MasterTemplateAlias is
// get-only, and the one settable member (ITemplate.SetMasterTemplate) is a no-op in practice
// because TemplateService re-derives the master from the view content's `Layout = "...";` line
// every time a template is saved (see ITemplateContentParserService). So the master is wired in
// by seeding the content of newly-created templates with that Layout directive.
//
// Runs before ContentTypeSyncEngine so that every [Template]-registered alias — including ones
// that aren't any document type's DefaultTemplate (e.g. a shared "_layout") — exists with its
// master wired up before content types reference it by alias.
internal sealed class TemplateSyncEngine
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplateSyncEngine> _logger;

    public TemplateSyncEngine(ITemplateService templateService, ILogger<TemplateSyncEngine> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    public async Task SyncAsync(IReadOnlyList<TemplateDefinition> definitions, CancellationToken ct = default)
    {
        var byMember = definitions.ToDictionary(t => t.Member);
        var visited = new HashSet<FieldInfo>();

        foreach (var def in definitions)
            await EnsureTemplateAsync(def, byMember, visited, ct);
    }

    private async Task EnsureTemplateAsync(
        TemplateDefinition def,
        IReadOnlyDictionary<FieldInfo, TemplateDefinition> byMember,
        HashSet<FieldInfo> visited,
        CancellationToken ct)
    {
        if (!visited.Add(def.Member))
            return;

        TemplateDefinition? masterDef = null;
        if (def.Master is not null && byMember.TryGetValue(def.Master, out var master))
        {
            masterDef = master;
            await EnsureTemplateAsync(master, byMember, visited, ct);
        }

        var existing = await _templateService.GetAsync(def.Alias);
        if (existing is not null)
        {
            if (masterDef is not null &&
                !string.Equals(existing.MasterTemplateAlias, masterDef.Alias, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Template '{Alias}' already exists without master '{Master}' — leaving as-is; " +
                    "code-first only sets a template's master at creation.",
                    def.Alias, masterDef.Alias);
            }

            return;
        }

        var content = masterDef is not null
            ? $"@{{\n\tLayout = \"{masterDef.Alias}.cshtml\";\n}}"
            : null;

        var result = await _templateService.CreateAsync(def.Alias, def.Alias, content, Constants.Security.SuperUserKey);

        if (!result.Success)
        {
            _logger.LogError("Failed to create template '{Alias}': {Status}.", def.Alias, result.Status);
            return;
        }

        _logger.LogInformation("Created template '{Alias}'.", def.Alias);
    }
}
