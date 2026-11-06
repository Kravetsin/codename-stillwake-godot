using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The chips over a hero's head (2026-11-06-head-tally): merging, fading, stacking and the anchor that never shakes.</summary>
public class HeadTallyTests
{
    private const double Frame = 1000.0 / 60;

    [Fact]
    public void ARepeatWhileAChipShowsAddsToItAndBumpsItPastItsTimeANewOneOpens()
    {
        var tally = new HeadTally();
        tally.Add("ore", 1);
        tally.Update(HeadTally.HoldMs + HeadTally.FadeMs / 2, 0, 0);
        tally.Add("ore", 2);
        var chip = Assert.Single(tally.Chips);
        Assert.Equal(3, chip.Count);
        Assert.Equal(1, tally.Bump(chip));
        Assert.Equal(1, tally.Alpha(chip));
        tally.Update(HeadTally.HoldMs + HeadTally.FadeMs, 0, 0);
        Assert.Empty(tally.Chips);
        tally.Add("ore", 1);
        Assert.Equal(1, Assert.Single(tally.Chips).Count);
    }

    [Fact]
    public void FadesInHoldsThenFadesOut()
    {
        var tally = new HeadTally();
        tally.Add("ore", 1);
        var chip = tally.Chips[0];
        Assert.Equal(0, tally.Alpha(chip));
        tally.Update(HeadTally.HoldMs - 1, 0, 0);
        Assert.Equal(1, tally.Alpha(chip));
        tally.Update(1 + HeadTally.FadeMs / 2, 0, 0);
        Assert.Equal(0.5, tally.Alpha(chip), 3);
    }

    [Fact]
    public void StacksItemsNewestLowestAndAGrowingChipComesDownToTheBottom()
    {
        var tally = new HeadTally();
        tally.Add("ore", 1);
        tally.Add("scrap", 3);
        Settle(tally, 0, 0);
        Assert.Equal(["ore", "scrap"], tally.Chips.Select(c => c.Key));
        Assert.Equal([1, 0], tally.Chips.Select(c => Math.Round(c.Rise, 3)));
        tally.Add("ore", 1);
        Settle(tally, 0, 0);
        Assert.Equal(["scrap", "ore"], tally.Chips.Select(c => c.Key));
        Assert.Equal(0, Math.Round(tally.Chips[1].Rise, 3));
    }

    [Fact]
    public void TheAnchorStandsOnTheHeadWithNoChipsAndReachesAStillOneWithoutOvershoot()
    {
        var tally = new HeadTally();
        tally.Update(Frame, 5, -3);
        Assert.Equal((5.0, -3.0), tally.Anchor);
        tally.Add("ore", 1);
        tally.Update(Frame, 5, -3);
        double before = tally.Anchor.X;
        for (int i = 0; i < 60; i++)
        {
            tally.Update(Frame, 9, -3);
            Assert.True(tally.Anchor.X <= 9 + 1e-9, $"overshot to {tally.Anchor.X}");
            Assert.True(tally.Anchor.X >= before - 1e-9, "went back");
            before = tally.Anchor.X;
        }
        Assert.Equal(9, tally.Anchor.X, 1);
    }

    [Fact]
    public void TrailsAWalkingHeroSmoothlyWithinTwoCellsWhereTheHeadBobs()
    {
        var tally = new HeadTally();
        tally.Add("ore", 1);
        double x = 0, lastStep = 0;
        tally.Update(Frame, x, 0);
        for (int i = 0; i < 90; i++)
        {
            // A walk: Hero.Walk thousandths of a cell each 60 Hz step.
            x += Content.Hero.Walk / 1000.0;
            // The head bobs a quarter cell with each stride; the anchor must not.
            double head = 0.25 * Math.Sin(i * 0.7);
            var (ax, ay) = (tally.Anchor.X, tally.Anchor.Y);
            tally.Update(Frame, x, head);
            Assert.InRange(x - tally.Anchor.X, -1e-9, 2);
            if (i > 30) Assert.InRange(Math.Abs(tally.Anchor.Y - ay), 0, 0.05);
            if (i > 30) Assert.InRange(tally.Anchor.X - ax - lastStep, -0.01, 0.01);
            lastStep = tally.Anchor.X - ax;
        }
    }

    private static void Settle(HeadTally tally, double x, double y)
    {
        for (int i = 0; i < 60; i++) tally.Update(Frame, x, y);
    }
}
