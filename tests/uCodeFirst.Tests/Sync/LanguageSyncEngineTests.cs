using Microsoft.Extensions.Logging.Abstractions;
using uCodeFirst.Attributes;
using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace uCodeFirst.Tests.Sync;

[TestFixture]
public class LanguageSyncEngineTests
{
    [Languages(DefaultLanguage: Lang.English)]
    private enum Lang
    {
        [Language(IsoCode: "en-US")]
        English,

        [Language(IsoCode: "sv-SE", Fallback = English, IsMandatory = true)]
        Swedish,
    }

    private static IReadOnlyList<LanguageSetDefinition> Scan() =>
        new DocumentTypeScanner().ScanLanguages(new[] { typeof(Lang).Assembly });

    [Test]
    public async Task SyncAsync_UpdatesExistingLanguage_WhenMandatoryOrFallbackDrifted()
    {
        // sv-SE already exists but with stale IsMandatory/FallbackIsoCode that no longer match
        // what [Language] on Lang.Swedish declares (IsMandatory: true, Fallback: English).
        var existingSwedish = new Language("sv-SE", "Swedish")
        {
            IsMandatory = false,
            FallbackIsoCode = null,
        };
        var existingEnglish = new Language("en-US", "English") { IsDefault = true };

        var service = new FakeLanguageService(existingEnglish, existingSwedish);
        var engine = new LanguageSyncEngine(service, NullLogger<LanguageSyncEngine>.Instance);

        var definition = Scan().Single(d => d.EnumType == typeof(Lang));
        await engine.SyncAsync(definition);

        Assert.That(existingSwedish.IsMandatory, Is.True);
        Assert.That(existingSwedish.FallbackIsoCode, Is.EqualTo("en-US"));
        Assert.That(service.UpdateCallCount, Is.EqualTo(1));
        Assert.That(service.CreateCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SyncAsync_DoesNotUpdate_WhenExistingLanguageAlreadyMatchesCode()
    {
        var existingSwedish = new Language("sv-SE", "Swedish")
        {
            IsMandatory = true,
            FallbackIsoCode = "en-US",
        };
        var existingEnglish = new Language("en-US", "English") { IsDefault = true };

        var service = new FakeLanguageService(existingEnglish, existingSwedish);
        var engine = new LanguageSyncEngine(service, NullLogger<LanguageSyncEngine>.Instance);

        var definition = Scan().Single(d => d.EnumType == typeof(Lang));
        await engine.SyncAsync(definition);

        Assert.That(service.UpdateCallCount, Is.EqualTo(0));
        Assert.That(service.CreateCallCount, Is.EqualTo(0));
    }

    // Minimal fake covering only what LanguageSyncEngine calls (GetAsync/CreateAsync/UpdateAsync).
    // Other ILanguageService members are unused by the engine and throw if ever exercised.
    private sealed class FakeLanguageService : ILanguageService
    {
        private readonly Dictionary<string, ILanguage> _byIsoCode;

        public FakeLanguageService(params ILanguage[] existing) =>
            _byIsoCode = existing.ToDictionary(l => l.IsoCode, StringComparer.OrdinalIgnoreCase);

        public int CreateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<ILanguage?> GetAsync(string isoCode) =>
            Task.FromResult(_byIsoCode.GetValueOrDefault(isoCode));

        public Task<Attempt<ILanguage, LanguageOperationStatus>> CreateAsync(ILanguage language, Guid userKey)
        {
            CreateCallCount++;
            _byIsoCode[language.IsoCode] = language;
            return Task.FromResult(Attempt.SucceedWithStatus(LanguageOperationStatus.Success, language));
        }

        public Task<Attempt<ILanguage, LanguageOperationStatus>> UpdateAsync(ILanguage language, Guid userKey)
        {
            UpdateCallCount++;
            _byIsoCode[language.IsoCode] = language;
            return Task.FromResult(Attempt.SucceedWithStatus(LanguageOperationStatus.Success, language));
        }

        public Task<ILanguage?> GetDefaultLanguageAsync() => throw new NotImplementedException();
        public Task<string> GetDefaultIsoCodeAsync() => throw new NotImplementedException();
        public Task<IEnumerable<ILanguage>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<ILanguage>> GetMultipleAsync(IEnumerable<string> isoCodes) => throw new NotImplementedException();
        public Task<Attempt<ILanguage?, LanguageOperationStatus>> DeleteAsync(string isoCode, Guid userKey) => throw new NotImplementedException();
        public Task<string[]> GetIsoCodesByIdsAsync(ICollection<int> ids) => throw new NotImplementedException();
    }
}
