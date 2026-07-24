using System.ComponentModel;
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
public class TemplateSyncEngineTests
{
    private enum Templates
    {
        [Template(Alias: "_layout")]
        Layout,

        [Template(Alias: "page", Master = Layout)]
        Page,
    }

    private static IReadOnlyList<TemplateDefinition> Scan() =>
        new DocumentTypeScanner().ScanTemplates(new[] { typeof(Templates).Assembly })
            .Where(d => d.Member.DeclaringType == typeof(Templates))
            .ToList();

    [Test]
    public async Task SyncAsync_UpdatesTemplateMaster_WhenLayoutLineDiffers()
    {
        var layout = new FakeTemplate("_layout", masterAlias: null, content: null);
        var page = new FakeTemplate("page", masterAlias: "_oldmaster", content: "Layout = \"_oldmaster.cshtml\";\r\n<h1>Page</h1>");

        var service = new FakeTemplateService(layout, page);
        var engine = new TemplateSyncEngine(service, NullLogger<TemplateSyncEngine>.Instance);

        await engine.SyncAsync(Scan());

        Assert.That(service.UpdateCallCount, Is.EqualTo(1));
        Assert.That(service.CreateCallCount, Is.EqualTo(0));
        Assert.That(page.Content, Does.Contain("Layout = \"_layout.cshtml\";"));
        Assert.That(page.Content, Does.Not.Contain("_oldmaster"));
        Assert.That(page.Content, Does.Contain("<h1>Page</h1>"));
    }

    [Test]
    public async Task SyncAsync_DoesNotUpdate_WhenMasterAlreadyMatches()
    {
        var layout = new FakeTemplate("_layout", masterAlias: null, content: null);
        var page = new FakeTemplate("page", masterAlias: "_layout", content: "Layout = \"_layout.cshtml\";\r\n<h1>Page</h1>");

        var service = new FakeTemplateService(layout, page);
        var engine = new TemplateSyncEngine(service, NullLogger<TemplateSyncEngine>.Instance);

        await engine.SyncAsync(Scan());

        Assert.That(service.UpdateCallCount, Is.EqualTo(0));
        Assert.That(service.CreateCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SyncAsync_LeavesTemplateAsIs_WhenLayoutDirectiveCannotBeLocated()
    {
        var layout = new FakeTemplate("_layout", masterAlias: null, content: null);
        // Master drifted (per Umbraco's own MasterTemplateAlias), but no recognizable Layout line
        // exists to safely rewrite — must not guess and risk corrupting hand-authored content.
        var page = new FakeTemplate("page", masterAlias: "_oldmaster", content: "@inherits SomeBase\n<h1>No layout directive here</h1>");

        var service = new FakeTemplateService(layout, page);
        var engine = new TemplateSyncEngine(service, NullLogger<TemplateSyncEngine>.Instance);

        await engine.SyncAsync(Scan());

        Assert.That(service.UpdateCallCount, Is.EqualTo(0));
        Assert.That(page.Content, Is.EqualTo("@inherits SomeBase\n<h1>No layout directive here</h1>"));
    }

    // Minimal fake covering only what TemplateSyncEngine reads/writes: Alias, MasterTemplateAlias,
    // Content. Other ITemplate members are unused by the engine and throw if ever exercised.
    private sealed class FakeTemplate(string alias, string? masterAlias, string? content) : ITemplate
    {
        public string? Name { get; set; } = alias;
        public string Alias { get; set; } = alias;
        public bool IsMasterTemplate { get; set; }
        public string? MasterTemplateAlias { get; set; } = masterAlias;
        public void SetMasterTemplate(ITemplate? masterTemplate) => throw new NotImplementedException();

        public string Path { get; set; } = string.Empty;
        public string OriginalPath => Path;
        public string? Content { get; set; } = content;
        public string? VirtualPath { get; set; }
        public void ResetOriginalPath() { }

        public int Id { get; set; }
        public Guid Key { get; set; } = Guid.NewGuid();
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        public bool HasIdentity => true;
        public void ResetIdentity() { }

        public object DeepClone() => throw new NotImplementedException();

        public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public bool IsDirty() => false;
        public bool IsPropertyDirty(string propName) => false;
        public IEnumerable<string> GetDirtyProperties() => [];
        public void ResetDirtyProperties() { }
        public void DisableChangeTracking() { }
        public void EnableChangeTracking() { }

        public bool WasDirty() => false;
        public bool WasPropertyDirty(string propertyName) => false;
        public void ResetWereDirtyProperties() { }
        public void ResetDirtyProperties(bool rememberDirty) { }
        public IEnumerable<string> GetWereDirtyProperties() => [];
    }

    // Minimal fake covering only what TemplateSyncEngine calls (GetAsync/UpdateAsync, and CreateAsync
    // for templates that don't exist yet). Other ITemplateService members are unused and throw.
    private sealed class FakeTemplateService(params FakeTemplate[] existing) : ITemplateService
    {
        private readonly Dictionary<string, ITemplate> _byAlias = existing.ToDictionary(t => t.Alias, t => (ITemplate)t, StringComparer.OrdinalIgnoreCase);

        public int CreateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<ITemplate?> GetAsync(string? alias) =>
            Task.FromResult(alias is not null ? _byAlias.GetValueOrDefault(alias) : null);

        public Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(string name, string alias, string? content, Guid userKey, Guid? templateKey = null)
        {
            CreateCallCount++;
            var template = new FakeTemplate(alias, null, content);
            _byAlias[alias] = template;
            return Task.FromResult(Attempt.SucceedWithStatus(TemplateOperationStatus.Success, (ITemplate)template));
        }

        public Task<Attempt<ITemplate, TemplateOperationStatus>> UpdateAsync(ITemplate template, Guid userKey)
        {
            UpdateCallCount++;
            _byAlias[template.Alias] = template;
            return Task.FromResult(Attempt.SucceedWithStatus(TemplateOperationStatus.Success, template));
        }

        public Task<IEnumerable<ITemplate>> GetAllAsync(params string[] aliases) => throw new NotImplementedException();
        public Task<IEnumerable<ITemplate>> GetAllAsync(Guid[] keys) => throw new NotImplementedException();
        public Task<IEnumerable<ITemplate>> GetChildrenAsync(int masterTemplateId) => throw new NotImplementedException();
        public Task<ITemplate?> GetAsync(int id) => throw new NotImplementedException();
        public Task<ITemplate?> GetAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<ITemplate>> GetDescendantsAsync(int masterTemplateId) => throw new NotImplementedException();
        public Task<Attempt<ITemplate, TemplateOperationStatus>> CreateForContentTypeAsync(string contentTypeAlias, string? contentTypeName, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(ITemplate template, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate?, TemplateOperationStatus>> DeleteAsync(string alias, Guid userKey) => throw new NotImplementedException();
        public Task<Attempt<ITemplate?, TemplateOperationStatus>> DeleteAsync(Guid key, Guid userKey) => throw new NotImplementedException();
        public Task<Stream> GetFileContentStreamAsync(string filepath) => throw new NotImplementedException();
        public Task SetFileContentAsync(string filepath, Stream content) => throw new NotImplementedException();
        public Task<long> GetFileSizeAsync(string filepath) => throw new NotImplementedException();
    }
}
