using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Tests.HeroTests;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// Hauling in along the tether (tether-haul): Shift held afloat with no jetpack pulls the body straight toward where the tether is
/// tied at <c>Hero.Haul</c> a step, once Shift has been let go since it was tied.
/// </summary>
public class TetherHaulTests
{
    private const int Unit = Hero.Unit;
    private static readonly HeroInput Haul = Still with { Sprint = true };
    private static readonly HeroInput Up = Still with { Y = -1 };

    private static double Far(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static double ReachOf(Hero h) => h.Anchor is Point a
        ? Math.Sqrt(Math.Pow(h.Middle.X - a.X, 2) + Math.Pow(h.Middle.Y - a.Y, 2))
        : 0;

    /// <summary>The wreck's airlock opened, the hero swum out of it to the tether's end.</summary>
    private static (Ship Ship, Hero Hero) Taut()
    {
        var opened = Opened();
        var outside = Until(opened, Hero.At(opened, Spawn), Right, h => h.Anchor != null);
        return (opened, Settle(opened, outside, Right));
    }

    [Fact]
    public void HaulsInAtItsSpeedTheKeysAsideAndComesAboardUntied()
    {
        var (ship, taut) = Taut();
        Assert.True(taut.HaulArmed);
        Assert.True(ReachOf(taut) > Spec.Tether - Spec.Swim);
        // Shift held, pushing away: hauled in anyway, a whole haul a step, facing where it is tied.
        var h = taut.Step(Haul with { X = 1 }, ship);
        Assert.InRange(ReachOf(taut) - ReachOf(h), Spec.Haul - 2, Spec.Haul + 2);
        Assert.Equal(-1, h.Facing);
        // On, Shift held throughout: the latch holds over the haul's steps, and the hero comes aboard, the tether off, in two seconds.
        int steps = 1;
        while (h.Anchor != null)
        {
            var next = h.Step(Haul, ship);
            Assert.True(ReachOf(next) < ReachOf(h) || next.Anchor == null, $"step {steps}: {ReachOf(h)} to {ReachOf(next)}");
            h = next;
            Assert.True(++steps <= 2 * 60 + 1, $"still out after {steps} steps at {h}");
        }
        Assert.False(ship.Space(h.BodyCell(ship.Width)) && !ship.Hull(h.BodyCell(ship.Width)));
        Assert.False(h.HaulArmed);
    }

    [Fact]
    public void LettingGoOfShiftStopsTheHaul()
    {
        var (ship, taut) = Taut();
        var hauled = taut.Step(Haul, ship).Step(Haul, ship);
        var stopped = hauled.Step(Still, ship);
        Assert.Equal((hauled.X, hauled.Y), (stopped.X, stopped.Y));
    }

    [Fact]
    public void AHeroRunOutWithShiftHeldIsNotHauledBackInTillShiftIsLetGoAndPressed()
    {
        var opened = Opened();
        var run = Still with { X = 1, Sprint = true };
        var outside = Until(opened, Hero.At(opened, Spawn), run, h => h.Anchor != null);
        Assert.False(outside.HaulArmed);
        // Running on with Shift held, it swims out, not back.
        var on = outside;
        for (int i = 0; i < 30; i++) on = on.Step(run, opened);
        Assert.True(ReachOf(on) > ReachOf(outside));
        Assert.False(on.Step(Haul, opened).HaulArmed);
        Assert.Equal((on.X, on.Y), (on.Step(Haul, opened) is var held ? (held.X, held.Y) : default));
        // Shift let go for a step, then pressed: hauled.
        var armed = on.Step(Still, opened);
        Assert.True(armed.HaulArmed);
        Assert.True(ReachOf(armed.Step(Haul, opened)) < ReachOf(armed) - Spec.Swim);
    }

    [Fact]
    public void TheGrapplesPullWinsOverAHaul()
    {
        var (ship, taut) = Taut();
        var hook = new Point(taut.Middle.X + 5 * Unit, taut.Middle.Y - 5 * Unit);
        var pulled = taut.Step(Haul with { Pull = hook, PullBy = Hero.GrapplePullStep }, ship);
        Assert.True(pulled.Y < taut.Y);
        Assert.True(ReachOf(pulled) <= Spec.Tether);
    }

    [Fact]
    public void WithAJetpackWornShiftThrustsAndNothingHauls()
    {
        var (ship, taut) = Taut();
        var flown = taut.Step(Haul with { Jetpack = true, Thrust = new Point(Unit, 0) }, ship);
        Assert.Null(flown.Anchor);
        Assert.True(flown.X >= taut.X);
    }

    [Fact]
    public void AKickIsTakenByTheHandsOnTheLine()
    {
        var (ship, taut) = Taut();
        var kicked = taut.Kicked(300, 0, ship);
        var hauled = kicked.Step(Haul, ship);
        Assert.Equal((0, 0), (hauled.Kx, hauled.Ky));
        Assert.True(hauled.X < taut.X);
    }

    /// <summary>A room whose door opens right onto open space, room above it and on its left; inside, where a hero stands.</summary>
    private static (Ship Ship, Hero Inside) Room() => (ShipFrom(new[]
    {
        "                    ",
        "                    ",
        "                    ",
        "                    ",
        "                    ",
        "        ######      ",
        "        #....d      ",
        "        #....d      ",
        "        #....d      ",
        "        ######      ",
        "                    ",
        "                    ",
        "                    ",
    }), new Hero(10 * Block, 9 * Block, 0, 0, false, 1));

    [Fact]
    public void FromOverTheDoorItIsHauledRoundTheLipAndIn()
    {
        var (ship, inside) = Room();
        var out_ = Until(ship, inside, Right, h => h.Anchor != null);
        // Swum out, up past the roof's edge and back over it: straight down to the anchor is through the roof.
        var right = Until(ship, out_, Right, h => h.X > 15 * Block);
        var up = Until(ship, right, Up, h => h.Y < 5 * Block);
        var over = Until(ship, up, Left, h => h.X < 13 * Block);
        Assert.NotNull(over.Wraps);
        var hauled = Until(ship, over, Haul, h => h.Anchor == null);
        Assert.False(ship.Space(hauled.BodyCell(ship.Width)) && !ship.Hull(hauled.BodyCell(ship.Width)));
    }

    [Fact]
    public void RoundTheRoomItIsHauledBackTheWayItWent()
    {
        var (ship, inside) = Room();
        var out_ = Until(ship, inside, Right, h => h.Anchor != null);
        var up = Until(ship, Until(ship, out_, Right, h => h.X > 15 * Block), Up, h => h.Y < 5 * Block);
        var over = Until(ship, up, Still with { X = -1 }, h => h.X < 8 * Block - Spec.Width);
        var behind = Until(ship, over, Still with { Y = 1 }, h => h.Y >= 9 * Block);
        Assert.NotNull(behind.Wraps);
        // Hauled, it goes back up over the roof and in by the door, never stopping at the room's left wall.
        int steps = 0;
        var h = behind;
        while (h.Anchor != null)
        {
            var next = h.Step(Haul, ship);
            Assert.True((next.X, next.Y) != (h.X, h.Y), $"stuck at step {steps}: {h}");
            h = next;
            Assert.True(++steps < 60 * 5, $"still out at {h}");
        }
    }

    [Fact]
    public void WoundFurtherThanItKeepsBendsTheHaulStopsAtTheHullAndASwimFreesIt()
    {
        var (ship, inside) = Room();
        var out_ = Until(ship, inside, Right, h => h.Anchor != null);
        // Put behind the room with no bends kept, as if wound past the most it keeps: the haul presses it flush to the wall.
        var behind = new Hero(8 * Block - Spec.Width / 2, 9 * Block, 0, 0, false, 1, out_.Anchor, HaulArmed: true);
        var stuck = Settle(ship, behind, Haul);
        Assert.Equal(behind.X, stuck.X);
        Assert.NotNull(stuck.Anchor);
        var swum = stuck.Step(Up, ship);
        Assert.True(swum.Y < stuck.Y);
    }

    private static readonly Gen<List<(HeroInput, int)>> Moves =
        (from x in Gen.Elements(-1, 0, 1)
         from y in Gen.Elements(-1, 0, 1)
         from sprint in Gen.Elements(false, false, true)
         from steps in Gen.Choose(1, 120)
         select (new HeroInput(x, y, false, Sprint: sprint), steps)).ListOf().Select(l => l.Take(20).ToList());

    [Property(MaxTest = 60)]
    public Property NeverPastTheTetherAndAHaulNeverTakesTheBodyFurtherFromWhereItIsHauled()
    {
        var (ship, _) = Taut();
        var outside = Until(ship, Hero.At(ship, Spawn), Right, h => h.Anchor != null);
        return Prop.ForAll(Moves.ToArbitrary(), list =>
        {
            var h = outside;
            foreach (var (keys, steps) in list)
                for (int i = 0; i < steps; i++)
                {
                    var next = h.Step(keys, ship);
                    if (ReachOf(next) > Spec.Tether) return false;
                    // Hauled, the body never gets further from where it is hauled to: the last bend, or the anchor.
                    if (keys.Sprint && h.HaulArmed && next.Anchor != null && (h.Wraps?.At ?? h.Anchor) is Point to
                        && Far(next.Middle, to) > Far(h.Middle, to)) return false;
                    h = next;
                }
            return true;
        });
    }
}
