using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;
using uCodeFirst.Discovery;
using uCodeFirst.Validation;

namespace uCodeFirst.Tests.Validation;

[TestFixture]
public class PreFlightValidatorTests
{
    // --- Fixtures used for AllowedChildren / composition-key reflection checks ---------------
    // These need to be *real* attributed CLR types because PreFlightValidator inspects
    // childType.GetCustomAttribute<...>() directly via reflection; hand-built DocumentTypeDefinition
    // records alone aren't enough for those specific checks.

    [DocumentType(Name: "Valid Child", Guid = "20000000-0000-0000-0000-000000000001")]
    private sealed class ValidChildFixture { }

    private sealed class NotADocumentTypeFixture { }

    [CompositionType(Name: "Seo Composition", Guid = "20000000-0000-0000-0000-000000000002")]
    private interface ISeoCompositionFixture
    {
        [TextString(Alias = "metaTitle")]
        string? MetaTitle { get; set; }
    }

    private static PropertyDefinition Property(string alias, string group = "Content") =>
        new(Alias: alias, Name: alias, GroupName: group, SortOrder: 0, Mandatory: false, Description: null, DataType: new TextString(), VariesByCulture: false);

    private static PropertyDefinition Property(string alias, DataTypeBase dataType) =>
        new(Alias: alias, Name: alias, GroupName: "Content", SortOrder: 0, Mandatory: false, Description: null, DataType: dataType, VariesByCulture: false);

    private static DocumentTypeDefinition Definition(
        Type clrType,
        string alias,
        Guid key,
        IReadOnlyList<PropertyDefinition>? properties = null,
        IReadOnlyList<Type>? allowedChildTypes = null,
        IReadOnlyList<Guid>? compositionKeys = null) =>
        new(
            ClrType: clrType,
            IsElement: false,
            Key: key,
            Alias: alias,
            Name: alias,
            Icon: null,
            Color: null,
            Description: null,
            AllowedAtRoot: false,
            Folder: null,
            DefaultTemplate: null,
            AllowedChildTypes: allowedChildTypes ?? Array.Empty<Type>(),
            Properties: properties ?? Array.Empty<PropertyDefinition>(),
            CompositionKeys: compositionKeys ?? Array.Empty<Guid>(),
            VariesByCulture: false,
            IsContainer: false);

    // --- Duplicate alias / GUID across document types ------------------------------------------

    [Test]
    public void DuplicateAlias_AcrossTwoDocumentTypes_ProducesError()
    {
        var a = Definition(typeof(ValidChildFixture), alias: "shared", key: Guid.NewGuid());
        var b = Definition(typeof(NotADocumentTypeFixture), alias: "shared", key: Guid.NewGuid());

        var errors = new PreFlightValidator().Validate(new[] { a, b });

        Assert.That(errors, Has.Some.Contains("Duplicate alias"));
    }

    [Test]
    public void DuplicateGuid_AcrossTwoDocumentTypes_ProducesError()
    {
        var sharedKey = Guid.NewGuid();
        var a = Definition(typeof(ValidChildFixture), alias: "articleA", key: sharedKey);
        var b = Definition(typeof(NotADocumentTypeFixture), alias: "articleB", key: sharedKey);

        var errors = new PreFlightValidator().Validate(new[] { a, b });

        Assert.That(errors, Has.Some.Contains("Duplicate GUID"));
    }

    // --- Duplicate property alias within one type ----------------------------------------------

    [Test]
    public void DuplicatePropertyAlias_WithinOneType_ProducesError()
    {
        var def = Definition(
            typeof(ValidChildFixture),
            alias: "article",
            key: Guid.NewGuid(),
            properties: new[] { Property("headline"), Property("headline") });

        var errors = new PreFlightValidator().Validate(new[] { def });

        Assert.That(errors, Has.Some.Contains("Duplicate property alias"));
    }

    // --- Duplicate property alias across a class and a composition it implements ---------------

    [Test]
    public void DuplicatePropertyAlias_AcrossCompositionAndImplementingClass_IsNotCurrentlyDetected()
    {
        // NOTE: This documents a real gap found while writing these tests, not desired behavior.
        // PreFlightValidator's duplicate-property-alias check (see PreFlightValidator.cs, the loop
        // building `propAliases` per definition) only looks within a single DocumentTypeDefinition's
        // own Properties list. Composition properties live on a *separate* DocumentTypeDefinition
        // (the one scanned for the [CompositionType] interface) and are never merged with the
        // implementing class's own definition for this check. So two properties that only collide
        // via a shared explicit Alias across a class and a composition it implements (different C#
        // member names, so the scanner's name-based exclusion doesn't apply either) currently slip
        // through PreFlightValidator with no error, only to fail later at the Umbraco API level when
        // ContentTypeSyncEngine wires up the composition. Not fixed here per instructions to report
        // rather than silently patch production code -- flagging this as a candidate follow-up.
        var composition = Definition(
            typeof(ISeoCompositionFixture),
            alias: "seoComposition",
            key: Guid.Parse("20000000-0000-0000-0000-000000000002"),
            properties: new[] { Property("metaTitle") });

        var implementingClass = Definition(
            typeof(ValidChildFixture),
            alias: "articleWithComposition",
            key: Guid.NewGuid(),
            properties: new[] { Property("metaTitle") }, // same alias, unrelated to the composition's own property by name
            compositionKeys: new[] { composition.Key });

        var errors = new PreFlightValidator().Validate(new[] { composition, implementingClass });

        Assert.That(errors, Is.Empty);
    }

    // --- AllowedChildren -------------------------------------------------------------------------

    [Test]
    public void AllowedChildren_ReferencingTypeWithoutDocumentTypeAttribute_ProducesError()
    {
        var def = Definition(
            typeof(ValidChildFixture),
            alias: "parent",
            key: Guid.NewGuid(),
            allowedChildTypes: new[] { typeof(NotADocumentTypeFixture) });

        var errors = new PreFlightValidator().Validate(new[] { def });

        Assert.That(errors, Has.Some.Contains("has no [DocumentType] attribute"));
    }

    [Test]
    public void AllowedChildren_ReferencingValidDiscoveredType_ProducesNoError()
    {
        var childKey = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var child = Definition(typeof(ValidChildFixture), alias: "validChild", key: childKey);
        var parent = Definition(
            typeof(NotADocumentTypeFixture),
            alias: "parent",
            key: Guid.NewGuid(),
            allowedChildTypes: new[] { typeof(ValidChildFixture) });

        var errors = new PreFlightValidator().Validate(new[] { child, parent });

        Assert.That(errors, Is.Empty);
    }

    // --- ContentPicker / MultiNodeTreePicker document-type references ----------------------------

    private sealed class ContentPickerFixture : ContentPickerDataType
    {
        public ContentPickerFixture(params Type[] allowedContentTypes) => AllowedContentTypes = allowedContentTypes;
        public override Type[] AllowedContentTypes { get; }
    }

    private sealed class MultiNodeTreePickerFixture : MultiNodeTreePickerDataType
    {
        public MultiNodeTreePickerFixture(Type[]? allowedContentTypes = null, DynamicRootConfig? dynamicRoot = null)
        {
            AllowedContentTypes = allowedContentTypes ?? Array.Empty<Type>();
            DynamicRoot = dynamicRoot;
        }

        public override Type[] AllowedContentTypes { get; }
        public override DynamicRootConfig? DynamicRoot { get; }
    }

    [Test]
    public void ContentPickerAllowedContentTypes_ReferencingTypeWithoutDocumentTypeAttribute_ProducesError()
    {
        var def = Definition(
            typeof(ValidChildFixture),
            alias: "hasPicker",
            key: Guid.NewGuid(),
            properties: new[] { Property("related", new ContentPickerFixture(typeof(NotADocumentTypeFixture))) });

        var errors = new PreFlightValidator().Validate(new[] { def });

        Assert.That(errors, Has.Some.Contains("has no [DocumentType] attribute"));
    }

    [Test]
    public void ContentPickerAllowedContentTypes_ReferencingValidDiscoveredType_ProducesNoError()
    {
        var childKey = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var child = Definition(typeof(ValidChildFixture), alias: "validChild", key: childKey);
        var parent = Definition(
            typeof(NotADocumentTypeFixture),
            alias: "hasPicker",
            key: Guid.NewGuid(),
            properties: new[] { Property("related", new ContentPickerFixture(typeof(ValidChildFixture))) });

        var errors = new PreFlightValidator().Validate(new[] { child, parent });

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void MultiNodeTreePickerAllowedContentTypes_ReferencingTypeWithoutDocumentTypeAttribute_ProducesError()
    {
        var def = Definition(
            typeof(ValidChildFixture),
            alias: "hasMultiPicker",
            key: Guid.NewGuid(),
            properties: new[] { Property("related", new MultiNodeTreePickerFixture(allowedContentTypes: new[] { typeof(NotADocumentTypeFixture) })) });

        var errors = new PreFlightValidator().Validate(new[] { def });

        Assert.That(errors, Has.Some.Contains("has no [DocumentType] attribute"));
    }

    [Test]
    public void MultiNodeTreePickerDynamicRootQueryStep_ReferencingTypeWithoutDocumentTypeAttribute_ProducesError()
    {
        var dynamicRoot = new DynamicRootConfig
        {
            Origin = DynamicRootOrigin.Site,
            QuerySteps = new[]
            {
                new DynamicRootQueryStep
                {
                    Direction = DynamicRootQueryStepDirection.NearestDescendantOrSelf,
                    DocumentTypes = new[] { typeof(NotADocumentTypeFixture) }
                }
            }
        };

        var def = Definition(
            typeof(ValidChildFixture),
            alias: "hasDynamicRoot",
            key: Guid.NewGuid(),
            properties: new[] { Property("related", new MultiNodeTreePickerFixture(dynamicRoot: dynamicRoot)) });

        var errors = new PreFlightValidator().Validate(new[] { def });

        Assert.That(errors, Has.Some.Contains("has no [DocumentType] attribute"));
    }

    [Test]
    public void MultiNodeTreePickerDynamicRootQueryStep_ReferencingValidDiscoveredType_ProducesNoError()
    {
        var childKey = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var child = Definition(typeof(ValidChildFixture), alias: "validChild", key: childKey);

        var dynamicRoot = new DynamicRootConfig
        {
            Origin = DynamicRootOrigin.Site,
            QuerySteps = new[]
            {
                new DynamicRootQueryStep
                {
                    Direction = DynamicRootQueryStepDirection.NearestDescendantOrSelf,
                    DocumentTypes = new[] { typeof(ValidChildFixture) }
                }
            }
        };

        var parent = Definition(
            typeof(NotADocumentTypeFixture),
            alias: "hasDynamicRoot",
            key: Guid.NewGuid(),
            properties: new[] { Property("related", new MultiNodeTreePickerFixture(dynamicRoot: dynamicRoot)) });

        var errors = new PreFlightValidator().Validate(new[] { child, parent });

        Assert.That(errors, Is.Empty);
    }

    // --- Dictionary item key collisions ----------------------------------------------------------

    [Test]
    public void DictionaryItems_TwoLeavesWithSameKey_ProducesError()
    {
        var fieldA = typeof(DictionaryFixtureA).GetField(nameof(DictionaryFixtureA.Greeting))!;
        var fieldB = typeof(DictionaryFixtureB).GetField(nameof(DictionaryFixtureB.Greeting))!;

        var definitions = new[]
        {
            new DictionaryItemDefinition(fieldA, "Greeting", Array.Empty<string>()),
            new DictionaryItemDefinition(fieldB, "Greeting", Array.Empty<string>()),
        };

        var errors = new PreFlightValidator().Validate(Array.Empty<DocumentTypeDefinition>(), dictionaryDefinitions: definitions);

        Assert.That(errors, Has.Some.Contains("Duplicate dictionary item key"));
    }

    [Test]
    public void DictionaryItems_LeafKeyCollidingWithAutoCreatedParentContainerName_ProducesError()
    {
        // Parent chain ["DictionaryContainerFixture"] means a parent DictionaryItem with that key
        // (ValidateDictionaryItems claims it) is auto-created. A leaf item literally keyed with
        // that same class name collides with the auto-created parent.
        var field = typeof(DictionaryFixtureA).GetField(nameof(DictionaryFixtureA.Greeting))!;

        var definitions = new[]
        {
            new DictionaryItemDefinition(field, nameof(DictionaryContainerFixture), new[] { nameof(DictionaryContainerFixture) }),
        };

        var errors = new PreFlightValidator().Validate(Array.Empty<DocumentTypeDefinition>(), dictionaryDefinitions: definitions);

        Assert.That(errors, Has.Some.Contains("Duplicate dictionary item key"));
    }

    private static class DictionaryContainerFixture { }

    private static class DictionaryFixtureA
    {
        public const string Greeting = nameof(Greeting);
    }

    private static class DictionaryFixtureB
    {
        public const string Greeting = nameof(Greeting);
    }

    // --- Negative-space: a fully valid, non-colliding set of definitions --------------------------

    [Test]
    public void FullyValidDefinitions_ProduceNoErrors()
    {
        var childKey = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var child = Definition(typeof(ValidChildFixture), alias: "validChild", key: childKey, properties: new[] { Property("headline") });
        var parent = Definition(
            typeof(NotADocumentTypeFixture),
            alias: "parent",
            key: Guid.NewGuid(),
            properties: new[] { Property("summary") },
            allowedChildTypes: new[] { typeof(ValidChildFixture) });

        var dictionaryField = typeof(DictionaryFixtureA).GetField(nameof(DictionaryFixtureA.Greeting))!;
        var dictionaryDefinitions = new[] { new DictionaryItemDefinition(dictionaryField, "Greeting", Array.Empty<string>()) };

        var errors = new PreFlightValidator().Validate(new[] { child, parent }, dictionaryDefinitions: dictionaryDefinitions);

        Assert.That(errors, Is.Empty);
    }

    // --- SeedContent ------------------------------------------------------------------------------

    private sealed class SeedFixtureA { }
    private sealed class SeedFixtureB { }
    private sealed class SeedFixtureC { }

    private static SeedContentDefinition Seed(Type clrType, Guid key, Type documentType, Type? parent = null) =>
        new(ClrType: clrType, Key: key, DocumentType: documentType, Name: clrType.Name, Parent: parent);

    [Test]
    public void SeedContent_DuplicateKey_ProducesError()
    {
        var sharedKey = Guid.NewGuid();
        var a = Seed(typeof(SeedFixtureA), sharedKey, typeof(ValidChildFixture));
        var b = Seed(typeof(SeedFixtureB), sharedKey, typeof(ValidChildFixture));

        var errors = new PreFlightValidator().Validate(
            new[] { Definition(typeof(ValidChildFixture), alias: "validChild", key: Guid.Parse("20000000-0000-0000-0000-000000000001")) },
            seedContentDefinitions: new[] { a, b });

        Assert.That(errors, Has.Some.Contains("Duplicate seed content GUID"));
    }

    [Test]
    public void SeedContent_DocumentTypeWithoutDocumentTypeAttribute_ProducesError()
    {
        var seed = Seed(typeof(SeedFixtureA), Guid.NewGuid(), typeof(NotADocumentTypeFixture));

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            seedContentDefinitions: new[] { seed });

        Assert.That(errors, Has.Some.Contains("has no [DocumentType] attribute"));
    }

    [Test]
    public void SeedContent_DocumentTypeNotInScannedSet_ProducesError()
    {
        var seed = Seed(typeof(SeedFixtureA), Guid.NewGuid(), typeof(ValidChildFixture));

        // ValidChildFixture carries a real [DocumentType] attribute, but it's deliberately not passed
        // in the `definitions` list here, so its key never lands in the scanned-keys set.
        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            seedContentDefinitions: new[] { seed });

        Assert.That(errors, Has.Some.Contains("was not discovered in the scanned assembly set"));
    }

    [Test]
    public void SeedContent_ParentNotDeclaredAsSeed_ProducesError()
    {
        var seed = Seed(typeof(SeedFixtureA), Guid.NewGuid(), typeof(ValidChildFixture), parent: typeof(SeedFixtureB));

        var errors = new PreFlightValidator().Validate(
            new[] { Definition(typeof(ValidChildFixture), alias: "validChild", key: Guid.Parse("20000000-0000-0000-0000-000000000001")) },
            seedContentDefinitions: new[] { seed });

        Assert.That(errors, Has.Some.Contains("has no [SeedContent] attribute"));
    }

    [Test]
    public void SeedContent_ParentCycle_ProducesError()
    {
        var a = Seed(typeof(SeedFixtureA), Guid.NewGuid(), typeof(ValidChildFixture), parent: typeof(SeedFixtureB));
        var b = Seed(typeof(SeedFixtureB), Guid.NewGuid(), typeof(ValidChildFixture), parent: typeof(SeedFixtureA));

        var errors = new PreFlightValidator().Validate(
            new[] { Definition(typeof(ValidChildFixture), alias: "validChild", key: Guid.Parse("20000000-0000-0000-0000-000000000001")) },
            seedContentDefinitions: new[] { a, b });

        Assert.That(errors, Has.Some.Contains("cycle"));
    }

    [Test]
    public void SeedContent_SelfReferencingParent_ProducesCycleError()
    {
        var seed = Seed(typeof(SeedFixtureA), Guid.NewGuid(), typeof(ValidChildFixture), parent: typeof(SeedFixtureA));

        var errors = new PreFlightValidator().Validate(
            new[] { Definition(typeof(ValidChildFixture), alias: "validChild", key: Guid.Parse("20000000-0000-0000-0000-000000000001")) },
            seedContentDefinitions: new[] { seed });

        Assert.That(errors, Has.Some.Contains("cycle"));
    }

    [Test]
    public void SeedContent_ValidChainOfThreeLevels_ProducesNoError()
    {
        var docTypeKey = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var a = Seed(typeof(SeedFixtureA), Guid.NewGuid(), typeof(ValidChildFixture));
        var b = Seed(typeof(SeedFixtureB), Guid.NewGuid(), typeof(ValidChildFixture), parent: typeof(SeedFixtureA));
        var c = Seed(typeof(SeedFixtureC), Guid.NewGuid(), typeof(ValidChildFixture), parent: typeof(SeedFixtureB));

        var errors = new PreFlightValidator().Validate(
            new[] { Definition(typeof(ValidChildFixture), alias: "validChild", key: docTypeKey) },
            seedContentDefinitions: new[] { a, b, c });

        Assert.That(errors, Is.Empty);
    }
}
