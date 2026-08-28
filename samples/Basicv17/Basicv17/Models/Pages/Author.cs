using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace Basicv17.Models.Pages;

// Composes Person's fields (Name, Email, Phone, Photo) via plain C# inheritance instead of a
// [CompositionType] interface. Since this project has no ModelsBuilder, every property needs a
// hand-written Value<T>(...) getter -- inheriting Person's class gives Author all of Person's
// already-written getters for free, where an interface composition would need each one
// re-implemented explicitly here (see Models/Compositions/ISeoComposition.cs and StartPage.cs for
// that pattern). The tradeoff: a class may only compose one other [DocumentType]/[ElementType] this
// way (C# single inheritance) -- [CompositionType] interfaces remain the normal, unlimited way to
// compose and can still be mixed in alongside a base class like this.
[DocumentType("Author",
    Icon: ContentTypeIcon.User,
    Color: ContentTypeColor.Blue,
    Folder: "Pages",
    Guid = "5002d95f-6029-4cba-96a9-ce4c6e322936")]
[PublishedModel("author")]
public partial class Author : Person
{
    public Author(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback)
    {
    }

    [Group(Groups.Content, SortOrder: 4)]
    [RichText(Name = "Bio")]
    public IHtmlEncodedString? Bio => this.Value<IHtmlEncodedString>(_publishedValueFallback, "bio");
}
