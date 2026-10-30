using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The tech window's words: every tab, branch and gate named, every refusal said.</summary>
public class TechTreeTextTests
{
    [Fact]
    public void NamesEveryTabBranchAndGateAndSaysEveryRefusal()
    {
        Assert.All(Spec.Research.Tabs, t => Assert.True(Table.ContainsKey($"research.tab.{t.Id}"), t.Id));
        Assert.All(Spec.Research.Tabs.SelectMany(t => t.Branches), b => Assert.True(Table.ContainsKey($"research.branch.{b}"), b));
        Assert.All(Spec.Research.Nodes.Where(n => n.Gate), g => Assert.True(Table.ContainsKey($"research.gate.{g.Id}"), g.Id));
        Assert.All(["bought", "locked", "noCredits", "gateOpen", "nothingToDeposit"], why => Assert.True(Table.ContainsKey($"research.{why}"), why));
    }

    [Fact]
    public void NamesEveryItemADepositAsks()
    {
        Assert.All(Spec.Research.Nodes.SelectMany(n => n.Deposit), d => Assert.True(Table.ContainsKey($"item.{d.Item}"), d.Item));
    }

    [Fact]
    public void NamesTheTradeStationAndWhatOpeningItSays()
    {
        Assert.True(Table.ContainsKey("tool.tradeStation"));
        Assert.True(Table.ContainsKey("tool.tradeStation.about"));
        Assert.True(Table.ContainsKey("use.open.tradeStation"));
    }
}
