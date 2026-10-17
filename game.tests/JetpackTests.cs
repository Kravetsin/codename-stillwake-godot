using Stillwake.Game.Features.Crew;
using Xunit;
using Pack = Stillwake.Content.Jetpack;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The jetpack's flight and the slower swim (0047), in the hero controller.</summary>
public class JetpackTests
{
    private const int Unit = Hero.Unit;

    private static readonly HeroInput Worn = new(0, 0, false, Jetpack: true);
    private static readonly Point East = new(Unit, 0);

    /// <summary>A hero adrift in open space, far from the hull, on no tether.</summary>
    private static (Ship Ship, Hero Hero) Adrift()
    {
        var opened = HeroTests.Opened();
        return (opened, Hero.At(opened, 40 * opened.Width + 120));
    }

    private static double Speed(Hero h) => Math.Sqrt((double)h.Vx * h.Vx + (double)h.Vy * h.Vy);

    [Fact]
    public void SwimsSlowlyInSpaceAboutTwoCellsASecond()
    {
        Assert.Equal(30, Spec.Swim);
        Assert.InRange(Spec.Swim * 60 / (double)Unit, 1.7, 1.9);
        var (ship, h) = Adrift();
        Assert.Equal(h.X + Spec.Swim, h.Step(HeroTests.Right, ship).X);
    }

    [Fact]
    public void ThrustGainsSpeedTheWayItPointsToFullSpeedInAboutHalfASecond()
    {
        var (ship, h) = Adrift();
        var thrust = Worn with { Thrust = East };
        int steps = 0;
        for (; Speed(h) < Pack.Top && steps < 600; steps++) h = h.Step(thrust, ship);
        Assert.Equal((Pack.Top, 0), (h.Vx, h.Vy));
        Assert.InRange(steps, 25, 40);
        // Held on, it flies no faster.
        Assert.Equal(Pack.Top, h.Step(thrust, ship).Vx);
        // Down and to the left at once, along the unit vector the keys give.
        var diagonal = Hero.ThrustToward(-1, 1, h.Middle, null)!.Value;
        var turned = Adrift().Hero.Step(Worn with { Thrust = diagonal }, ship);
        Assert.Equal((-6, 6), (turned.Vx, turned.Vy));
    }

    [Fact]
    public void CoastsOnWithoutThrustSlowingToAStopOverSomeSevenCells()
    {
        var (ship, h) = Adrift();
        for (int i = 0; i < 60; i++) h = h.Step(Worn with { Thrust = East }, ship);
        int from = h.X, steps = 0;
        for (; h.Vx > 0 && steps < 600; steps++) h = h.Step(Worn, ship);
        Assert.Equal((0, 0), (h.Vx, h.Vy));
        Assert.InRange(h.X - from, 6 * Unit, 9 * Unit);
        Assert.InRange(steps, Pack.Top / Pack.Drag - 2, Pack.Top / Pack.Drag + 2);
    }

    [Fact]
    public void CoastingWithAKeyHeldSettlesAtTheSwimNotAtAStop()
    {
        var (ship, h) = Adrift();
        for (int i = 0; i < 60; i++) h = h.Step(Worn with { Thrust = East }, ship);
        for (int i = 0; i < 200; i++) h = h.Step(Worn with { X = 1 }, ship);
        Assert.Equal((Spec.Swim, 0), (h.Vx, h.Vy));
    }

    [Fact]
    public void ACoastIsStoppedByAWallAndAFallCarriedOutIsCutToTheTopSpeed()
    {
        var (ship, h) = Adrift();
        Assert.Equal((Pack.Top, 0), Hero.Flown(Spec.MaxFall, 0, Worn with { Thrust = East }));
        Assert.Equal((Pack.Top - Pack.Drag, 0), Hero.Flown(Pack.Top, 0, Worn));
        // Into the grid's edge: what the edge stops, the hero loses.
        for (int i = 0; i < 2000; i++) h = h.Step(Worn with { Thrust = new Point(0, -Unit) }, ship);
        Assert.Equal((0, Spec.Height), (h.Vy, h.Y));
    }

    [Fact]
    public void ThrustsWhereTheKeysPointElseTowardThePointer()
    {
        var middle = new Point(5000, 5000);
        Assert.Equal(new Point(Unit, 0), Hero.ThrustToward(1, 0, middle, (0, 0)));
        Assert.Equal(new Point(-707, -707), Hero.ThrustToward(-1, -1, middle, null));
        Assert.Equal(new Point(0, Unit), Hero.ThrustToward(0, 0, middle, (5, 9)));
        Assert.Equal(new Point(-600, 800), Hero.ThrustToward(0, 0, middle, (2, 9)));
        Assert.Null(Hero.ThrustToward(0, 0, middle, null));
        Assert.Null(Hero.ThrustToward(0, 0, middle, (5, 5)));
    }

    [Fact]
    public void TiesNoTetherWithAJetpackWornAndFliesPastWhereOneWouldHold()
    {
        var opened = HeroTests.Opened();
        var h = Hero.At(opened, HeroTests.Spawn);
        for (int i = 0; i < 3000 && h.X < (HeroTests.Wreck.Width - 3) * Unit; i++) h = h.Step(Worn with { X = 1, Thrust = East }, opened);
        Assert.Null(h.Anchor);
        Assert.True(h.X > HeroTests.Spawn % opened.Width * Unit + Spec.Tether + 20 * Unit);
        // Taken off out there, it swims on slowly, still on no tether: none is tied in open space.
        var off = h.Step(HeroTests.Right, opened);
        Assert.Null(off.Anchor);
        Assert.Equal(h.X + Spec.Swim, off.X);
    }

    [Fact]
    public void NeverExertsInSpaceAndSaysWhenTheSimsThrustDiffers()
    {
        var fast = new Hero(0, 0, Pack.Top, 0, false, 1);
        Assert.False(Report.Exerting(fast, inSpace: true));
        Assert.True(Report.Exerting(fast with { Vx = Spec.Run }, inSpace: false));
        var m = new Sim.Registry();
        var world = Sim.World.Create(1, Sim.Layout.Parse("S...."), m.All);
        Assert.Null(Report.ThrustOf(m.Crew, 0, true));
        world.Step([new Sim.Crew.Enter(1) { From = 0 }]);
        Assert.Equal(new Sim.Crew.Thrust(true), Report.ThrustOf(m.Crew, 0, true));
        Assert.Null(Report.ThrustOf(m.Crew, 0, false));
    }
}
