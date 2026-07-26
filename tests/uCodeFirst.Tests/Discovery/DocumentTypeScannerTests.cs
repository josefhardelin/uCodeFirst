using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;

namespace uCodeFirst.Tests.Discovery;

[TestFixture]
public class DocumentTypeScannerTests
{
    // --- Fixtures -----------------------------------------------------------------------------

    [DocumentType(
        Name: "Article Page",
        Alias: "customArticleAlias",
        Icon: "icon-article",
        Color: "red",
        Description: "An article",
        AllowedAtRoot: true,
        Folder: "Pages/Articles",
        DefaultTemplate: "articleTemplate",
        VariesByCulture: true,
        IsContainer: true,
        PreventCleanup: true,
        KeepAllVersionsNewerThanDays: 30,
        KeepLatestVersionPerDayForDays: 90,
        Guid = "10000000-0000-0000-0000-000000000001")]
    private sealed class ArticlePageFixture
    {
        [TextString(Alias = "customHeadlineAlias", Name = "Headline Text", Mandatory = true, Description = "The headline", VariesByCulture = true)]
        public string? Headline { get; set; }

        [TextString]
        [Group("SEO", SortOrder: 5)]
        public string? MetaKeywords { get; set; }
    }

    [DocumentType(Name: "Landing Page", Guid = "10000000-0000-0000-0000-000000000002")]
    private sealed class LandingPageFixture
    {
        [TextString]
        public string? Subtitle { get; set; }
    }

    [ElementType(Name: "Simple Block", IsContainer: true, Guid = "10000000-0000-0000-0000-000000000003")]
    private sealed class SimpleBlockFixture
    {
        [TextString(Mandatory = true)]
        public string? Title { get; set; }
    }

    [CompositionType(Name: "Seo Composition", Guid = "10000000-0000-0000-0000-000000000004")]
    private interface ISeoComposition
    {
        [TextString(Alias = "metaTitle")]
        string? MetaTitle { get; set; }
    }

    [DocumentType(Name: "Landing Page With Composition", Guid = "10000000-0000-0000-0000-000000000005")]
    private sealed class LandingPageWithCompositionFixture : ISeoComposition
    {
        // No data-type attribute needed here at all -- exclusion is purely name-based against the
        // composition interface's own property names, independent of whether this redeclaration
        // carries a [TextString] attribute.
        public string? MetaTitle { get; set; }

        [TextString]
        public string? Headline { get; set; }
    }

    [DocumentType(Name: "Child Fixture", Guid = "10000000-0000-0000-0000-000000000006")]
    private sealed class ChildFixture { }

    [DocumentType(Name: "Parent Fixture", Guid = "10000000-0000-0000-0000-000000000007")]
    [AllowedChildren(typeof(ChildFixture))]
    private sealed class ParentFixture { }

    [MediaType(Name: "Custom Image", Alias: "customImage", AllowedAtRoot: true, Folder: "Media/Custom", Guid = "10000000-0000-0000-0000-000000000008")]
    private sealed class CustomImageMediaTypeFixture
    {
        [TextString(Mandatory = true)]
        public string? AltText { get; set; }
    }

    [MediaType(Name: "External Stub", External: true, Guid = "10000000-0000-0000-0000-000000000009")]
    private sealed class ExternalStubMediaTypeFixture { }

    [DictionaryItem]
    public const string RootGreeting = nameof(RootGreeting);

    private static class Emails
    {
        public static class Welcome
        {
            [DictionaryItem]
            public const string Subject = nameof(Subject);
        }
    }

    private static IReadOnlyList<Assembly> Assemblies => new[] { typeof(DocumentTypeScannerTests).Assembly };

    private static DocumentTypeDefinition Find<T>() =>
        new DocumentTypeScanner().Scan(Assemblies).Single(d => d.ClrType == typeof(T));

    // --- DocumentType: full round trip ---------------------------------------------------------

    [Test]
    public void Scan_DocumentType_RoundTripsTypeLevelMetadata()
    {
        var def = Find<ArticlePageFixture>();

        Assert.Multiple(() =>
        {
            Assert.That(def.IsElement, Is.False);
            Assert.That(def.Key, Is.EqualTo(Guid.Parse("10000000-0000-0000-0000-000000000001")));
            Assert.That(def.Alias, Is.EqualTo("customArticleAlias"));
            Assert.That(def.Name, Is.EqualTo("Article Page"));
            Assert.That(def.Icon, Is.EqualTo("icon-article"));
            Assert.That(def.Color, Is.EqualTo("red"));
            Assert.That(def.Description, Is.EqualTo("An article"));
            Assert.That(def.AllowedAtRoot, Is.True);
            Assert.That(def.Folder, Is.EqualTo("Pages/Articles"));
            Assert.That(def.DefaultTemplate, Is.EqualTo("articleTemplate"));
            Assert.That(def.VariesByCulture, Is.True);
            Assert.That(def.IsContainer, Is.True);
            Assert.That(def.PreventCleanup, Is.True);
            Assert.That(def.KeepAllVersionsNewerThanDays, Is.EqualTo(30));
            Assert.That(def.KeepLatestVersionPerDayForDays, Is.EqualTo(90));
        });
    }

    [Test]
    public void Scan_DocumentType_WithoutHistoryCleanupParams_DefaultsToUmbracoDefaults()
    {
        var def = Find<LandingPageFixture>();

        Assert.Multiple(() =>
        {
            Assert.That(def.PreventCleanup, Is.False);
            Assert.That(def.KeepAllVersionsNewerThanDays, Is.Null);
            Assert.That(def.KeepLatestVersionPerDayForDays, Is.Null);
        });
    }

    [Test]
    public void Scan_DocumentType_RoundTripsPropertyLevelMetadata()
    {
        var def = Find<ArticlePageFixture>();
        var headline = def.Properties.Single(p => p.Alias == "customHeadlineAlias");

        Assert.Multiple(() =>
        {
            Assert.That(headline.Name, Is.EqualTo("Headline Text"));
            Assert.That(headline.Mandatory, Is.True);
            Assert.That(headline.Description, Is.EqualTo("The headline"));
            Assert.That(headline.VariesByCulture, Is.True);
            Assert.That(headline.GroupName, Is.EqualTo(Groups.Content));
            Assert.That(headline.SortOrder, Is.EqualTo(0));
            Assert.That(headline.DataType, Is.InstanceOf<TextString>());
        });
    }

    [Test]
    public void Scan_Property_WithGroupAttribute_CapturesNameAndSortOrder()
    {
        var def = Find<ArticlePageFixture>();
        var meta = def.Properties.Single(p => p.Alias == "metaKeywords");

        Assert.That(meta.GroupName, Is.EqualTo("SEO"));
        Assert.That(meta.SortOrder, Is.EqualTo(5));
    }

    // --- Alias defaulting -------------------------------------------------------------------

    [Test]
    public void Scan_DocumentType_WithoutExplicitAlias_DefaultsToLowerCamelCaseTypeName()
    {
        var def = Find<LandingPageFixture>();

        Assert.That(def.Alias, Is.EqualTo("landingPageFixture"));
    }

    [Test]
    public void Scan_Property_WithoutExplicitAlias_DefaultsToLowerCamelCasePropertyName()
    {
        var def = Find<LandingPageFixture>();
        var prop = def.Properties.Single();

        Assert.That(prop.Alias, Is.EqualTo("subtitle"));
    }

    // --- ElementType ----------------------------------------------------------------------------

    [Test]
    public void Scan_ElementType_IsMarkedAsElement_AndNeverAllowedAtRootOrContainer()
    {
        var def = Find<SimpleBlockFixture>();

        Assert.Multiple(() =>
        {
            Assert.That(def.IsElement, Is.True);
            Assert.That(def.AllowedAtRoot, Is.False);
            Assert.That(def.Folder, Is.Null);
            Assert.That(def.DefaultTemplate, Is.Null);
            // [ElementType]'s IsContainer is kept for API symmetry but intentionally ignored by the
            // scanner -- element types are Block List/Grid item content, never tree nodes.
            Assert.That(def.IsContainer, Is.False);
        });

        var prop = def.Properties.Single();
        Assert.That(prop.Mandatory, Is.True);
    }

    // --- CompositionType --------------------------------------------------------------------

    [Test]
    public void Scan_CompositionType_DefaultsAliasFromInterfaceName_TrimmingLeadingI()
    {
        var def = Find<ISeoComposition>();

        Assert.Multiple(() =>
        {
            Assert.That(def.Alias, Is.EqualTo("seoComposition"));
            Assert.That(def.IsElement, Is.False);
            Assert.That(def.AllowedAtRoot, Is.False);
            Assert.That(def.Properties, Has.Count.EqualTo(1));
            Assert.That(def.Properties[0].Alias, Is.EqualTo("metaTitle"));
        });
    }

    [Test]
    public void Scan_ClassImplementingComposition_ExcludesCompositionPropertiesFromOwnList()
    {
        var def = Find<LandingPageWithCompositionFixture>();
        var compositionKey = Find<ISeoComposition>().Key;

        Assert.That(def.CompositionKeys, Does.Contain(compositionKey));

        // MetaTitle belongs to ISeoComposition and must not be duplicated onto the implementing
        // class's own property list, even though the class redeclares it (without any data-type
        // attribute) to satisfy the interface.
        Assert.That(def.Properties, Has.Count.EqualTo(1));
        Assert.That(def.Properties[0].Alias, Is.EqualTo("headline"));
    }

    // --- AllowedChildren ----------------------------------------------------------------------

    [Test]
    public void Scan_AllowedChildrenAttribute_FlowsThroughUntouched()
    {
        var def = Find<ParentFixture>();

        Assert.That(def.AllowedChildTypes, Is.EqualTo(new[] { typeof(ChildFixture) }));
    }

    // --- MediaType ------------------------------------------------------------------------------

    [Test]
    public void ScanMediaTypes_FindsMediaTypeAndItsProperties()
    {
        var def = new DocumentTypeScanner().ScanMediaTypes(Assemblies)
            .Single(d => d.ClrType == typeof(CustomImageMediaTypeFixture));

        Assert.Multiple(() =>
        {
            Assert.That(def.Alias, Is.EqualTo("customImage"));
            Assert.That(def.Name, Is.EqualTo("Custom Image"));
            Assert.That(def.AllowedAtRoot, Is.True);
            Assert.That(def.Folder, Is.EqualTo("Media/Custom"));
            Assert.That(def.ParentKey, Is.Null);
            Assert.That(def.Properties, Has.Count.EqualTo(1));
        });

        var prop = def.Properties.Single();
        Assert.That(prop.Alias, Is.EqualTo("altText"));
        Assert.That(prop.Mandatory, Is.True);
    }

    [Test]
    public void ScanMediaTypes_SkipsTypesMarkedExternal()
    {
        var definitions = new DocumentTypeScanner().ScanMediaTypes(Assemblies);

        Assert.That(definitions, Has.None.Matches<MediaTypeDefinition>(d => d.ClrType == typeof(ExternalStubMediaTypeFixture)));
    }

    // --- DictionaryItem -------------------------------------------------------------------------

    [Test]
    public void ScanDictionaryItems_NestedStaticClasses_ResolveParentChain()
    {
        var def = new DocumentTypeScanner().ScanDictionaryItems(Assemblies)
            .Single(d => d.Field.DeclaringType == typeof(Emails.Welcome));

        Assert.That(def.ItemKey, Is.EqualTo("Subject"));
        Assert.That(def.ParentChain, Is.EqualTo(new[] { typeof(Emails), typeof(Emails.Welcome) }));
    }

    [Test]
    public void ScanDictionaryItems_TopLevelField_HasEmptyParentChain()
    {
        var def = new DocumentTypeScanner().ScanDictionaryItems(Assemblies)
            .Single(d => d.Field.DeclaringType == typeof(DocumentTypeScannerTests));

        Assert.That(def.ItemKey, Is.EqualTo("RootGreeting"));
        Assert.That(def.ParentChain, Is.Empty);
    }

    // --- SeedContent ------------------------------------------------------------------------------

    [DocumentType(Name: "Site Settings Page", Guid = "10000000-0000-0000-0000-00000000000a")]
    private sealed class SiteSettingsPageFixture { }

    [SeedContent(DocumentType: typeof(SiteSettingsPageFixture), Name: "Site Settings", Guid = "10000000-0000-0000-0000-00000000000b")]
    private sealed class SiteSettingsSeedFixture { }

    [SeedContent(DocumentType: typeof(SiteSettingsPageFixture), Name: "Nested Child", Parent: typeof(SiteSettingsSeedFixture), Guid = "10000000-0000-0000-0000-00000000000c")]
    private sealed class ChildSeedFixture { }

    [Test]
    public void ScanSeedContent_FindsSeedAndItsMetadata()
    {
        var def = new DocumentTypeScanner().ScanSeedContent(Assemblies)
            .Single(d => d.ClrType == typeof(SiteSettingsSeedFixture));

        Assert.Multiple(() =>
        {
            Assert.That(def.Key, Is.EqualTo(Guid.Parse("10000000-0000-0000-0000-00000000000b")));
            Assert.That(def.DocumentType, Is.EqualTo(typeof(SiteSettingsPageFixture)));
            Assert.That(def.Name, Is.EqualTo("Site Settings"));
            Assert.That(def.Parent, Is.Null);
        });
    }

    [Test]
    public void ScanSeedContent_RoundTripsParentReference()
    {
        var def = new DocumentTypeScanner().ScanSeedContent(Assemblies)
            .Single(d => d.ClrType == typeof(ChildSeedFixture));

        Assert.That(def.Parent, Is.EqualTo(typeof(SiteSettingsSeedFixture)));
    }
}
