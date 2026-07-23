using System.Reflection;
using System.Text.RegularExpressions;
using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst.Sync;

// Code owns which [Template]-registered templates exist and their master/parent relationship.
// Like the language engine, an existing template's master is kept in sync on every run — but
// unlike view content in general, which is never touched, this is scoped to rewriting only the
// single `Layout = "...";` line itself, never the rest of a template's hand-authored content.
//
// Umbraco 17 has no direct API to set a template's master — ITemplate.MasterTemplateAlias is
// get-only, and the one settable member (ITemplate.SetMasterTemplate) is a no-op in practice
// because TemplateService re-derives the master from the view content's `Layout = "...";` line
// every time a template is saved (see ITemplateContentParserService). So the master is wired in
// by seeding/rewriting that Layout directive in the template's content.
//
// If the existing Layout directive can't be unambiguously located (zero matches while a master
// already applies per Umbraco, or more than one match), the template is left untouched and a
// warning is logged — better to leave a stale master than risk corrupting hand-authored content.
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
            await UpdateMasterIfChangedAsync(existing, masterDef, ct);
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

    // Matches a single, whole `Layout = "...";` line (as seeded by EnsureTemplateAsync above),
    // including its trailing newline, so it can be swapped/removed without disturbing anything
    // else in the file.
    private static readonly Regex LayoutLineRegex = new(
        """^[ \t]*Layout\s*=\s*"[^"]*"\s*;[ \t]*\r?\n?""",
        RegexOptions.Multiline);

    private async Task UpdateMasterIfChangedAsync(ITemplate existing, TemplateDefinition? masterDef, CancellationToken ct)
    {
        var previousAlias = existing.MasterTemplateAlias;
        var targetAlias = masterDef?.Alias;

        if (string.Equals(previousAlias, targetAlias, StringComparison.OrdinalIgnoreCase))
            return;

        var content = existing.Content ?? string.Empty;
        var matches = LayoutLineRegex.Matches(content);

        string newContent;
        if (matches.Count == 1)
        {
            newContent = targetAlias is not null
                ? LayoutLineRegex.Replace(content, $"Layout = \"{targetAlias}.cshtml\";\r\n")
                : LayoutLineRegex.Replace(content, string.Empty);
        }
        else if (matches.Count == 0 && previousAlias is null && targetAlias is not null)
        {
            // No existing Layout directive at all — safe to add one without touching anything else.
            newContent = $"Layout = \"{targetAlias}.cshtml\";\r\n" + content;
        }
        else
        {
            _logger.LogWarning(
                "Template '{Alias}' master differs from its code-first definition ('{Previous}' vs '{Target}') " +
                "but its Layout directive could not be unambiguously located in the view content — leaving as-is " +
                "to avoid corrupting hand-authored content.",
                existing.Alias, previousAlias ?? "(none)", targetAlias ?? "(none)");
            return;
        }

        existing.Content = newContent;
        var result = await _templateService.UpdateAsync(existing, Constants.Security.SuperUserKey);

        if (!result.Success)
        {
            _logger.LogError("Failed to update template '{Alias}' master: {Status}.", existing.Alias, result.Status);
            return;
        }

        _logger.LogInformation(
            "Updated template '{Alias}' master: '{Previous}' -> '{Current}'.",
            existing.Alias, previousAlias ?? "(none)", targetAlias ?? "(none)");
    }
}
