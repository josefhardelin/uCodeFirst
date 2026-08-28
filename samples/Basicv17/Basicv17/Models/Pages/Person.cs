using uCodeFirst;
using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace Basicv17.Models.Pages;

// Standalone page in its own right, but also composed into Author (see Author.cs) via plain C#
// inheritance rather than a [CompositionType] interface -- see that file for why.
[DocumentType("Person",
    Icon: ContentTypeIcon.People,
    Color: ContentTypeColor.Blue,
    Folder: "Pages",
    Guid = "0989bc0c-d2e6-461e-bdce-8fd76fdee4f3")]
[PublishedModel("person")]
public partial class Person : PublishedContentModel
{
    // protected, not private: classes that compose Person via inheritance (e.g. Author) reuse this
    // field for their own Value<T> getters instead of re-resolving IPublishedValueFallback themselves.
    protected readonly IPublishedValueFallback _publishedValueFallback;

    public Person(IPublishedContent content, IPublishedValueFallback fallback)
        : base(content, fallback) => _publishedValueFallback = fallback;

    [Group(Groups.Content, SortOrder: 0)]
    [TextString(Name = "Name", Mandatory = true)]
    public string? PersonName => this.Value<string>(_publishedValueFallback, "personName");

    [Group(Groups.Content, SortOrder: 1)]
    [TextString(Name = "Email")]
    public string? Email => this.Value<string>(_publishedValueFallback, "email");

    [Group(Groups.Content, SortOrder: 2)]
    [TextString(Name = "Phone")]
    public string? Phone => this.Value<string>(_publishedValueFallback, "phone");

    [Group(Groups.Content, SortOrder: 3)]
    [MediaPicker3(Name = "Photo")]
    public IPublishedContent? Photo => this.Value<IPublishedContent>(_publishedValueFallback, "photo");
}
