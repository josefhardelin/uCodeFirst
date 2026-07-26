using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;

namespace uCodeFirst.Tests.DataTypes;

[TestFixture]
public class MultiNodeTreePickerDataTypeTests
{
    [DocumentType(Name: "News Article", Guid = "30000000-0000-0000-0000-000000000001")]
    private sealed class NewsArticleFixture { }

    [DocumentType(Name: "Blog Post", Guid = "30000000-0000-0000-0000-000000000002")]
    private sealed class BlogPostFixture { }

    private sealed class PlainPicker : MultiNodeTreePickerDataType { }

    private sealed class DynamicRootPicker : MultiNodeTreePickerDataType
    {
        public override DynamicRootConfig? DynamicRoot { get; } = new()
        {
            Origin = DynamicRootOrigin.Site,
            QuerySteps =
            [
                new DynamicRootQueryStep
                {
                    Direction = DynamicRootQueryStepDirection.NearestDescendantOrSelf,
                    DocumentTypes = [typeof(NewsArticleFixture)]
                }
            ]
        };
    }

    private sealed class FilteredPicker : MultiNodeTreePickerDataType
    {
        public override Type[] AllowedContentTypes { get; } = [typeof(NewsArticleFixture), typeof(BlogPostFixture)];
    }

    private static IDictionary<string, object?> StartNode(IDictionary<string, object> config) =>
        (IDictionary<string, object?>)config["startNode"];

    [Test]
    public void BuildRecipe_WithoutDynamicRoot_LeavesDynamicRootNull()
    {
        var recipe = new PlainPicker().BuildRecipe(Guid.NewGuid(), "Plain");
        var startNode = StartNode(recipe.ConfigData);

        Assert.That(startNode["dynamicRoot"], Is.Null);
        Assert.That(startNode["id"], Is.Null);
    }

    [Test]
    public void BuildRecipe_WithDynamicRoot_PopulatesOriginAliasAndQuerySteps()
    {
        var recipe = new DynamicRootPicker().BuildRecipe(Guid.NewGuid(), "Dynamic");
        var startNode = StartNode(recipe.ConfigData);

        Assert.That(startNode["id"], Is.Null, "a fixed start node id and a dynamic root are mutually exclusive");

        var dynamicRoot = (IDictionary<string, object>)startNode["dynamicRoot"]!;
        Assert.That(dynamicRoot["originAlias"], Is.EqualTo("Site"));
        Assert.That(dynamicRoot["originKey"], Is.Null);

        var querySteps = (List<object>)dynamicRoot["querySteps"];
        Assert.That(querySteps, Has.Count.EqualTo(1));

        var step = (IDictionary<string, object>)querySteps[0];
        Assert.That(step["alias"], Is.EqualTo("NearestDescendantOrSelf"));
        var docTypeKeys = (List<Guid>)step["anyOfDocTypeKeys"];
        Assert.That(docTypeKeys, Is.EquivalentTo(new[] { Guid.Parse("30000000-0000-0000-0000-000000000001") }));
    }

    [Test]
    public void BuildRecipe_WithAllowedContentTypes_SetsFilterToCommaSeparatedAliases()
    {
        var recipe = new FilteredPicker().BuildRecipe(Guid.NewGuid(), "Filtered");

        Assert.That(recipe.ConfigData["filter"], Is.EqualTo("newsArticleFixture,blogPostFixture"));
    }

    [Test]
    public void BuildRecipe_WithoutAllowedContentTypes_OmitsFilterKey()
    {
        var recipe = new PlainPicker().BuildRecipe(Guid.NewGuid(), "Plain");

        Assert.That(recipe.ConfigData.ContainsKey("filter"), Is.False);
    }
}
