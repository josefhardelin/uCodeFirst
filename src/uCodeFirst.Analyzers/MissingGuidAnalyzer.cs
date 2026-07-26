using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace uCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingGuidAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UCF001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Missing GUID on code-first type",
        messageFormat: "[{0}] on '{1}' is missing a Guid — use the code fixer to generate one",
        category: "uCodeFirst",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All code-first type attributes require a stable, unique GUID. Use the code fixer to generate one.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static readonly ImmutableHashSet<string> WatchedAttributes = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "DocumentType", "DocumentTypeAttribute",
        "ElementType", "ElementTypeAttribute",
        "CompositionType", "CompositionTypeAttribute",
        "DataType", "DataTypeAttribute",
        "SeedContent", "SeedContentAttribute");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;

        var attrName = attributeSyntax.Name switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            QualifiedNameSyntax q => q.Right.Identifier.Text,
            _ => null
        };

        if (attrName is null || !WatchedAttributes.Contains(attrName))
            return;

        // Only fire on class/interface declarations
        if (attributeSyntax.Parent?.Parent is not (ClassDeclarationSyntax or InterfaceDeclarationSyntax))
            return;

        var args = attributeSyntax.ArgumentList?.Arguments ?? default;

        // Look for a named argument Guid = "..."
        var guidArg = args.FirstOrDefault(a =>
            a.NameEquals?.Name.Identifier.Text == "Guid");

        if (guidArg is null)
        {
            // Guid property not set at all
            Report(context, attributeSyntax, attrName);
            return;
        }

        // Guid is set but empty
        if (guidArg.Expression is LiteralExpressionSyntax lit &&
            lit.Token.ValueText == string.Empty)
        {
            Report(context, attributeSyntax, attrName);
        }
    }

    private static void Report(SyntaxNodeAnalysisContext context, AttributeSyntax attr, string attrName)
    {
        var typeName = (attr.Parent?.Parent as MemberDeclarationSyntax) switch
        {
            ClassDeclarationSyntax c => c.Identifier.Text,
            InterfaceDeclarationSyntax i => i.Identifier.Text,
            _ => "?"
        };

        context.ReportDiagnostic(Diagnostic.Create(Rule, attr.GetLocation(), attrName, typeName));
    }
}
