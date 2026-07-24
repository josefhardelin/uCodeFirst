using uCodeFirst.DataTypes;
using uCodeFirst.Discovery;
using uCodeFirst.Validation;

namespace uCodeFirst.Tests.Validation;

[TestFixture]
public class PreFlightValidatorCultureVarianceTests
{
    private static DocumentTypeDefinition MakeDefinition(bool variesByCulture, bool propertyVariesByCulture)
    {
        var property = new PropertyDefinition(
            Alias: "headline",
            Name: "Headline",
            GroupName: Groups.Content,
            SortOrder: 0,
            Mandatory: false,
            Description: null,
            DataType: new TextString(),
            VariesByCulture: propertyVariesByCulture);

        return new DocumentTypeDefinition(
            ClrType: typeof(PreFlightValidatorCultureVarianceTests),
            IsElement: false,
            Key: Guid.NewGuid(),
            Alias: "article",
            Name: "Article",
            Icon: null,
            Color: null,
            Description: null,
            AllowedAtRoot: false,
            Folder: null,
            DefaultTemplate: null,
            AllowedChildTypes: Array.Empty<Type>(),
            Properties: [property],
            CompositionKeys: Array.Empty<Guid>(),
            VariesByCulture: variesByCulture,
            IsContainer: false);
    }

    [Test]
    public void CultureVaryingProperty_OnInvariantType_ProducesError()
    {
        var definitions = new[] { MakeDefinition(variesByCulture: false, propertyVariesByCulture: true) };

        var errors = new PreFlightValidator().Validate(definitions);

        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors.Single(), Does.Contain("VariesByCulture"));
    }

    [Test]
    public void CultureVaryingProperty_OnCultureVaryingType_ProducesNoError()
    {
        var definitions = new[] { MakeDefinition(variesByCulture: true, propertyVariesByCulture: true) };

        var errors = new PreFlightValidator().Validate(definitions);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void InvariantProperty_OnInvariantType_ProducesNoError()
    {
        var definitions = new[] { MakeDefinition(variesByCulture: false, propertyVariesByCulture: false) };

        var errors = new PreFlightValidator().Validate(definitions);

        Assert.That(errors, Is.Empty);
    }
}
