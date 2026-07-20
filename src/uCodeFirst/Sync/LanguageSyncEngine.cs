using System.Globalization;
using System.Reflection;
using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst.Sync;

// Code owns which languages exist and their default/mandatory/fallback config at creation time
// only. Additive-only, like the dictionary item engine: an already-existing language (including
// the built-in default from installation) is never updated, only ensured to exist.
internal sealed class LanguageSyncEngine
{
    private readonly ILanguageService _languageService;
    private readonly ILogger<LanguageSyncEngine> _logger;

    public LanguageSyncEngine(ILanguageService languageService, ILogger<LanguageSyncEngine> logger)
    {
        _languageService = languageService;
        _logger = logger;
    }

    public async Task SyncAsync(LanguageSetDefinition definition, CancellationToken ct = default)
    {
        var byMember = definition.Languages.ToDictionary(l => l.Member);
        var visited = new HashSet<FieldInfo>();

        foreach (var lang in definition.Languages)
            await EnsureLanguageAsync(lang, definition, byMember, visited, ct);
    }

    private async Task EnsureLanguageAsync(
        LanguageDefinition lang,
        LanguageSetDefinition definition,
        IReadOnlyDictionary<FieldInfo, LanguageDefinition> byMember,
        HashSet<FieldInfo> visited,
        CancellationToken ct)
    {
        if (!visited.Add(lang.Member))
            return;

        LanguageDefinition? fallbackLang = null;
        if (lang.Fallback is not null && byMember.TryGetValue(lang.Fallback, out var fb))
        {
            fallbackLang = fb;
            await EnsureLanguageAsync(fb, definition, byMember, visited, ct);
        }

        var existing = await _languageService.GetAsync(lang.IsoCode);
        if (existing is not null)
            return;

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(lang.IsoCode);
        }
        catch (CultureNotFoundException ex)
        {
            _logger.LogError(ex, "Failed to create language '{IsoCode}': not a valid culture.", lang.IsoCode);
            return;
        }

        var language = new Language(lang.IsoCode, culture.DisplayName)
        {
            IsDefault = lang.Member.Equals(definition.DefaultMember),
            IsMandatory = lang.IsMandatory,
            FallbackIsoCode = fallbackLang?.IsoCode,
        };

        var result = await _languageService.CreateAsync(language, Constants.Security.SuperUserKey);

        if (!result.Success)
        {
            _logger.LogError("Failed to create language '{IsoCode}': {Status}.", lang.IsoCode, result.Status);
            return;
        }

        _logger.LogInformation("Created language '{IsoCode}'.", lang.IsoCode);
    }
}
