using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using PlanetTypes = Stillwake.Content.PlanetTypes;
using Ruins = Stillwake.Content.Ruins;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// The way back out of a spire (0096): from its chamber floor up its ledges, a held jump each, to the room in its tip and out
/// through a plug dug open, with plain keys under the first planet's gravity, on the spires as the ground grows them.
/// </summary>
public class SpireClimbTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;
    private static readonly HeroInput Still = new(0, 0, false);

    /// <summary>
    /// The first spire the first planet grows on <paramref name="seed"/>, cut out of its ground with a margin, both its plugs dug
    /// open and its drawn rows <paramref name="edits"/> drawn anew, a character a 16 px block: its ship, and its left column and top row in it, in cells.
    /// </summary>
    private static (Ship Ship, int X0, int Y0) Spire(uint seed, params (int Row, string Cells)[] edits)
    {
        var l = Terrain.Generate(PlanetTypes.First, seed);
        var lens = l.Flora!.First(p => p.Kind == Ruins.SpireLamp).Cell;
        int lx = lens % l.Width - Ruins.Spire[0].IndexOf(Ruins.Lamp), ly = lens / l.Width;
        const int Margin = 24;
        int left = lx - Margin, top = Math.Max(0, ly - 8), wide = Ruins.SpireWide + 2 * Margin, tall = Ruins.Spire.Count + 12;
        var rows = new List<string>();
        for (int y = top; y < top + tall; y++)
        {
            var row = l.Rows[y].Substring(left, wide).ToCharArray();
            int dy = y - ly;
            for (int dx = 0; dx < Ruins.SpireWide && dy >= 0 && dy < Ruins.Spire.Count; dx++)
                if (Ruins.Spire[dy][dx] == Ruins.Plug) row[Margin + dx] = '.';
            foreach (var (at, cells) in edits)
                if (dy >= 0 && at == dy / 2) string.Concat(cells.Select(c => new string(c, 2))).CopyTo(0, row, Margin, 2 * cells.Length);
            rows.Add(new string(row));
        }
        return (OnPlanet(Layout.Parse(rows)), Margin, ly - top);
    }

    /// <summary>Cells cut out of a planet as its surface's ship has them: nothing floats, its gravity (0076).</summary>
    private static Ship OnPlanet(Layout layout)
    {
        var grid = new TestWorld(layout).M.Grid;
        return new Ship(grid.Width, grid.Height, grid.StructureAt, _ => false, null,
            PlanetTypes.First.Fall(Spec.Gravity), Spec.PlanetFallGravity);
    }

    /// <summary>
    /// What a player does, from the chamber's floor under the light: a held jump right onto the slab; along it to its edge and a held
    /// jump left onto the first ledge; then right, left and right again, a running jump off each ledge's edge, the last up through
    /// the hole into the room; and out right through the plug. Every position stepped through, and the row each jump landed on.
    /// </summary>
    private static (List<Hero> Path, List<int> Landed) ClimbOut(Ship ship, int x0, int y0)
    {
        var hero = Hero.At(ship, (y0 + 55) * ship.Width + x0 + 8);
        var path = new List<Hero> { hero };
        var landed = new List<int>();
        void Go(HeroInput keys, int steps, Func<Hero, bool>? until = null)
        {
            for (int i = 0; i < steps && !(until?.Invoke(hero) ?? false); i++) path.Add(hero = hero.Step(keys, ship));
        }
        void Hop(int dir)
        {
            Go(Still with { X = dir, Jump = true }, 1);
            int steps = 0;
            Go(Still with { X = dir, JumpHeld = true }, 150, h => ++steps > 3 && h.Airborne == 0);
            landed.Add((hero.Y - y0 * Unit) / Block);
        }
        int X(double blocks) => (int)(x0 * Unit + blocks * Block);
        Go(Still, 30);
        Hop(1);
        Go(Still with { X = -1 }, 200, h => h.X <= X(6.3));
        Hop(-1);
        foreach (int dir in (int[])[1, -1, 1])
        {
            Go(Still with { X = dir }, 200, h => dir > 0 ? h.X >= X(3.7) : h.X <= X(5.3));
            Hop(dir);
        }
        Go(Still with { X = 1 }, 200);
        return (path, landed);
    }

    /// <summary>Whether the climb ended out of the spire: right of it on its roof or the ground beside it, never down its shaft.</summary>
    private static bool Out(Hero end, int x0, int y0) => end.X > x0 * Unit + 7 * Block && (end.X >= x0 * Unit + 9 * Block || end.Y <= y0 * Unit + 10 * Block);

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(7u)]
    [InlineData(11u)]
    public void AHeroClimbsFromTheChamberFloorUpTheLedgesInFiveJumpsAndWalksOutOfTheSpire(uint seed)
    {
        var (ship, x0, y0) = Spire(seed);
        var (path, landed) = ClimbOut(ship, x0, y0);
        // The slab, the ledges at rows 22, 18 and 14 by turns, the room's floor: each jump lands where it aimed.
        Assert.True(landed.SequenceEqual([26, 22, 18, 14, 10]), $"landed on rows {string.Join(", ", landed)}; {Trace(path, x0, y0)}");
        Assert.True(Out(path[^1], x0, y0), Trace(path, x0, y0));
    }

    [Theory]
    [InlineData(18, "#.......#")]
    [InlineData(18, "####....#")]
    [InlineData(22, "#....####")]
    public void ALedgeTakenOutOrMovedOverItsNeighbourLeavesTheSameKeysShortOfTheWayOut(int row, string cells)
    {
        // Proof the climb bites (0096): the ledge at row 18 gone, or moved left over the one at 22, or that one moved right under it.
        var (ship, x0, y0) = Spire(1, (row, cells));
        var (path, landed) = ClimbOut(ship, x0, y0);
        Assert.False(landed.SequenceEqual([26, 22, 18, 14, 10]) && Out(path[^1], x0, y0), string.Join(", ", landed));
    }

    /// <summary>The path every fifth step, its feet and whether it climbs, for a failure to say where the hero went.</summary>
    private static string Trace(List<Hero> path, int x0, int y0) =>
        string.Join(" ", path.Where((_, i) => i % 5 == 0).Select(h => $"({h.X}, {h.Y}{(h.Climbing ? ", climbing" : "")})")) + $" from the spire at ({x0}, {y0})";

    [Fact]
    public void LedgesThreeRowsApartLeaveTheOneOverTheTakeOffInTheWay()
    {
        // Why the ledges are four rows apart (0096), the old shaft measured: three apart either side by turns. From the right one
        // at row 11 the next is the left at row 8, three up, a held jump's 5.8 on the planet; but the right one at row 5 is over
        // the hero's head, five open rows over its feet for a body 2.625 tall, so the feet rise 2.375 and no more, past the ledge
        // assist's reach. Rows here are characters, two cells each (0104).
        string[] rows =
        [
            "#########", "#.......#", "####....#", "#.......#", "#.......#", "#....####", "#.......#",
            "#.......#", "####....#", "#.......#", "#.......#", "#....####", "#.......#", "#########",
        ];
        var ship = OnPlanet(Layout.Parse(rows).Doubled());
        var hero = Hero.At(ship, HeroTests.BlockCell(ship, 6, 10));
        Assert.Equal(11 * Block, hero.Y);
        int highest = hero.Y;
        hero = hero.Step(Still with { Jump = true }, ship);
        for (int i = 0; i < 90; i++)
        {
            hero = hero.Step(Still with { X = i < 10 ? 0 : -1, JumpHeld = true }, ship);
            highest = Math.Min(highest, hero.Y);
        }
        Assert.Equal(6 * Block + Spec.Height, highest);
        Assert.True(highest - 8 * Block > Hero.MantleReach(0), $"feet at {highest}, the next ledge's top at {8 * Block}");
        Assert.True(hero.Y > 8 * Block, $"landed at {hero.Y}");
    }
}
