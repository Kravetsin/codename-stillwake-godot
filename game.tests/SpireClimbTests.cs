using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Ruins = Stillwake.Content.Ruins;
using Spec = Stillwake.Content.Hero;
using Surfaces = Stillwake.Content.Surfaces;

namespace Stillwake.Game.Tests;

/// <summary>
/// The way back out of a spire (0095): from its chamber floor up the ladder in its light well to the room in its tip, and out
/// through a plug dug open, with plain keys under the first planet's gravity, on the spires as the ground grows them.
/// </summary>
public class SpireClimbTests
{
    private const int Unit = Hero.Unit;
    private static readonly HeroInput Still = new(0, 0, false);

    /// <summary>
    /// The first spire the first planet grows on <paramref name="seed"/>, cut out of its ground with a margin, both its plugs dug
    /// open: its ship, and its left column and top row in it.
    /// </summary>
    private static (Ship Ship, int X0, int Y0) Spire(uint seed)
    {
        var l = Terrain.Generate(Content.Ground.Planet, seed);
        var lens = l.Flora!.First(p => p.Kind == Ruins.SpireLamp).Cell;
        int lx = lens % l.Width - Ruins.Spire[0].IndexOf(Ruins.Lamp), ly = lens / l.Width;
        const int Margin = 12;
        int left = lx - Margin, top = Math.Max(0, ly - 4), wide = Ruins.SpireWide + 2 * Margin, tall = Ruins.Spire.Count + 6;
        var rows = new List<string>();
        for (int y = top; y < top + tall; y++)
        {
            var row = l.Rows[y].Substring(left, wide).ToCharArray();
            for (int dx = 0; dx < Ruins.SpireWide; dx++)
                if (y - ly is >= 0 and var dy && dy < Ruins.Spire.Count && Ruins.Spire[dy][dx] == Ruins.Plug) row[Margin + dx] = '.';
            rows.Add(new string(row));
        }
        return (OnPlanet(rows), Margin, ly - top);
    }

    /// <summary>Cells cut out of a planet as its surface's ship has them: nothing floats, its gravity (0076).</summary>
    private static Ship OnPlanet(IReadOnlyList<string> rows)
    {
        var grid = new TestWorld(Layout.Parse(rows)).M.Grid;
        return new Ship(grid.Width, grid.Height, grid.StructureAt, _ => false, null, grid.ShapeAt,
            Surfaces.Planet.Fall(Spec.Gravity), Spec.PlanetFallGravity);
    }

    /// <summary>
    /// What a player does: from the chamber's floor walks to the ladder's foot, jumps at it with up held, climbs, and at the top
    /// walks out to the left. Returns every position stepped through, its feet and body.
    /// </summary>
    private static List<Hero> ClimbOut(Ship ship, int x0, int y0)
    {
        int ladder = (x0 + 4) * Unit + Unit / 2;
        var hero = Hero.At(ship, (y0 + 27) * ship.Width + x0 + 6);
        var path = new List<Hero> { hero };
        void Go(HeroInput keys, int steps, Func<Hero, bool>? until = null)
        {
            for (int i = 0; i < steps && !(until?.Invoke(hero) ?? false); i++) path.Add(hero = hero.Step(keys, ship));
        }
        Go(Still, 30);
        Go(Still with { X = -1 }, 200, h => Math.Abs(h.X - ladder) < 300);
        Go(Still, 20);
        Go(Still with { Y = -1, Jump = true }, 1);
        Go(Still with { Y = -1, JumpHeld = true }, 60, h => h.Climbing);
        Go(Still with { Y = -1 }, 600, h => !h.Climbing && h.Vy == 0);
        Go(Still with { X = -1 }, 300);
        return path;
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(7u)]
    [InlineData(11u)]
    public void AHeroClimbsFromTheChamberFloorUpTheLadderAndWalksOutOfTheSpire(uint seed)
    {
        var (ship, x0, y0) = Spire(seed);
        var path = ClimbOut(ship, x0, y0);
        Assert.True(path.Any(h => h.Climbing), string.Join(" ", path.Where((_, i) => i % 5 == 0).Select(h => $"({h.X},{h.Y},{(h.Climbing ? "c" : "")})")) + $" x0={x0} y0={y0}");
        // It stood on the room's floor, the ladder's top, before it walked out.
        Assert.True(path.Any(h => !h.Climbing && h.Y == (y0 + 10) * Unit && Math.Abs(h.X - ((x0 + 4) * Unit + Unit / 2)) < Unit),
            string.Join(" ", path.Where((_, i) => i % 5 == 0).Select(h => $"({h.X},{h.Y},{(h.Climbing ? "c" : "")})")) + $" x0={x0} y0={y0}");
        var end = path[^1];
        Assert.True(end.X < (x0 + 1) * Unit, $"seed {seed}: ended at ({end.X}, {end.Y}), the spire's left at {x0 * Unit}");
        // Out over the spire's roof or the ground beside it, never back down its shaft.
        Assert.True(end.X < x0 * Unit || end.Y <= (y0 + 10) * Unit, $"seed {seed}: ended at ({end.X}, {end.Y})");
    }

    [Fact]
    public void WithoutTheLadderTheLedgeOverTheNextStopsAJumpUpToIt()
    {
        // Why the ladder (0095), the old shaft measured: ledges three rows apart either side by turns. From the right one at row
        // 11 the next is the left at row 8, three up, a held jump's six on the planet; but the right one at row 5 is over the
        // hero's head, five open rows over its feet for a body 2.625 tall, so the feet rise 2.375 and no more, past the ledge
        // assist's third of a cell.
        string[] rows =
        [
            "#########", "#.......#", "####....#", "#.......#", "#.......#", "#....####", "#.......#",
            "#.......#", "####....#", "#.......#", "#.......#", "#....####", "#.......#", "#########",
        ];
        var ship = OnPlanet(rows);
        var hero = Hero.At(ship, 10 * ship.Width + 6);
        Assert.Equal(11 * Unit, hero.Y);
        int highest = hero.Y;
        hero = hero.Step(Still with { Jump = true }, ship);
        for (int i = 0; i < 90; i++)
        {
            hero = hero.Step(Still with { X = i < 10 ? 0 : -1, JumpHeld = true }, ship);
            highest = Math.Min(highest, hero.Y);
        }
        Assert.Equal(6 * Unit + Spec.Height, highest);
        Assert.True(highest - 8 * Unit > 300, $"feet at {highest}, the next ledge's top at {8 * Unit}");
        Assert.True(hero.Y > 8 * Unit, $"landed at {hero.Y}");
    }
}
