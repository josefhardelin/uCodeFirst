using uCodeFirst.Attributes;
using uCodeFirst.Discovery;
using uCodeFirst.Validation;

namespace uCodeFirst.Tests.Validation;

[TestFixture]
public class PreFlightValidatorTemplatesTests
{
    private static class ValidTemplates
    {
        [Template]
        public const string Layout = "_layout";

        [Template(Master = Layout)]
        public const string Page = "page";
    }

    private static class CyclicTemplates
    {
        [Template(Master = B)]
        public const string A = "a";

        [Template(Master = A)]
        public const string B = "b";
    }

    private static class SelfReferencingTemplate
    {
        [Template(Master = Self)]
        public const string Self = "self";
    }

    private static IReadOnlyList<TemplateDefinition> Scan(Type containingType) =>
        new DocumentTypeScanner().ScanTemplates(new[] { containingType.Assembly })
            .Where(d => d.Member.DeclaringType == containingType)
            .ToList();

    [Test]
    public void MasterChain_WithoutCycle_ProducesNoError()
    {
        var definitions = Scan(typeof(ValidTemplates));

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            templateDefinitions: definitions);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void MasterChain_WithCycle_ProducesError()
    {
        var definitions = Scan(typeof(CyclicTemplates));

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            templateDefinitions: definitions);

        Assert.That(errors, Has.Some.Contains("cycle"));
    }

    [Test]
    public void MasterReferencingItself_ProducesCycleError()
    {
        var definitions = Scan(typeof(SelfReferencingTemplate));

        var errors = new PreFlightValidator().Validate(
            Array.Empty<DocumentTypeDefinition>(),
            templateDefinitions: definitions);

        Assert.That(errors, Has.Some.Contains("cycle"));
    }
}
