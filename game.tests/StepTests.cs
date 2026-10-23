using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// Steps replace slopes (0106): a hero walking steps up onto ground a cell higher, the head permitting, and follows ground a cell
/// lower without a fall; two cells stop it, and fall it. Maps here are a character a cell, the steps' own size.
/// </summary>
public class StepTests
{
    private const int Unit = Hero.Unit, Half = Spec.Width / 2;
    private static readonly HeroInput Right = HeroTests.Right, Run = Right with { Sprint = true };

    /// <summary>A room 24 wide with its floor on row 10's top, <paramref name="fill"/> deciding which other cells are walls.</summary>
    private static Ship Room(Func<int, int, bool> fill) => HeroTests.ShipFrom(Layout.Parse([.. Enumerable.Range(0, 12).Select(y =>
        new string([.. Enumerable.Range(0, 24).Select(x => y == 0 || y >= 10 || x == 0 || x == 23 || fill(x, y) ? '#' : '.')]))]));

    private static Hero Standing(int x, int y) => new(x, y, 0, 0, false, 1);

    [Fact]
    public void AWalkStepsUpACellButNotTwo()
    {
        // A step a cell high from column 8, and from column 14 one two cells higher still.
        var ship = Room((x, y) => x >= 8 && y == 9 || x >= 14 && y >= 7);
        var walked = HeroTests.Settle(ship, Standing(3 * Unit, 10 * Unit), Right);
        Assert.Equal((14 * Unit - Half, 9 * Unit), (walked.X, walked.Y));
        // Run at, the higher step stops the hero as surely.
        var ran = HeroTests.Settle(ship, Standing(3 * Unit, 10 * Unit), Run);
        Assert.Equal((14 * Unit - Half, 9 * Unit), (ran.X, ran.Y));
    }

    [Fact]
    public void AStepUnderACeilingTooLowToRiseIntoStopsTheWalk()
    {
        // A step a cell high from column 8, under a ceiling at row 3 from column 6: standing, the head is just under it.
        var ship = Room((x, y) => x >= 8 && y == 9 || x >= 6 && y == 3);
        var walked = HeroTests.Settle(ship, Standing(3 * Unit, 10 * Unit), Right);
        Assert.Equal((8 * Unit - Half, 10 * Unit), (walked.X, walked.Y));
        // With the ceiling a row higher the step is climbed.
        var higher = Room((x, y) => x >= 8 && y == 9 || x >= 6 && y == 2);
        Assert.Equal(9 * Unit, HeroTests.Settle(higher, Standing(3 * Unit, 10 * Unit), Right).Y);
    }

    [Fact]
    public void AWalkFollowsAStepACellDownWithoutAFallButFallsOffTwo()
    {
        foreach (int high in new[] { 1, 2 })
        {
            // A ledge from the left wall to column 10, its top `high` cells over the floor.
            var ship = Room((x, y) => x <= 10 && y >= 10 - high);
            var h = Standing(3 * Unit, (10 - high) * Unit);
            bool fell = false;
            for (int i = 0; i < 300 && h.X < 18 * Unit; i++)
            {
                h = h.Step(Right, ship);
                fell |= h.Vy != 0;
            }
            Assert.Equal(10 * Unit, h.Y);
            Assert.Equal(high == 2, fell);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AStairOfStepsIsWalkedUpAndDownAtItsSpeedOnTheFlatNeverInTheAir(bool run)
    {
        // A cell a column up from column 6 to three high, a plateau, and down a cell a column to the floor by column 18.
        var ship = Room((x, y) => y >= 10 - Math.Clamp(Math.Min(x - 5, 18 - x), 0, 3));
        int top = run ? Spec.Run : Spec.Walk;
        var h = Standing(2 * Unit, 10 * Unit);
        int highest = h.Y;
        for (int i = 0; i < 400 && h.X < 21 * Unit; i++)
        {
            h = h.Step(run ? Run : Right, ship);
            Assert.Equal(0, h.Vy);
            highest = Math.Min(highest, h.Y);
            // Past setting off, never slowed by a step.
            if (i > 10) Assert.True(top == h.Vx, $"step {i}: {h}");
        }
        Assert.Equal((7 * Unit, 10 * Unit), (highest, h.Y));
        Assert.True(h.X >= 21 * Unit);
    }

    [Fact]
    public void AStepIsDrawnEasedOverItsTimeNeverMoreThanAStepBehind()
    {
        const double Step = 8, Ms = 100;
        // Standing still, the body lifted a step: drawn where it was, then closing the step over Ms.
        var ease = new StepEase(Step, Ms);
        Assert.Equal(0, ease.Offset(0, 80, true, 16));
        Assert.Equal(Step - Step * 16 / Ms, ease.Offset(0, 72, true, 16), 9);
        double left = Step;
        for (int i = 0; i < 6; i++) left = ease.Offset(0, 72, true, 16);
        Assert.Equal(0, left);
        // Off the ground a rise is drawn as it is.
        var aloft = new StepEase(Step, Ms);
        aloft.Offset(0, 80, false, 16);
        Assert.Equal(0, aloft.Offset(0, 72, false, 16));
        // Up a stair a step every four frames, a quarter step across each: eased, and never more than a step behind.
        var stair = new StepEase(Step, Ms);
        var drawn = new List<double>();
        for (int i = 0; i < 40; i++) drawn.Add(stair.Offset(i * Step / 4, 400 - i / 4 * Step, true, 16));
        Assert.All(drawn, d => Assert.InRange(d, 0, Step));
        Assert.Contains(drawn, d => d > 0);
    }
}
