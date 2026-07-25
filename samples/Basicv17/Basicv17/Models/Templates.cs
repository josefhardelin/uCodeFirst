using uCodeFirst.Attributes;

namespace Basicv17.Models;

// Demonstrates Umbraco's master/parent template hierarchy. Unlike [Languages], there's no
// single-enum requirement for [Template] — any number of enums may carry [Template]-decorated
// members. Layout has no Master and becomes a top-level template; StartPage declares Layout as
// its Master, so code-first creates it with `Layout = "_layout.cshtml";` wired into its content,
// matching StartPage's DefaultTemplate: "startPage" (see Pages/StartPage.cs).
public enum Templates
{
    [Template(Alias: "_Layout")]
    Layout,

    [Template(Alias: "startPage", Master = Layout)]
    StartPage,
}
