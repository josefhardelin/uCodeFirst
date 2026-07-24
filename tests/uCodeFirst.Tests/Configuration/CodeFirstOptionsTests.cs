using uCodeFirst.Configuration;
using Microsoft.Extensions.Configuration;

namespace uCodeFirst.Tests.Configuration;

[TestFixture]
public class CodeFirstOptionsTests
{
    [Test]
    public void Defaults_WhenNoConfigSectionPresent_MatchTodaysBehavior()
    {
        var options = new CodeFirstOptions();

        Assert.That(options.Enabled, Is.True);
        Assert.That(options.Strategy, Is.EqualTo(CodeFirstStrategy.NonDestructive));
    }

    [Test]
    public void Binds_FromUCodeFirstSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["uCodeFirst:Enabled"] = "false",
                ["uCodeFirst:Strategy"] = "Destructive",
            })
            .Build();

        var options = new CodeFirstOptions();
        config.GetSection("uCodeFirst").Bind(options);

        Assert.That(options.Enabled, Is.False);
        Assert.That(options.Strategy, Is.EqualTo(CodeFirstStrategy.Destructive));
    }

    [Test]
    public void Binds_PartialSection_KeepsUnspecifiedDefaults()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["uCodeFirst:Strategy"] = "Destructive",
            })
            .Build();

        var options = new CodeFirstOptions();
        config.GetSection("uCodeFirst").Bind(options);

        Assert.That(options.Enabled, Is.True);
        Assert.That(options.Strategy, Is.EqualTo(CodeFirstStrategy.Destructive));
    }
}
