using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content.Hero;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>The hero controller on a planet (0076): its lower gravity, and no floating anywhere, the open sky included.</summary>
public class PlanetHeroTests
{
    private const int Unit = Hero.Unit;
    private static readonly HeroInput Still = new(0, 0, false), Right = Still with { X = 1 }, Jump = Still with { Jump = true };

    /// <summary>
    /// A hall twelve cells tall, the hero at its left on the floor, a ledge <paramref name="high"/> cells high three
    /// columns to its right; the hall's world and the ship of it under <paramref name="rules"/>.
    /// </summary>
    private static (Ship Ship, Hero Hero) Ledge(int high, Surfaces.Kind rules)
    {
        var rows = new[] { "########" }.Concat(Enumerable.Repeat("#......#", 12)).Append("########").ToArray();
        for (int y = 13 - high; y <= 12; y++) rows[y] = "#...####";
        rows[12] = "#S." + rows[12][3..];
        var world = new TestWorld(Layout.Parse(rows));
        var ship = Ship.Of(world.M.Grid, world.M.Rooms, rules: rules);
        return (ship, Hero.At(ship, world.M.Crew.Spawn));
    }

    /// <summary>Where the feet end after a jump and a walk right: on the ledge's top, or still on the floor.</summary>
    private static int Landed(int high, Surfaces.Kind rules)
    {
        var (ship, hero) = Ledge(high, rules);
        hero = hero.Step(Jump, ship);
        for (int i = 0; i < 300; i++) hero = hero.Step(Right with { JumpHeld = true }, ship);
        return hero.Y;
    }

    [Fact]
    public void UnderThePlanetsLowerGravityAJumpGainsLessEachStep()
    {
        var (ship, hero) = Ledge(0, Surfaces.TestStrip);
        int fall = Surfaces.TestStrip.Fall(Spec.Gravity);
        Assert.Equal(fall, ship.Gravity);
        Assert.True(fall < Spec.Gravity);
        var up = hero.Step(Jump, ship);
        Assert.Equal(-Spec.Jump + fall, up.Vy);
        Assert.Equal(up.Vy + fall, up.Step(Still with { JumpHeld = true }, ship).Vy);
    }

    [Fact]
    public void OnThePlanetAJumpClearsSixCellsNotSevenWhereAboardItClearsThreeAndStepsUpOntoFour()
    {
        var ship = Surfaces.Ship;
        var planet = Surfaces.TestStrip;
        Assert.Equal((13 - 3) * Unit, Landed(3, ship));
        // Four with the ledge assist (0092): a held jump falls 46 short of it, inside the drawn foot's reach.
        Assert.Equal((13 - 4) * Unit, Landed(4, ship));
        Assert.Equal(13 * Unit, Landed(5, ship));
        Assert.Equal((13 - 6) * Unit, Landed(6, planet));
        Assert.Equal(13 * Unit, Landed(7, planet));
    }

    [Fact]
    public void OnThePlanetTheOpenSkyHoldsNoOneUpTheJetpackNeitherAndTheHeroLandsOnTheGround()
    {
        var world = new TestWorld("""
            ......
            .#S#..
            .###..
            """).Step(new OpenSurface(Surfaces.TestStrip.Id));
        var m = Registry.On(world.World, 1);
        var ship = Ship.Of(m.Grid, m.Rooms, rules: m.Rules);
        int width = m.Grid.Width, sky = 5 * width + 20;
        Assert.False(ship.Space(sky));
        Assert.True(Ship.Of(world.M.Grid, world.M.Rooms).Space(0));

        var falling = Hero.At(ship, sky);
        var landed = HeroTests.Settle(ship, falling, Still);
        Assert.Equal((falling.X, 30 * Unit), (landed.X, landed.Y));
        // Thrusting up with a jetpack worn lifts nothing: it falls all the same, untethered.
        var thrusting = HeroTests.Settle(ship, falling, Still with { Jetpack = true, Thrust = new Point(0, -Unit) });
        Assert.Equal((falling.X, 30 * Unit, null), (thrusting.X, thrusting.Y, thrusting.Anchor));
    }
}
