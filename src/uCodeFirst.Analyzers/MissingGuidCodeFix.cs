using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace uCodeFirst.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingGuidCodeFix)), Shared]
public sealed class MissingGuidCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [MissingGuidAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var attributeSyntax = node as AttributeSyntax ?? node.FirstAncestorOrSelf<AttributeSyntax>();
        if (attributeSyntax is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Generate stable GUID",
                createChangedDocument: ct => InsertGuidAsync(context.Document, attributeSyntax, ct),
                equivalenceKey: "GenerateGuid"),
            diagnostic);
    }

    private static async Task<Document> InsertGuidAsync(
        Document document,
        AttributeSyntax attribute,
        System.Threading.CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newGuid = Guid.NewGuid().ToString("D");

        var guidNameEquals = SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Guid"));
        var guidValue = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(newGuid));
        var newArg = SyntaxFactory.AttributeArgument(guidNameEquals, null, guidValue);

        AttributeSyntax newAttribute;
        var existingArgs = attribute.ArgumentList?.Arguments ?? default;

        // Replace existing empty Guid arg, or add new one
        var existingGuidArg = existingArgs.FirstOrDefault(a =>
            a.NameEquals?.Name.Identifier.Text == "Guid");

        if (existingGuidArg is not null)
        {
            var newArgs = existingArgs.Replace(existingGuidArg, newArg);
            newAttribute = attribute.WithArgumentList(attribute.ArgumentList!.WithArguments(newArgs));
        }
        else
        {
            var argList = attribute.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
            var newArgList = argList.AddArguments(newArg);
            newAttribute = attribute.WithArgumentList(newArgList);
        }

        var newRoot = root.ReplaceNode(attribute, newAttribute);
        return document.WithSyntaxRoot(newRoot);
    }
}
