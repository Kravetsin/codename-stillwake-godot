using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content.Hero;
using Structure = Stillwake.Content.Structure;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>
/// Climbing out of a planet's deep by the harpoon (grapple-climbs): the hero and its harpoon stepped together as the
/// hero's view steps them, a click each time it hangs, the barrel at the shaft's wall high up; Space at the top jumps it
/// off the line onto the ground.
/// </summary>
public class GrappleClimbTests
{
    private const int Unit = Hero.Unit;

    /// <summary>The shaft's depth under the ground, its columns, the ground's row, and how high up the wall each shot aims.</summary>
    private const int Deep = 800, Left = 3, Right = 5, Ground = 20, Aim = 36 * Unit;

    private static readonly int Width = Right + 4, Height = Ground + Deep + 1;

    /// <summary>A 3-wide shaft <see cref="Deep"/> cells down from the ground at row <see cref="Ground"/>, under a planet's gravity.</summary>
    private static Ship Shaft()
    {
        int StructureAt(int cell)
        {
            int x = cell % Width, y = cell / Width;
            return y >= Ground && (x < Left || x > Right || y == Height - 1) ? Structure.Wall : Structure.Empty;
        }
        var planet = Surfaces.TestStrip;
        return new Ship(Width, Height, StructureAt, _ => false, gravity: planet.Fall(Spec.Gravity), falling: Spec.PlanetFallGravity);
    }

    private static HarpoonWorld WorldOf(Ship ship) => new(
        (from, to) => Aiming.Grapples(ship, from, to) is var (at, _) ? new RodHit(null, at) : null,
        _ => null, _ => false, _ => false, at => Aiming.Grips(ship, at));

    [Fact]
    public void EightHundredCellsUpAShaftTakeUnderSeventySecondsOfClicksAndAJumpOffTheLineLandsOnTheGround()
    {
        var ship = Shaft();
        var world = WorldOf(ship);
        var hero = new Hero((Left * Unit + (Right + 1) * Unit) / 2, (Height - 1) * Unit, 0, 0, false, -1);
        var shot = new HarpoonShot();
        int steps = 0, shots = 0;
        // Up: a click whenever the gun is ready, hooked or hanging, at the left wall Aim higher, never over the ground's top.
        for (; steps < 70 * 60 && !(shot.Rod == Rod.Hanging && hero.Y < (Ground + 6) * Unit); steps++)
        {
            hero = hero.Step(new HeroInput(0, 0, false) { Pull = shot.PullTo }, ship);
            var middle = hero.Middle;
            (double X, double Y) muzzle = (middle.X, middle.Y - Unit);
            (double X, double Y) target = (Left * Unit - 1, Math.Max(muzzle.Y - Aim, Ground * Unit + Unit / 2));
            bool press = shot.Rod is Rod.Hanging or Rod.Hooked || shot.Ready;
            var step = shot.Step(Hero.StepMs, press, new GunHold(true, muzzle, (target.X - muzzle.X, target.Y - muzzle.Y), (middle.X, middle.Y)), world);
            if (step.Fired) shots++;
        }
        Assert.True(hero.Y < (Ground + 6) * Unit, $"after {steps / 60.0:F1} s the feet are at row {hero.Y / Unit}");
        // A hop a click, near the most the range allows: no shot missed.
        Assert.InRange(shots, Deep / 40, Deep / 34);

        // Space on the line, then a jump held toward the ground on the left: up over the lip, onto it.
        hero = hero.Step(new HeroInput(0, 0, true) { Pull = shot.PullTo }, ship);
        var free = new HeroInput(-1, 0, false) { JumpHeld = true };
        for (int i = 0; i < 300 && !(hero.Y == Ground * Unit && hero.X < Left * Unit); i++) hero = hero.Step(free, ship);
        Assert.Equal(Ground * Unit, hero.Y);
        Assert.True(hero.X < Left * Unit);
    }

    [Fact]
    public void LetGoOnTheLineWithoutSpaceTheHeroFalls()
    {
        var ship = Shaft();
        var hero = new Hero((Left * Unit + (Right + 1) * Unit) / 2, (Height - 1) * Unit, 0, 0, false, -1);
        var to = new Point(hero.X, hero.Middle.Y - 20 * Unit);
        hero = HeroTests.Settle(ship, hero, new HeroInput(0, 0, false) { Pull = to });
        Assert.Equal(to, hero.Middle);
        var after = hero.Step(new HeroInput(0, 0, false), ship);
        Assert.Equal(ship.Gravity, after.Vy);
    }
}
