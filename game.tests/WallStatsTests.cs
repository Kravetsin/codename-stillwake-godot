using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A wall's numbers on its build card (2026-10-29-meteor-wave): its leak a minute, and how it stands up to the meteors here.</summary>
public class WallStatsTests
{
    private static readonly Meteors.Shower Start = Meteors.StartingSpace;

    [Fact]
    public void GiveScrapsAndIronsLeakAndTheShareOfTheStartingSpacesMeteorsThatBreakThem()
    {
        Assert.Equal(["Leaks 3 O₂ a minute through each cell", "Meteor resistance 2: 100% of meteors here break it"],
            WallStats.Of(Materials.Get("scrap"), back: false, Start));
        Assert.Equal(["Leaks 2.4 O₂ a minute through each cell", "Meteor resistance 5: 20% of meteors here break it"],
            WallStats.Of(Materials.Get("iron"), back: false, Start));
    }

    [Fact]
    public void GiveABackWallItsStrengthAloneAndAPlaceWithoutMeteorsItsLeakAlone()
    {
        Assert.Equal(["Leaks 3 O₂ a minute through each cell", "Meteor resistance 2"], WallStats.Of(Materials.Get("scrap"), back: true, Start));
        Assert.Equal(["Leaks 12 O₂ a minute through each cell"], WallStats.Of(Materials.Get("soil"), back: false, shower: null));
    }

    [Fact]
    public void GiveStoneArmourThatLeaksAndTheSealsNoArmourAndNoLeak()
    {
        Assert.Equal(["Leaks 9 O₂ a minute through each cell", "Meteor resistance 4: 40% of meteors here break it"],
            WallStats.Of(Materials.Get("stone"), back: false, Start));
        foreach (var seal in new[] { "glass", "mica", "hide" })
            Assert.Equal(["Leaks 0.6 O₂ a minute through each cell", "Meteor resistance 1: 100% of meteors here break it"],
                WallStats.Of(Materials.Get(seal), back: false, Start));
    }

    [Fact]
    public void CountTheShareOfMeteorsByTheirWeights()
    {
        var shower = Start with { Powers = [(1, 1), (3, 3)] };
        Assert.Equal((100, 75, 0), (WallStats.BreakingShare(1, shower), WallStats.BreakingShare(2, shower), WallStats.BreakingShare(4, shower)));
    }
}
