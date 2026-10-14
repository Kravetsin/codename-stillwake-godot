using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// Aim and reach: the tests of apps/game/src/features/crew/aim.test.ts whose modules are ported, doors
/// and valves; tanks, plans, crates and debris come with theirs.
/// </summary>
public class AimTests
{
    private const int Unit = Hero.Unit;

    private static (Registry M, World World) WorldFrom(Layout layout)
    {
        var m = new Registry();
        return (m, World.Create(1, layout, m.All));
    }

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero HeroOn(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (x + 0.5, y + 0.5);

    private static readonly Registry Corridor = WorldFrom(Layout.Parse("""
        ########
        #..D.v.#
        ########
        """)).M;

    private static int At(int x, int y) => y * Corridor.Grid.Width + x;

    [Fact]
    public void OffersToToggleADoorOrAValveUnderThePointer()
    {
        var hero = HeroOn(2, 1);
        Assert.Equal(new Aim(At(3, 1), true, new Use(new ToggleDoor(At(3, 1)), null)), Aiming.AimAt(Corridor.Grid, hero, Centre(3, 1)));
        Assert.Null(Aiming.AimAt(Corridor.Grid, hero, Centre(4, 1))?.Press);
        Assert.Equal(new ToggleValve(At(5, 1)), Aiming.AimAt(Corridor.Grid, HeroOn(4, 1), Centre(5, 1))?.Press?.Command);
    }

    [Fact]
    public void ReachesCellsWhoseMiddleIsAtMostTheReachFromTheMiddleOfTheBody()
    {
        var open = WorldFrom(Layout.Parse("""
            ..........
            ..........
            ..........
            """)).M.Grid;
        var hero = HeroOn(1, 1);
        const int cellsAway = Spec.Reach / Unit;
        Assert.True(Aiming.AimAt(open, hero, Centre(1 + cellsAway, 1))?.InReach);
        Assert.False(Aiming.AimAt(open, hero, Centre(2 + cellsAway, 1))?.InReach);
        Assert.False(Aiming.AimAt(open, hero, Centre(1 + cellsAway, 2))?.InReach);
    }

    [Fact]
    public void AimsAtNothingOffTheGrid()
    {
        var hero = HeroOn(2, 1);
        Assert.Null(Aiming.AimAt(Corridor.Grid, hero, (-0.5, 1)));
        Assert.Null(Aiming.AimAt(Corridor.Grid, hero, (2, 3.2)));
        Assert.Null(Aiming.AimAt(Corridor.Grid, hero, (Corridor.Grid.Width + 0.5, 1)));
    }

    [Fact]
    public void KeepsTheHeroBehindClosedDoorsUntilTheyAreToggledThroughTheSession()
    {
        var (m, world) = WorldFrom(HeroTests.Wreck);
        var (host, client) = LocalSession.Create(world);
        Ship ShipNow() => Ship.Of(m.Grid, m.Rooms);
        Hero Run(Hero hero, HeroInput input, int steps)
        {
            for (int i = 0; i < steps; i++) hero = hero.Step(input, ShipNow());
            return hero;
        }
        void Use(Hero hero, int x, int y)
        {
            var aim = Aiming.AimAt(m.Grid, hero, Centre(x, y));
            Assert.True(aim?.InReach);
            if (aim?.Press is { } press) client.Send(press.Command);
            host.Advance();
            client.Update();
        }

        // Along room A, where the hero wakes: the closed doorway to room B stops the hero, until opened.
        var h = Run(Hero.At(ShipNow(), HeroTests.Spawn), HeroTests.Left, 120);
        Assert.Equal(46 * Unit + Spec.Width / 2, h.X);
        Use(h, 45, 22);
        h = Run(h, HeroTests.Left, 300);
        // Into room B, torn open to space, as far as the tether from its doorway lets the hero.
        Assert.Equal(45 * Unit + Unit / 2 - Spec.Tether, h.X);

        // Back the other way, the airlock's inner doorway stops the hero, until opened.
        h = Run(h, HeroTests.Right, 400);
        Assert.Equal(54 * Unit - Spec.Width / 2, h.X);
        Use(h, 54, 22);
        h = Run(h, HeroTests.Right, 60);
        Assert.Equal(57 * Unit - Spec.Width / 2, h.X);
    }

    private static readonly ToggleDoor Door = new(3);
    private static readonly Aim OnDoor = new(3, true, new Use(Door, null));

    [Fact]
    public void TogglesADoorOnceOnAPressAndNothingWhileEStaysHeld()
    {
        Assert.Equal(Door, Aiming.UseOf(OnDoor, pressed: true));
        Assert.Null(Aiming.UseOf(OnDoor, pressed: false));
    }

    [Fact]
    public void SendsNothingOutOfReach() => Assert.Null(Aiming.UseOf(OnDoor with { InReach = false }, pressed: true));

    [Fact]
    public void PressesNothingTheSimWouldRefuse() =>
        Assert.Null(Aiming.UseOf(OnDoor with { Press = new Use(Door, "atmosphere.tankFull") }, pressed: true));
}
