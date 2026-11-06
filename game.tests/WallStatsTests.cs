using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// A wall's numbers on its build card (2026-10-29-meteor-wave): its leak a minute by the leak sign (tip-signs), and how it stands
/// up to the meteors here.
/// </summary>
public class WallStatsTests
{
    private static readonly Meteors.Shower Start = Meteors.StartingSpace;

    private static TipLine Leak(string rate) => new($"−{rate}/min a cell", Sign: Sign.Leak);
    private static TipLine Line(string text) => new(text);

    [Fact]
    public void GiveScrapsAndIronsLeakAndTheShareOfTheStartingSpacesMeteorsThatBreakThem()
    {
        Assert.Equal([Leak("9"), Line("Meteor resistance 2: 100% of meteors here break it")], WallStats.Of(Materials.Get("scrap"), back: false, Start));
        Assert.Equal([Leak("7.2"), Line("Meteor resistance 5: 20% of meteors here break it")], WallStats.Of(Materials.Get("iron"), back: false, Start));
    }

    [Fact]
    public void GiveABackWallItsStrengthAloneAndAPlaceWithoutMeteorsItsLeakAlone()
    {
        Assert.Equal([Leak("9"), Line("Meteor resistance 2")], WallStats.Of(Materials.Get("scrap"), back: true, Start));
        Assert.Equal([Leak("36")], WallStats.Of(Materials.Get("soil"), back: false, shower: null));
    }

    [Fact]
    public void GiveStoneArmourThatLeaksAndTheSealsNoArmourAndNoLeak()
    {
        Assert.Equal([Leak("27"), Line("Meteor resistance 4: 40% of meteors here break it")], WallStats.Of(Materials.Get("stone"), back: false, Start));
        foreach (var seal in new[] { "glass", "mica", "hide" })
            Assert.Equal([Leak("1.8"), Line("Meteor resistance 1: 100% of meteors here break it")], WallStats.Of(Materials.Get(seal), back: false, Start));
    }

    [Fact]
    public void CountTheShareOfMeteorsByTheirWeights()
    {
        var shower = Start with { Powers = [(1, 1), (3, 3)] };
        Assert.Equal((100, 75, 0), (WallStats.BreakingShare(1, shower), WallStats.BreakingShare(2, shower), WallStats.BreakingShare(4, shower)));
    }
}
