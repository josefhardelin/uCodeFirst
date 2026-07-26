using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;

namespace uCodeFirst.Tests.DataTypes;

[TestFixture]
public class ContentPickerDataTypeTests
{
    [DocumentType(Name: "News Article", Guid = "31000000-0000-0000-0000-000000000001")]
    private sealed class NewsArticleFixture { }

    [DocumentType(Name: "Blog Post", Alias: "customBlogAlias", Guid = "31000000-0000-0000-0000-000000000002")]
    private sealed class BlogPostFixture { }

    private sealed class PlainPicker : ContentPickerDataType { }

    private sealed class FilteredPicker : ContentPickerDataType
    {
        public override Type[] AllowedContentTypes { get; } = [typeof(NewsArticleFixture), typeof(BlogPostFixture)];
    }

    [Test]
    public void BuildRecipe_WithoutAllowedContentTypes_OmitsFilterKey()
    {
        var recipe = new PlainPicker().BuildRecipe(Guid.NewGuid(), "Plain");

        Assert.That(recipe.ConfigData.ContainsKey("filter"), Is.False);
    }

    [Test]
    public void BuildRecipe_WithAllowedContentTypes_SetsFilterToCommaSeparatedAliases()
    {
        var recipe = new FilteredPicker().BuildRecipe(Guid.NewGuid(), "Filtered");

        // NewsArticleFixture has no explicit Alias, so it derives from the class name via
        // DocumentTypeScanner.ToAlias (lower-camel-case of the CLR type name); BlogPostFixture
        // has an explicit Alias which must win over any derived name.
        Assert.That(recipe.ConfigData["filter"], Is.EqualTo("newsArticleFixture,customBlogAlias"));
    }

    [Test]
    public void BuildRecipe_UsesUmbracoContentPickerEditorAlias()
    {
        var recipe = new PlainPicker().BuildRecipe(Guid.NewGuid(), "Plain");

        Assert.That(recipe.EditorAlias, Is.EqualTo("Umbraco.ContentPicker"));
        Assert.That(recipe.EditorUiAlias, Is.EqualTo("Umb.PropertyEditorUi.DocumentPicker"));
    }
}
