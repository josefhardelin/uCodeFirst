using uCodeFirst.Attributes;

namespace Basicv17.Models;

// Demonstrates Umbraco's master/parent template hierarchy. Each field's own literal value IS the
// template's alias — matched verbatim against [DocumentType(DefaultTemplate: ...)] — so there's no
// separate Alias to keep in sync. Layout has no Master and becomes a top-level template; StartPage
// declares Layout as its Master, so code-first creates it with `Layout = "_layout.cshtml";` wired
// into its content, matching StartPage's DefaultTemplate: Templates.StartPage (see Pages/StartPage.cs).
public static class Templates
{
    [Template]
    public const string Layout = "_Layout";

    [Template(Master = Layout)]
    public const string StartPage = "startPage";

    [Template]
    public const string NewsArticle = "newsArticle";
}
