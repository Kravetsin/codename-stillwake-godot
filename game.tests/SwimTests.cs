using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>Swimming in water (docs/plans/liquids.md, L-4): maps a character a cell, the water drawn beside them.</summary>
public class SwimTests
{
    private const int Unit = Hero.Unit;

    // A pool six cells deep under six of air, in a box of walls; a ladder up its left side.
    private const string Map = """
        ############
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        #H.........#
        ############
        """;

    private const string Pool = """
        ............
        ............
        ............
        ............
        ............
        ............
        ............
        .wwwwwwwwww.
        .wwwwwwwwww.
        .wwwwwwwwww.
        .wwwwwwwwww.
        .wwwwwwwwww.
        .wwwwwwwwww.
        ............
        """;

    /// <summary>The ship <paramref name="map"/> draws, its cells holding what <paramref name="water"/> gives them, read as it is now.</summary>
    private static Ship ShipOf(string map, Func<int, int> water)
    {
        var m = new Registry();
        World.Create(1, Layout.Parse(map), m.All);
        return Ship.Of(m.Grid, m.Rooms, liquid: water);
    }

    private static int[] Amounts(string water)
    {
        var l = Layout.Parse(Map).WithLiquids(water);
        return [.. l.Liquids!.Select(b => b & 15)];
    }

    private static Ship PoolShip() => ShipOf(Map, Amounts(Pool) is var a ? c => a[c] : null!);

    /// <summary>A hero with its feet at the bottom of row <paramref name="row"/>, its body over columns 5 and 6.</summary>
    private static Hero At(int row) => new(6 * Unit, (row + 1) * Unit, 0, 0, false, 1);

    private static readonly HeroInput Still = new(0, 0, false);

    [Fact]
    public void InWaterTheHeroSinksSlowlyToTheFloor()
    {
        var ship = PoolShip();
        var h = At(10).Step(Still, ship);
        Assert.Equal(Spec.WaterSink, h.Vy);
        h = HeroTests.Settle(ship, h, Still);
        Assert.Equal(13 * Unit, h.Y);
    }

    [Fact]
    public void AHeroFallingInSlowsToASink()
    {
        var ship = PoolShip();
        var h = HeroTests.Until(ship, At(6), Still, x => ship.Wet(x.BodyCell(ship.Width)));
        Assert.True(h.Vy > Spec.WaterSink, "it fell in fast");
        h = h.Step(Still, ship);
        Assert.Equal(Spec.WaterSink, h.Vy);
    }

    [Fact]
    public void UpSwimsToTheSurfaceAndBobsThereWithoutLeavingIt()
    {
        var ship = PoolShip();
        var up = Still with { Y = -1 };
        var h = At(12);
        int highest = h.Y;
        for (int i = 0; i < 600; i++)
        {
            h = h.Step(up, ship);
            highest = Math.Min(highest, h.Y);
        }
        // The middle comes up to the surface, row 7's top, and a little past it before it sinks back: never the feet out.
        Assert.InRange(highest - Hero.MiddleUp, 6 * Unit, 7 * Unit + Unit / 2);
        Assert.False(h.Climbing, "the ladder in the water is swum past");
    }

    [Fact]
    public void SpaceAtTheSurfaceJumpsOutOfTheWater()
    {
        var ship = PoolShip();
        var up = Still with { Y = -1 };
        var h = HeroTests.Until(ship, At(12), up, x => ship.Wet(x.BodyCell(ship.Width)) && !ship.Wet(x.BodyCell(ship.Width) - ship.Width));
        h = h.Step(Still with { Jump = true, JumpHeld = true }, ship);
        Assert.True(h.Vy < -Spec.WaterSwim, "it leapt");
        int highest = h.Y;
        for (int i = 0; i < 60; i++)
        {
            h = h.Step(Still with { JumpHeld = true }, ship);
            highest = Math.Min(highest, h.Y);
        }
        // The feet clear the surface, row 7's top, till the head meets the box's roof; swimming up alone they never do.
        Assert.True(highest < 7 * Unit, $"the feet stayed in the water: {highest}");
    }

    [Fact]
    public void WaterRisingRoundAStandingHeroIsSwum()
    {
        var amounts = new int[Amounts(Pool).Length];
        var ship = ShipOf(Map, c => amounts[c]);
        var up = Still with { Y = -1 };
        var h = HeroTests.Settle(ship, At(12), Still);
        Assert.Equal(h, h.Step(up, ship) with { Facing = h.Facing });
        Array.Copy(Amounts(Pool), amounts, amounts.Length);
        Assert.Equal(-Spec.WaterSwim, h.Step(up, ship).Vy);
    }

    [Fact]
    public void WaterToTheCeilingHasNoWayOut()
    {
        var full = string.Join("\n", Enumerable.Range(0, 14).Select(r => r == 0 || r == 13 ? new string('.', 12) : ".wwwwwwwwww."));
        var a = Amounts(full);
        var ship = ShipOf(Map, c => a[c]);
        var h = At(12);
        for (int i = 0; i < 300; i++) h = h.Step(Still with { Y = -1, Jump = i % 20 == 0, JumpHeld = true }, ship);
        Assert.True(ship.Wet(h.BodyCell(ship.Width)), "still in the water");
        Assert.Equal(1 * Unit + Spec.Height, h.Y);
    }
}
