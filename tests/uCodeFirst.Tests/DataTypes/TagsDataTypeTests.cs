using uCodeFirst.DataTypes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.Tests.DataTypes;

[TestFixture]
public class TagsDataTypeTests
{
    private sealed class CustomDelimiterTags : TagsDataType
    {
        public override string Group { get; } = "seo";
        public override string StorageType { get; } = "Csv";
        public override char Delimiter { get; } = '|';
    }

    [Test]
    public void BuildRecipe_Default_UsesCommaDelimiterAndDefaultGroupAndStorageType()
    {
        var recipe = new Tags().BuildRecipe(Guid.NewGuid(), "Tags");

        Assert.That(recipe.EditorAlias, Is.EqualTo("Umbraco.Tags"));
        Assert.That(recipe.EditorUiAlias, Is.EqualTo("Umb.PropertyEditorUi.Tags"));
        Assert.That(recipe.ConfigData["group"], Is.EqualTo("default"));
        Assert.That(recipe.ConfigData["storageType"], Is.EqualTo("Json"));
        Assert.That(recipe.ConfigData["delimiter"], Is.EqualTo(','));
    }

    [Test]
    public void BuildRecipe_WithOverrides_UsesOverriddenGroupStorageTypeAndDelimiter()
    {
        var recipe = new CustomDelimiterTags().BuildRecipe(Guid.NewGuid(), "Custom Tags");

        Assert.That(recipe.ConfigData["group"], Is.EqualTo("seo"));
        Assert.That(recipe.ConfigData["storageType"], Is.EqualTo("Csv"));
        Assert.That(recipe.ConfigData["delimiter"], Is.EqualTo('|'));
    }
}
