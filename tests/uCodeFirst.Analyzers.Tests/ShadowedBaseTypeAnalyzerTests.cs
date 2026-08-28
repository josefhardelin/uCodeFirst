using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using uCodeFirst.Analyzers;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.Analyzers.Tests;

[TestFixture]
public class ShadowedBaseTypeAnalyzerTests
{
    // Microsoft.CodeAnalysis.Testing predates net10.0 and can't resolve a matching
    // reference-assembly package for it, so the test compilation is built directly against
    // the currently running runtime's assemblies instead of a downloaded reference set.
    private static readonly ImmutableArray<MetadataReference> RuntimeReferences =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

    [Test]
    public async Task ConsumerType_ShadowingLibraryBaseType_ReportsDiagnostic()
    {
        const string source = """
            using uCodeFirst.DataTypes.Bases;

            namespace Consumer.DataTypes
            {
                public sealed class {|#0:TagsDataType|} : uCodeFirst.DataTypes.Bases.TagsDataType
                {
                }
            }
            """;

        var expected = new DiagnosticResult(ShadowedBaseTypeAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0);

        await Verify(source, expected);
    }

    [Test]
    public async Task ConsumerType_SubclassingWithNoNameCollision_ReportsNoDiagnostic()
    {
        const string source = """
            using uCodeFirst.DataTypes.Bases;

            namespace Consumer.DataTypes
            {
                public sealed class BlogPostTagsDataType : TagsDataType
                {
                }
            }
            """;

        await Verify(source);
    }

    private static async Task Verify(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ShadowedBaseTypeAnalyzer, NUnitVerifier>
        {
            TestState = { Sources = { source } },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var references = RuntimeReferences.Add(MetadataReference.CreateFromFile(typeof(TagsDataType).Assembly.Location));
            return solution.GetProject(projectId)!.WithMetadataReferences(references).Solution;
        });

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
