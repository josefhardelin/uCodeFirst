using uCodeFirst.Attributes;

namespace Basicv17.Models;

// Code owns which languages exist and their default/mandatory/fallback config at creation time
// only — an already-existing language (e.g. the built-in en-US default from installation) is
// never updated, only ensured to exist. The enum is the full language roster for the site
// (including any pre-existing ones you need as a Fallback target), not just "languages to add".
[Languages(DefaultLanguage: Lang.English)]
public enum Lang
{
    [Language(IsoCode: "en-US")]
    English,

    [Language(IsoCode: "sv-SE", Fallback = Lang.English)]
    Swedish,
}
