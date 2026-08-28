using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace uCodeFirst.Analyzers;

/// <summary>
/// Flags a consumer type whose simple name collides with a type in a <c>uCodeFirst.*</c> namespace
/// the consumer also imports via <c>using</c>. C# binds an unqualified name to a same-namespace
/// declaration before a <c>using</c>-imported one, for every file sharing that namespace — so one
/// colliding declaration can silently rebind unrelated ": Xyz" base-class references elsewhere in
/// the same namespace to the wrong type (CS0509/CS0115 downstream).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ShadowedBaseTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UCF003";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Type shadows a uCodeFirst type in this namespace",
        messageFormat: "Type '{0}' shadows '{1}' in this namespace. Any ': {2}' elsewhere in '{3}' relying on 'using {4};' will now silently resolve to this type instead, which can cause confusing CS0509/CS0115 errors in unrelated files. Rename this type, or fully-qualify references to the library type.",
        category: "uCodeFirst",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type declared in a namespace takes precedence over a same-named type brought in by a using directive, for every file that shares that namespace. Naming a type after a uCodeFirst base or wrapper class can silently break unrelated subclasses elsewhere in the namespace.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var libraryTypesByName = CollectLibraryTypes(context.Compilation);
        if (libraryTypesByName.Count == 0)
            return;

        var importedNamespaces = CollectImportedNamespaces(context.Compilation, context.CancellationToken);
        if (importedNamespaces.Count == 0)
            return;

        context.RegisterSymbolAction(
            ctx => AnalyzeNamedType(ctx, libraryTypesByName, importedNamespaces),
            SymbolKind.NamedType);
    }

    private static ILookup<string, INamedTypeSymbol> CollectLibraryTypes(Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                CollectFromNamespace(assembly.GlobalNamespace, results);
        }

        return results.ToLookup(t => t.Name, StringComparer.Ordinal);
    }

    private static void CollectFromNamespace(INamespaceSymbol ns, List<INamedTypeSymbol> results)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                CollectFromNamespace(childNamespace, results);
            }
            else if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } type
                     && IsUCodeFirstNamespace(type.ContainingNamespace))
            {
                results.Add(type);
            }
        }
    }

    private static bool IsUCodeFirstNamespace(INamespaceSymbol? ns)
    {
        for (var current = ns; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
        {
            if (current.Name == "uCodeFirst")
                return true;
        }

        return false;
    }

    private static ImmutableHashSet<string> CollectImportedNamespaces(
        Compilation compilation, System.Threading.CancellationToken cancellationToken)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetCompilationUnitRoot(cancellationToken);
            CollectUsings(root.Usings, builder);
            foreach (var ns in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
                CollectUsings(ns.Usings, builder);
        }

        return builder.ToImmutable();
    }

    private static void CollectUsings(SyntaxList<UsingDirectiveSyntax> usings, ImmutableHashSet<string>.Builder builder)
    {
        foreach (var usingDirective in usings)
        {
            if (usingDirective.Alias is not null || usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                continue;

            if (usingDirective.Name is { } name)
                builder.Add(name.ToString());
        }
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        ILookup<string, INamedTypeSymbol> libraryTypes,
        ImmutableHashSet<string> importedNamespaces)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        if (typeSymbol.DeclaringSyntaxReferences.IsEmpty)
            return;

        var shadowed = libraryTypes[typeSymbol.Name]
            .FirstOrDefault(candidate => importedNamespaces.Contains(candidate.ContainingNamespace.ToDisplayString()));

        if (shadowed is null)
            return;

        foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(context.CancellationToken);
            var location = syntax is BaseTypeDeclarationSyntax typeDeclaration
                ? typeDeclaration.Identifier.GetLocation()
                : syntax.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                location,
                typeSymbol.ToDisplayString(),
                shadowed.ToDisplayString(),
                shadowed.Name,
                typeSymbol.ContainingNamespace.ToDisplayString(),
                shadowed.ContainingNamespace.ToDisplayString()));
        }
    }
}
