using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The tech window's words: every tab and gate named, every refusal said.</summary>
public class TechTreeTextTests
{
    [Fact]
    public void NamesEveryTabAndGateAndSaysEveryRefusal()
    {
        Assert.True(Table.ContainsKey("research.tab.board"));
        Assert.All(Spec.Research.Tabs, t => Assert.True(Table.ContainsKey($"research.tab.{t.Id}"), t.Id));
        Assert.All(Spec.Research.Nodes.Where(n => n.Gate), g => Assert.True(Table.ContainsKey($"research.gate.{g.Id}"), g.Id));
        string[] market = ["order", "sell", "filled", "unlocks", "allFilled", "shelf.all", "shelf.upgrades", "shelf.blueprints", "sort", "kind.upgrade", "kind.blueprint",
            "nextOrder", "allBought", "owned", "boughtOf", "nothing", "waits", "boughtOut"];
        Assert.All([.. market.Select(m => "market." + m), "bought", "locked", "noCredits"], why => Assert.True(Table.ContainsKey($"research.{why}"), why));
    }

    [Fact]
    public void NamesEveryItemAnOrderAsks()
    {
        Assert.All(Spec.Research.Nodes.SelectMany(n => n.Order), d => Assert.True(Table.ContainsKey($"item.{d.Item}"), d.Item));
    }

    [Fact]
    public void NamesTheTradeStationAndWhatOpeningItSays()
    {
        Assert.True(Table.ContainsKey("tool.tradeStation"));
        Assert.True(Table.ContainsKey("tool.tradeStation.about"));
        Assert.True(Table.ContainsKey("use.open.tradeStation"));
    }
}
