using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The fan of rays: a tool's hit is a fan from the hand, each ray striking the first wall it meets and stopping there, passing
/// through the plants it crosses; each thing struck takes the damage once.
/// </summary>
public class HewTests
{
    private static readonly string Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;
    private const int Right = 0, Down = 64, Left = 128, Up = HewTesting.Up;

    /// <summary>A map a character a cell, its walls of the materials map: s soil, t stone, b bedrock, a dot the hull.</summary>
    private static Scenario World(string map, string materials) => new(Layout.Parse(map).WithMaterials(materials).OnPlanet());

    /// <summary>A point in thousandths of a cell: the middle of cell (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static (int X, int Y) Mid(int x, int y) => (x * 1000 + 500, y * 1000 + 500);

    private static Hewn Struck(Scenario w, string tool, (int X, int Y) at, int heading) =>
        w.M.Building.Struck(tool, at.X, at.Y, heading);

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    // The rays.

    [Fact]
    public void AHeadingIsAStepOfTheTurnFromPlusXClockwiseAndAFansRaysSpreadEvenlyOverItsArc()
    {
        Assert.Equal((1000, 0), Rays.Direction(Right));
        Assert.Equal((0, 1000), Rays.Direction(Down));
        Assert.Equal((-1000, 0), Rays.Direction(Left));
        Assert.Equal((0, -1000), Rays.Direction(Up));
        Assert.Equal((707, 707), Rays.Direction(32));
        Assert.Equal(Rays.Direction(0), Rays.Direction(Headings.Turn));
        // The axe's quarter turn in nine rays 8 steps apart, the drill's 8 steps in three 4 apart; round the turn\x27s end.
        Assert.Equal([224, 232, 240, 248, 0, 8, 16, 24, 32], Enumerable.Range(0, Weapons.Axe.Rays).Select(i => Rays.HeadingOf(Weapons.Axe, 0, i)));
        Assert.Equal([60, 64, 68], Enumerable.Range(0, Weapons.Drill.Rays).Select(i => Rays.HeadingOf(Weapons.Drill, Down, i)));
    }

    [Fact]
    public void ARayVisitsTheCellsItCrossesInOrderToItsRangeAndAnExactCornerVisitsBothCellsBesideIt()
    {
        var seen = new List<int>();
        Rays.Cast(10, 10, 500, 500, Right, 3500, c => { seen.Add(c); return false; });
        Assert.Equal([0, 1, 2, 3], seen);
        seen.Clear();
        // Through the corners of the diagonal: each corner\x27s two cells beside it, x first, then the next on the diagonal.
        Rays.Cast(10, 10, 500, 500, 32, 3000, c => { seen.Add(c); return false; });
        Assert.Equal([0, 1, 10, 11, 12, 21, 22], seen);
        seen.Clear();
        // Off the grid it ends unasked.
        Rays.Cast(10, 10, 500, 500, Left, 3500, c => { seen.Add(c); return false; });
        Assert.Equal([0], seen);
    }

    // The front layer.

    private const string Face = """
        ..........
        ..........
        ..........
        ......####
        ......####
        ......####
        ..........
        """;

    private const string Stone = """
        ..........
        ..........
        ..........
        ......tttt
        ......tttt
        ......tttt
        ..........
        """;

    [Fact]
    public void TheAxeAtAFlatFaceCracksABandOfItsFaceCellsOnceEachAndNoneBehindThem()
    {
        var w = World(Face, Stone);
        var at = Mid(3, 4);
        // Nine rays, but each face cell they meet struck once: the face column, nothing a cell deeper.
        Assert.Equal([At(w, 6, 3), At(w, 6, 4), At(w, 6, 5)], Struck(w, Axe, at, Right).Cells);
        w.Do(new Hew(Axe, at.X, at.Y, Right)).Run();
        Assert.Empty(w.Refusals);
        int hit = Weapons.Axe.BlockDamage;
        Assert.Equal([(At(w, 6, 3), hit), (At(w, 6, 4), hit), (At(w, 6, 5), hit)], w.M.Building.Cracks.Select(c => (c.Cell, c.Damage)));
    }

    [Fact]
    public void TheDrillsNarrowBeamStrikesOneOrTwoCellsFartherOff()
    {
        var w = World(Face, Stone);
        // From five cells off, out of the axe's reach.
        Assert.Empty(Struck(w, Axe, Mid(1, 4), Right).Cells);
        Assert.Equal([At(w, 6, 4)], Struck(w, Drill, Mid(1, 4), Right).Cells);
        Assert.Equal([At(w, 6, 3), At(w, 6, 4)], Struck(w, Drill, (1500, 4000), Right).Cells);
    }

    [Fact]
    public void AnOverhangIsStruckOnItsUndersideAndItsFace()
    {
        var w = World(Face, Stone);
        // From below and left of its corner, aimed up and right: the face and the underside both.
        var cells = Struck(w, Axe, Mid(5, 6), 224).Cells;
        Assert.Contains(At(w, 6, 5), cells);
        Assert.Contains(cells, c => c / 10 == 5 && c % 10 > 6);
        Assert.All(cells, c => Assert.True(c % 10 == 6 || c / 10 == 5, $"cell {c} lies inside the rock"));
    }

    [Fact]
    public void TwoWallsTouchingAtACornerCloseTheGapBetweenThem()
    {
        var w = World("""
            ....
            ..#.
            .#..
            ....
            """, """
            ....
            ..s.
            .s..
            ....
            """);
        // From the open cell above and left of the corner the two walls share, down and right through it: both struck, the open
        // cell past the corner never reached.
        var seen = new List<int>();
        Rays.Cast(4, 4, 1500, 1500, 32, 3000, c => { seen.Add(c); return w.M.Grid.WallAt(c) != null; });
        Assert.Equal([At(w, 1, 1), At(w, 2, 1), At(w, 1, 2)], seen);
        Assert.Equal([At(w, 2, 1), At(w, 1, 2)], Struck(w, Drill, (1500, 1500), 32).Cells);
    }

    [Fact]
    public void BedrockInFrontStopsTheFanSoTheGroundBehindItIsSpared()
    {
        var w = World(Face, """
            ..........
            ..........
            ..........
            ......bttt
            ......bttt
            ......bttt
            ..........
            """);
        w.Do(new Hew(Axe, 5500, 4500, Right)).Run();
        Assert.Equal(["building.nothingToDig"], w.Refusals.Select(r => r.Reason));
        Assert.Empty(w.M.Building.Cracks);
    }

    // Plants.

    private const string Meadow = """
        ........
        ........
        ........
        ........
        ########
        ########
        """;

    private const string MeadowGround = """
        ........
        ........
        ........
        ........
        tttttttt
        tttttttt
        """;

    [Fact]
    public void ARayPassesThroughABushStrikingItAndTheWallBehindIt()
    {
        var w = new Scenario((Layout.Parse(Meadow).WithMaterials(MeadowGround) with { Flora = [(3 * 8 + 3, "bush")] }).OnPlanet());
        var bush = w.M.Building.Plants.Single();
        // From above and beside the bush, down through it to the ground under it.
        var struck = Struck(w, Drill, Mid(3, 1), Down);
        Assert.Equal([bush], struck.Plants);
        Assert.Contains(At(w, 3, 4), struck.Cells);
        w.Do(new Hew(Axe, 3500, 1500, Down)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Weapons.Axe.BlockDamage, w.M.Building.Cracks.Single(c => c.Cell == bush.Cell).Damage);
        Assert.Equal(Weapons.Axe.BlockDamage, w.M.Building.Cracks.Single(c => c.Cell == At(w, 3, 4)).Damage);
    }

    [Fact]
    public void ATreeMetByManyRaysIsStruckOnce()
    {
        var w = new Scenario((Layout.Parse(Meadow).WithMaterials(MeadowGround) with { Flora = [(3 * 8 + 3, "acacia")] }).OnPlanet());
        var tree = w.M.Building.Plants.Single();
        Assert.Equal([tree], Struck(w, Axe, Mid(1, 2), Right).Plants);
        w.Do(new Hew(Axe, 1500, 2500, Right)).Run();
        Assert.Equal(Weapons.Axe.BlockDamage, w.M.Building.Cracks.Single(c => c.Cell == tree.Cell).Damage);
    }

    // In space.

    /// <summary>Twenty wide and eight tall, a room at the left; hero 0 out in space at (12, 4) with the axe.</summary>
    private static Scenario Space()
    {
        var w = new Scenario(Layout.Parse(
        [
            " #####              ",
            " #...#              ",
            " #...#              ",
            " #.S.#              ",
            " #...#              ",
            " #...#              ",
            " #...#              ",
            " #####              ",
        ])).Run(DebrisSpec.Every);
        foreach (var p in w.M.Debris.Pieces) p.Speed = 0;
        return w.Do(new Enter(4 * 20 + 12)).Run();
    }

    [Fact]
    public void TheFanStrikesTheNearWreckAndNotOneWhollyBehindIt()
    {
        var w = Space();
        var near = w.M.Debris.Pieces[0];
        (near.Item, near.X, near.Y) = ("cableRack", 15_000, 4_000);
        // A second rack wholly behind the first along the beam: the rays stop in the first.
        var far = new Piece { Id = 99, Zone = near.Zone, Item = "cableRack", X = 19_000, Y = 4_000, End = 0 };
        ((DebrisState)w.M.Debris.Slice).Pieces.Add(far);
        Assert.Equal([near], w.M.Debris.Struck(Drill, 12_500, 4_000, Right));
        w.Do(new HewWrecks(Axe, 12_500, 4_000, Right)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((Weapons.Axe.Damage, 0), (near.Damage, far.Damage));
    }

    [Fact]
    public void AWallStopsARayBeforeAWreckBeyondIt()
    {
        var w = Space();
        var wreck = w.M.Debris.Pieces[0];
        (wreck.Item, wreck.X, wreck.Y) = ("cableRack", 9_000, 4_000);
        // From inside the room, aimed out through its wall.
        Assert.Empty(w.M.Debris.Struck(Axe, 4_500, 4_000, Right));
        Assert.Equal([wreck], w.M.Debris.Struck(Axe, 6_500, 4_000, Right));
    }

    // Invariants.

    /// <summary>
    /// On any planet's ground, from any point and heading: every cell struck is the first wall some ray of the fan meets, and every
    /// ray's first wall that breaks is struck; bedrock never.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property EveryCellStruckIsTheFirstARayMeetsAndNothingBehindItIs()
    {
        const int width = 12, height = 10;
        var gen =
            from cells in Gen.Elements('.', '.', 's', 't', 'b', '#').ArrayOf(width * height)
            from backs in Gen.Elements('.', 's', 't').ArrayOf(width * height)
            from x in Gen.Choose(0, width * 1000 - 1)
            from y in Gen.Choose(0, height * 1000 - 1)
            from heading in Gen.Choose(0, Headings.Turn - 1)
            from tool in Gen.Elements(Axe, Drill)
            select (cells, backs, x, y, heading, tool);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            string Rows(Func<int, char> at) =>
                string.Join("\n", Enumerable.Range(0, height).Select(r => new string(Enumerable.Range(0, width).Select(c => at(r * width + c)).ToArray())));
            var map = Rows(i => t.cells[i] == '.' ? '.' : '#');
            var materials = Rows(i => t.cells[i] is '.' or '#' ? (t.cells[i] == '.' ? t.backs[i] : '.') : t.cells[i]);
            var w = new Scenario(Layout.Parse(map).WithMaterials(materials).OnPlanet());
            var grid = w.M.Grid;
            var struck = w.M.Building.Struck(t.tool, t.x, t.y, t.heading).Cells;
            var weapon = Weapons.Of(t.tool)!;
            var expected = new SortedSet<int>();
            for (int i = 0; i < weapon.Rays; i++)
                Rays.Cast(width, height, t.x, t.y, Rays.HeadingOf(weapon, t.heading, i), weapon.Range, c =>
                {
                    if (grid.WallAt(c) is { } wall)
                    {
                        if (wall.Strength != Material.Unbreakable) expected.Add(c);
                        return true;
                    }
                    return grid.Blocks(c);
                });
            return struck.SequenceEqual(expected);
        });
    }

    /// <summary>A map and its mirror image: the fan aimed the mirrored way strikes the mirrored cells.</summary>
    [Property(MaxTest = 200)]
    public Property AMirroredFanStrikesTheMirroredCells()
    {
        const int width = 12, height = 10;
        var gen =
            from cells in Gen.Elements('.', '.', 's', 't').ArrayOf(width * height)
            from cx in Gen.Choose(0, width - 1)
            from fx in Gen.Choose(1, 999)
            from y in Gen.Choose(0, height * 1000 - 1)
            from heading in Gen.Choose(0, Headings.Turn - 1)
            from tool in Gen.Elements(Axe, Drill)
            select (cells, x: cx * 1000 + fx, y, heading, tool);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            Scenario Of(bool mirrored)
            {
                char At(int r, int c) => t.cells[r * width + (mirrored ? width - 1 - c : c)];
                var rows = Enumerable.Range(0, height).Select(r => new string(Enumerable.Range(0, width).Select(c => At(r, c)).ToArray())).ToArray();
                return new Scenario(Layout.Parse(string.Join("\n", rows.Select(r => new string(r.Select(ch => ch == '.' ? '.' : '#').ToArray()))))
                    .WithMaterials(string.Join("\n", rows)).OnPlanet());
            }
            var plain = Of(false).M.Building.Struck(t.tool, t.x, t.y, t.heading).Cells;
            var mirror = Of(true).M.Building.Struck(t.tool, width * 1000 - t.x, t.y, (Headings.Turn / 2 - t.heading) & (Headings.Turn - 1)).Cells;
            return plain.Select(c => c / width * width + width - 1 - c % width).OrderBy(c => c).SequenceEqual(mirror);
        });
    }
}
