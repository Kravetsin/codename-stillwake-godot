using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Pipes;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>
/// The client looks only within what is built (F-6 slice 0) and at the cells the grid's log names (0051):
/// starlight and the drawn layers come out as they did over the whole grid, for ships anywhere
/// in worlds of space, edited inside, on the hull and out in space, with toggles in flight, past the log too.
/// </summary>
public class BuiltBoundsTests
{
    private static readonly Placement[] Placements =
    [
        Placement.Wall(), Placement.Wall("iron"), Placement.BackWall(), Placement.BackWall("iron"),
        new("structure", "floor"), new("structure", "ladder"), new("structure", "door"),
        new("machine", "lamp"), new("machine", "tank"),
    ];

    private static readonly string[] Layers = ["structure", "back", "machine"];

    /// <summary>A world of space <paramref name="w"/>×<paramref name="h"/> with a ship of random cells in a box somewhere in it, maybe against an edge.</summary>
    private static Registry Ship(Random rng, int w, int h)
    {
        var m = new Registry();
        int left = rng.Next(w - 3), top = rng.Next(h - 3), right = rng.Next(left + 2, w), bottom = rng.Next(top + 2, h);
        // Now and then a wall as a map draws it; others are built, out in space with no back wall too.
        var rows = Enumerable.Range(0, h).Select(y => new string([.. Enumerable.Range(0, w).Select(x =>
            x >= left && x <= right && y >= top && y <= bottom && rng.Next(12) == 0 ? '#' : ' ')])).ToList();
        World.Create(1, Layout.Parse(rows), m.All);
        for (int i = 0; i < (right - left + 1) * (bottom - top + 1); i++)
            Place(m, rng, (top + rng.Next(bottom - top + 1)) * w + left + rng.Next(right - left + 1));
        return m;
    }

    private static void Place(Registry m, Random rng, int cell)
    {
        var p = Placements[rng.Next(Placements.Length)];
        if (m.Grid.PlacementProblem(cell, p) == null) m.Grid.Place(cell, p);
    }

    /// <summary>A world of space with a hall in it: back walls with holes, many or very few, a wall or a lamp now and then.</summary>
    private static Registry Hall(Random rng, int w, int h)
    {
        var m = new Registry();
        World.Create(1, Layout.Parse(Enumerable.Repeat(new string(' ', w), h).ToList()), m.All);
        int left = rng.Next(1, 8), top = rng.Next(1, 4), right = w - rng.Next(2, 8), bottom = h - rng.Next(2, 4), holes = rng.Next(3) switch { 0 => 400, 1 => 30, _ => 4 };
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                int cell = y * w + x, roll = rng.Next(10_000);
                if (roll >= holes) m.Grid.Place(cell, Placement.BackWall());
                if (roll >= 9_900 && m.Grid.PlacementProblem(cell, Placement.Wall()) == null) m.Grid.Place(cell, Placement.Wall());
                if (roll is >= 9_850 and < 9_900 && m.Grid.PlacementProblem(cell, new Placement("machine", "lamp")) == null) m.Grid.Place(cell, new Placement("machine", "lamp"));
            }
        return m;
    }

    /// <summary>Some cells built, some taken apart, mostly in the ship and its edge, now and then far out in space.</summary>
    private static void Edit(Registry m, Random rng)
    {
        var b = m.Grid.Built;
        int w = m.Grid.Width, h = m.Grid.Height;
        for (int i = rng.Next(1, 6); i > 0; i--)
        {
            int cell = b.IsEmpty || rng.Next(5) == 0 ? rng.Next(w * h)
                : Math.Clamp(b.Top - 1 + rng.Next(b.Height + 2), 0, h - 1) * w + Math.Clamp(b.Left - 1 + rng.Next(b.Width + 2), 0, w - 1);
            if (rng.Next(2) == 0) Place(m, rng, cell);
            else m.Grid.Clear(cell, Layers[rng.Next(Layers.Length)]);
        }
    }

    [Fact]
    public void StarlightOverWhatIsBuiltIsWhatTheWholeGridGaveAndEveryCellPastItOpenStars()
    {
        var rng = new Random(6);
        for (int world = 0; world < 300; world++)
        {
            int w = rng.Next(6, 48), h = rng.Next(6, 28);
            var m = Ship(rng, w, h);
            if (world % 3 == 0) Edit(m, rng);
            var bounded = new StarField();
            bounded.Work(m.Grid);
            var whole = new StarField();
            whole.Work(m.Grid, new Box(-StarField.Margin, -StarField.Margin, w - 1 + StarField.Margin, h - 1 + StarField.Margin));
            var r = bounded.Region;
            Assert.True(m.Grid.Built.IsEmpty || (r.Left < m.Grid.Built.Left && r.Right > m.Grid.Built.Right));
            for (int y = whole.Region.Top; y <= whole.Region.Bottom; y++)
                for (int x = whole.Region.Left; x <= whole.Region.Right; x++)
                {
                    // What the shader reads at a cell: its texel, or past the texture's edge the edge's.
                    int bx = Math.Clamp(x, r.Left, r.Right) - r.Left, by = Math.Clamp(y, r.Top, r.Bottom) - r.Top;
                    int at = ((y - whole.Region.Top) * whole.Region.Width + x - whole.Region.Left) * 4, shown = (by * r.Width + bx) * 4;
                    Assert.True(whole.Texels.AsSpan(at, 4).SequenceEqual(bounded.Texels.AsSpan(shown, 4)), $"world {world}, cell {x},{y}");
                }
        }
    }

    [Fact]
    public void StarlightWithNothingBuiltIsOpenStarsAllRound()
    {
        var m = new Registry();
        World.Create(1, Layout.Parse(["      ", "      "]), m.All);
        var field = new StarField();
        field.Work(m.Grid);
        var open = field.Texels.AsSpan(0, 4).ToArray();
        Assert.Equal(0, open[3]);
        Assert.True(open[0] > 0);
        for (int i = 0; i < field.Texels.Length; i += 4) Assert.Equal(open, field.Texels.AsSpan(i, 4).ToArray());
    }

    [Fact]
    public void StarlightWorksAChangeOutWithinAReachShortOfTheWholeRegion() =>
        Assert.InRange(new StarField().Reach, 1, 60);

    /// <summary>
    /// Starlight followed around the cells the grid's log names comes out byte for byte as worked out anew,
    /// over ships wider than the reach and narrower and halls where the light runs far, edited inside, on
    /// the hull and out in space, what is built growing and shrinking, and several edits far apart at once.
    /// </summary>
    [Fact]
    public void StarlightFollowedAroundTheChangesIsWhatWorkingItOutAnewGives()
    {
        var rng = new Random(12);
        int windowed = 0;
        for (int world = 0; world < 24; world++)
        {
            int w = world % 3 == 0 ? rng.Next(6, 40) : rng.Next(300, 420), h = world % 3 == 0 ? rng.Next(6, 28) : rng.Next(30, 90);
            // A hall of back walls with a few holes, where the stars' light runs far, or cells of anything.
            var m = world % 3 == 2 ? Hall(rng, w, h) : Ship(rng, w, h);
            var followed = new StarField();
            followed.Work(m.Grid);
            for (int step = 0; step < 12; step++)
            {
                int version = m.Grid.Version;
                for (int k = rng.Next(1, 3); k > 0; k--) Edit(m, rng);
                var changed = m.Grid.ChangedSince(version)!
                    .Where(c => (c.Layers & (GridLayers.Structure | GridLayers.Back | GridLayers.Machine)) != 0).Select(c => c.Cell).ToList();
                if (changed.Count == 0) continue;
                var (was, wasRegion) = (followed.Texels.ToArray(), followed.Region);
                followed.Follow(m.Grid, changed);
                var whole = new StarField();
                whole.Work(m.Grid);
                Assert.Equal(whole.Region, followed.Region);
                Assert.True(whole.Texels.AsSpan().SequenceEqual(followed.Texels), $"world {world}, step {step}");
                // What it says it wrote holds every texel that changed: all the tiles sent again need (StarTiles).
                var r = followed.Region;
                if (r == wasRegion)
                    for (int i = 0; i < was.Length; i++)
                        if (was[i] != followed.Texels[i])
                            Assert.Contains(followed.Written, b => b.Contains(r.Left + i / 4 % r.Width, r.Top + i / 4 / r.Width));
                if (whole.Region.Width > 4 * new StarField().Reach + 1) windowed++;
            }
        }
        Assert.True(windowed > 50, $"{windowed} changes followed in a region wider than their window");
    }

    [Fact]
    public void DrawnLayersFindTheCellsAWholeCompareFindsWithTogglesInFlightAndPastTheLog()
    {
        var rng = new Random(9);
        for (int world = 0; world < 60; world++)
        {
            int w = rng.Next(6, 40), h = rng.Next(6, 24);
            var m = Ship(rng, w, h);
            var drawn = new DrawnLayers(w, h);
            int[][] shown = [new int[w * h], new int[w * h], new int[w * h], new int[w * h]];
            for (int step = 0; step < 40; step++)
            {
                if (step > 0 && rng.Next(3) != 0) Edit(m, rng);
                // Now and then more written than the grid's log holds: the drawing looks within what is built again.
                if (world % 4 == 0 && step == 20) Outrun(m, rng);
                var view = InFlight(m, rng, step);
                int[][] now =
                [
                    Shown(m.Grid.StructureLayer, view.Structure), m.Grid.MaterialLayer.ToArray(),
                    m.Grid.BackLayer.ToArray(), m.Grid.BackMaterialLayer.ToArray(),
                ];
                var expected = Enumerable.Range(0, w * h).Where(c => now.Where((layer, k) => layer[c] != shown[k][c]).Any())
                    .Select(c => new DrawnLayers.Change(c, shown[0][c])).ToList();
                var changes = drawn.Follow(m.Grid, view);
                Assert.Equal(expected, changes);
                Assert.Equal(now, new[] { drawn.Structure, drawn.Material, drawn.Back, drawn.BackMaterial });
                shown = now;
            }
        }
    }

    [Fact]
    public void DrawnLayersCallStaleEachChangedCellAndItsNeighboursOnTheGridOnce()
    {
        var drawn = new DrawnLayers(4, 3);
        var stale = drawn.Stale([new DrawnLayers.Change(4, 0), new DrawnLayers.Change(5, 0), new DrawnLayers.Change(11, 0)]);
        Assert.Equal(new[] { 0, 1, 4, 5, 6, 7, 8, 9, 10, 11 }, stale.Order());
    }

    [Fact]
    public void ADoorCellComeCallsStaleTheWholeDoorwayItJoinsDrawnByWhereItsCellsLie()
    {
        // A door two thick and six tall, a cell come under its left column: every cell of it is drawn anew (0104).
        var drawn = new DrawnLayers(4, 10);
        for (int y = 0; y < 7; y++)
            for (int x = 1; x < 3; x++)
                if (y < 6 || x == 1) drawn.Structure[y * 4 + x] = Structure.DoorClosed;
        var stale = drawn.Stale([new DrawnLayers.Change(6 * 4 + 1, Structure.Empty)]);
        Assert.Subset(stale.ToHashSet(), Enumerable.Range(0, 6).SelectMany(y => new[] { y * 4 + 1, y * 4 + 2 }).ToHashSet());
    }

    /// <summary>A back wall built and taken apart again and again in one cell, more writes than the grid's log holds.</summary>
    private static void Outrun(Registry m, Random rng)
    {
        int cell = rng.Next(m.Grid.Width * m.Grid.Height), version = m.Grid.Version;
        for (int i = 0; i < 34_000; i++)
            if (m.Grid.BackLayer[cell] == Back.Wall) m.Grid.Clear(cell, "back");
            else if (m.Grid.PlacementProblem(cell, Placement.BackWall()) == null) m.Grid.Place(cell, Placement.BackWall());
        Assert.Null(m.Grid.ChangedSince(version));
    }

    /// <summary>Now and then some doors toggled, as a peer's in flight; else nothing in flight.</summary>
    private static ShownGrid InFlight(Registry m, Random rng, int step)
    {
        if (rng.Next(2) == 0) return new ShownGrid($"{step}", null, null);
        var cells = Enumerable.Range(0, m.Grid.Width * m.Grid.Height)
            .Where(c => m.Grid.StructureAt(c) is Structure.DoorClosed or Structure.DoorOpen).ToList();
        var toggles = new List<Command>();
        for (int i = rng.Next(1, 4); i > 0 && cells.Count > 0; i--)
        {
            int cell = cells[rng.Next(cells.Count)];
            toggles.Add(new SetDoor(cell, rng.Next(2) == 0));
        }
        return new ShownGrid($"{step}:toggles", m.Grid.AfterToggles(toggles), null);
    }

    private static int[] Shown(ReadOnlySpan<int> layer, IReadOnlyDictionary<int, int>? cells)
    {
        int[] shown = layer.ToArray();
        foreach (var (cell, code) in cells ?? new Dictionary<int, int>()) shown[cell] = code;
        return shown;
    }
}
