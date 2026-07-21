using uCodeFirst.Attributes;

namespace Basicv17.Models;

// Code owns which languages exist. A language that doesn't exist yet is created (including
// IsDefault, set only at creation time); an already-existing language (e.g. the built-in en-US
// default from installation) has its IsMandatory/Fallback kept in sync with the code on every
// run, but its IsDefault status is left alone. The enum is the full language roster for the site
// (including any pre-existing ones you need as a Fallback target), not just "languages to add".
[Languages(DefaultLanguage: Lang.English)]
public enum Lang
{
    [Language(IsoCode: "en-US")]
    English,

    [Language(IsoCode: "sv-SE", Fallback = Lang.English)]
    Swedish,
}
