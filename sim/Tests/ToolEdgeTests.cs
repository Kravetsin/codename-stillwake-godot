using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A running tool's hit on what its drawn edge touches (tool-edge): the client names the wrecks and cells, the sim hits each listed
/// one it may, once, and checks neither reach nor position.
/// </summary>
public class ToolEdgeTests
{
    private static readonly string Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    // Wrecks.

    /// <summary>Twenty wide and eight tall, a room at the left; hero 0 out in space at (12, 4), two racks before it, still.</summary>
    private static (Scenario W, Piece A, Piece B) Space(bool enter = true)
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
        var a = w.M.Debris.Pieces[0];
        (a.Item, a.X, a.Y) = ("cableRack", 15_000, 2_000);
        var b = new Piece { Id = 99, Zone = a.Zone, Item = "cableRack", X = 17_000, Y = 6_000, End = 0 };
        ((DebrisState)w.M.Debris.Slice).Pieces.Add(b);
        return (enter ? w.Do(new Enter(4 * 20 + 12)).Run() : w, a, b);
    }

    [Fact]
    public void EachListedWreckLosesTheDamageOnceWhateverTheOrderAndRepeats()
    {
        var (w, a, b) = Space();
        w.Do(new HewPieces(Axe, new List<int> { b.Id, a.Id, b.Id, a.Id })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((Weapons.Axe.Damage, Weapons.Axe.Damage), (a.Damage, b.Damage));
    }

    [Fact]
    public void NeitherReachNorPositionIsChecked()
    {
        // Far from the hero and on no line from it: the client's word.
        var (w, _, b) = Space();
        (b.X, b.Y) = (19_000, 7_000);
        w.Do(new HewPieces(Axe, new List<int> { b.Id })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Weapons.Axe.Damage, b.Damage);
    }

    [Fact]
    public void AGoneIdIsSkippedAndTheRestHit()
    {
        var (w, a, _) = Space();
        w.Do(new HewPieces(Axe, new List<int> { 12_345, a.Id })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Weapons.Axe.Damage, a.Damage);
    }

    [Fact]
    public void NoWreckInSpaceAmongThemIsNothingToHit()
    {
        var (w, a, b) = Space();
        // A small piece is caught, not hewn; a wreck over the hull is not in space.
        a.Item = "scrapPlate";
        (b.X, b.Y) = (3_500, 3_500);
        w.Do(new HewPieces(Axe, new List<int> { a.Id, b.Id, 12_345 })).Run();
        Assert.Equal(["debris.nothingToHit"], Reasons(w));
        Assert.Equal(0, b.Damage);
    }

    [Fact]
    public void AHeroAboardHitsNoWreck()
    {
        var (w, a, _) = Space(enter: false);
        w.Do(new HewPieces(Axe, new List<int> { a.Id })).Run();
        Assert.Equal(["debris.notOutside"], Reasons(w));
        Assert.Equal(0, a.Damage);
    }

    [Fact]
    public void ARunningToolHitsOnlyWhileItRuns()
    {
        var (w, a, _) = Space();
        w.GiveItems(0, (Drill, 1)).Do(new SetCreative(true)).Run();
        w.Do(new HewPieces(Drill, new List<int> { a.Id })).Run();
        Assert.Equal(["items.notRunning"], Reasons(w));
        Assert.Null(w.M.Debris.HewPiecesProblem(0, Drill, [a.Id]));
        w.Do(new Run(Drill, true)).Do(new HewPieces(Drill, new List<int> { a.Id })).Run();
        Assert.Single(w.Refusals);
        Assert.Equal(Weapons.Drill.Damage, a.Damage);
    }

    // Cells.

    /// <summary>Open air over two rows of stone, a bush at (3, 3), on a planet, a character a cell.</summary>
    private static Scenario Meadow() => new((Layout.Parse("""
        ........
        ........
        ........
        ........
        ########
        ########
        """).WithMaterials("""
        ........
        ........
        ........
        ........
        tttttttt
        tttttttt
        """) with { Flora = [(3 * 8 + 3, "bush")] }).OnPlanet());

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    private static int CrackAt(Scenario w, int cell) => w.M.Building.Cracks.SingleOrDefault(c => c.Cell == cell)?.Damage ?? 0;

    [Fact]
    public void EachListedCellIsStruckOnceWhateverTheOrderAndRepeats()
    {
        var w = Meadow();
        int left = At(w, 2, 4), right = At(w, 5, 5);
        w.Do(new HewCells(Axe, new List<int> { right, left, right, left })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((Weapons.Axe.BlockDamage, Weapons.Axe.BlockDamage), (CrackAt(w, left), CrackAt(w, right)));
        Assert.Equal(0, CrackAt(w, At(w, 3, 4)));
    }

    [Fact]
    public void APlantInAListedCellIsStruck()
    {
        var w = Meadow();
        var bush = w.M.Building.Plants.Single();
        w.Do(new HewCells(Axe, new List<int> { bush.Cell })).Run();
        Assert.Empty(w.Refusals);
        Assert.True(CrackAt(w, bush.Cell) > 0 || !w.M.Building.Plants.Contains(bush));
    }

    [Fact]
    public void CellsOffTheGridAmongGoodOnesAreSkipped()
    {
        var w = Meadow();
        int wall = At(w, 1, 4);
        w.Do(new HewCells(Axe, new List<int> { -3, wall, 10_000 })).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Weapons.Axe.BlockDamage, CrackAt(w, wall));
    }

    [Fact]
    public void OnlyOpenAirOrCellsOffTheGridIsNothingToDig()
    {
        var w = Meadow();
        w.Do(new HewCells(Axe, new List<int> { -1, At(w, 0, 0), 10_000 })).Run();
        Assert.Equal(["building.nothingToDig"], Reasons(w));
    }

    [Fact]
    public void TheShipsWallsAreHewnAtATenth()
    {
        var (w, _, _) = Space();
        w.Do(new HewCells(Axe, new List<int> { 0 * 20 + 1, 3 * 20 + 1 })).Run();
        Assert.Empty(Reasons(w));
        int tenth = Weapons.Axe.BlockDamage * Damage.OwnShipTakes / 1000;
        Assert.Equal([(1, tenth), (61, tenth)], w.M.Building.Cracks.Select(c => (c.Cell, c.Damage)).OrderBy(c => c.Cell));
    }
}
