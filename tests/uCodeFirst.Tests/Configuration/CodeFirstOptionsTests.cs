using uCodeFirst.Configuration;
using Microsoft.Extensions.Configuration;

namespace uCodeFirst.Tests.Configuration;

public class CodeFirstOptionsTests
{
    [Fact]
    public void Defaults_WhenNoConfigSectionPresent_MatchTodaysBehavior()
    {
        var options = new CodeFirstOptions();

        Assert.True(options.Enabled);
        Assert.Equal(CodeFirstStrategy.NonDestructive, options.Strategy);
    }

    [Fact]
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

        Assert.False(options.Enabled);
        Assert.Equal(CodeFirstStrategy.Destructive, options.Strategy);
    }

    [Fact]
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

        Assert.True(options.Enabled);
        Assert.Equal(CodeFirstStrategy.Destructive, options.Strategy);
    }
}
