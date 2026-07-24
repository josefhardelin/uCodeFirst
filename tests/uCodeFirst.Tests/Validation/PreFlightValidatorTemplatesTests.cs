using uCodeFirst.Attributes;
using uCodeFirst.Discovery;
using uCodeFirst.Validation;

namespace uCodeFirst.Tests.Validation;

[TestFixture]
public class PreFlightValidatorTemplatesTests
{
    private enum ValidTemplates
    {
        [Template(Alias: "_layout")]
        Layout,

        [Template(Alias: "page", Master = Layout)]
        Page,
    }

    private enum CyclicTemplates
    {
        [Template(Alias: "a", Master = B)]
        A,

        [Template(Alias: "b", Master = A)]
        B,
    }

    private enum SelfReferencingTemplate
    {
        [Template(Alias: "self", Master = Self)]
        Self,
    }

    private static IReadOnlyList<TemplateDefinition> Scan<TEnum>() where TEnum : struct, Enum =>
        new DocumentTypeScanner().ScanTemplates(new[] { typeof(TEnum).Assembly });

    [Test]
    public void MasterChain_WithoutCycle_ProducesNoError()
    {
        var definitions = Scan<ValidTemplates>()
            .Where(d => d.Member.DeclaringType == typeof(ValidTemplates))
            .ToList();

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            templateDefinitions: definitions);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void MasterChain_WithCycle_ProducesError()
    {
        var definitions = Scan<CyclicTemplates>()
            .Where(d => d.Member.DeclaringType == typeof(CyclicTemplates))
            .ToList();

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            templateDefinitions: definitions);

        Assert.That(errors, Has.Some.Contains("cycle"));
    }

    [Test]
    public void MasterReferencingItself_ProducesCycleError()
    {
        var definitions = Scan<SelfReferencingTemplate>()
            .Where(d => d.Member.DeclaringType == typeof(SelfReferencingTemplate))
            .ToList();

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            templateDefinitions: definitions);

        Assert.That(errors, Has.Some.Contains("cycle"));
    }
}
