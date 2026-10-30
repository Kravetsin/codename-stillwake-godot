using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The tech window's words: every category and gate named, every refusal said.</summary>
public class TechTreeTextTests
{
    [Fact]
    public void NamesEveryCategoryAndGateAndSaysEveryRefusal()
    {
        Assert.All(Spec.Research.Categories, c => Assert.True(Table.ContainsKey($"research.category.{c.Id}"), c.Id));
        Assert.All(Spec.Research.Nodes.Where(n => n.Gate), g => Assert.True(Table.ContainsKey($"research.gate.{g.Id}"), g.Id));
        Assert.All(["noMachine", "bought", "locked", "noCredits", "gateOpen", "nothingToDeposit"], why => Assert.True(Table.ContainsKey($"research.{why}"), why));
    }

    [Fact]
    public void NamesEveryItemADepositAsks()
    {
        Assert.All(Spec.Research.Nodes.SelectMany(n => n.Deposit), d => Assert.True(Table.ContainsKey($"item.{d.Item}"), d.Item));
    }

    [Fact]
    public void NamesBothStationsAndWhatOpeningThemSays()
    {
        Assert.All(["techStation", "tradeStation"], kind =>
        {
            Assert.True(Table.ContainsKey($"tool.{kind}"), kind);
            Assert.True(Table.ContainsKey($"tool.{kind}.about"), kind);
            Assert.True(Table.ContainsKey($"use.open.{kind}"), kind);
        });
    }
}
