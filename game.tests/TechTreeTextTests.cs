using Xunit;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The tech window's words (2026-10-30-trade-and-tech): every tier and branch named, every refusal said.</summary>
public class TechTreeTextTests
{
    [Fact]
    public void NamesEveryTierAndBranchAndSaysEveryRefusal()
    {
        Assert.All(Spec.Research.Tiers, tier =>
        {
            Assert.True(Table.ContainsKey($"research.tier.{tier.Id}"), tier.Id);
            Assert.All(tier.Branches, branch => Assert.True(Table.ContainsKey($"research.branch.{branch.Id}"), branch.Id));
        });
        Assert.All(["noMachine", "bought", "locked", "noCredits", "tierOpen", "nothingToDeposit"], why => Assert.True(Table.ContainsKey($"research.{why}"), why));
    }

    [Fact]
    public void NamesEveryItemADepositAsks()
    {
        Assert.All(Spec.Research.Tiers.SelectMany(t => t.Deposit), d => Assert.True(Table.ContainsKey($"item.{d.Item}"), d.Item));
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
