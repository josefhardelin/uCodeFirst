using uCodeFirst.Attributes;
using Basicv17.Models.Pages;

namespace Basicv17.Models.Seeds;

// Deterministic-GUID singleton "Home" node — an empty stub content instance of StartPage, created
// once at startup so other code-first config (e.g. a future MultiNodeTreePicker dynamic-root ByKey
// origin — roadmap #2) has a stable node to point at. No property values are ever seeded here; that's
// a distinct, still-open roadmap item (source-generated typed builder + its own pre-flight validation
// for mandatory/type checks). Apply [SeedContent] to a plain marker class with no members.
[SeedContent(DocumentType: typeof(StartPage), Name: "Home", Guid = "b2c3d4e5-f6a7-8901-bcde-f12345678901")]
public sealed class HomePageSeed { }
