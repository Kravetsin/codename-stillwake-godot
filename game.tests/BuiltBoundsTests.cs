using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The client looks only within what is built (F-6 slice 0): starlight and the drawn layers come out as
/// they did over the whole grid, for ships anywhere in worlds of space, edited inside, on the hull and
/// out in space, with toggles in flight.
/// </summary>
public class BuiltBoundsTests
{
    private static readonly Placement[] Placements =
    [
        Placement.Wall(), Placement.Wall("iron"), Placement.BackWall(), Placement.BackWall("iron"),
        new("structure", "floor"), new("structure", "ladder"), new("structure", "door"),
        new("conduit", "pipe"), new("conduit", "valve"), new("machine", "lamp"), new("machine", "tank"),
    ];

    private static readonly string[] Layers = ["structure", "conduit", "back", "machine"];

    /// <summary>A world of space <paramref name="w"/>×<paramref name="h"/> with a ship of random cells in a box somewhere in it, maybe against an edge.</summary>
    private static Registry Ship(Random rng, int w, int h)
    {
        var m = new Registry();
        World.Create(1, Layout.Parse(Enumerable.Repeat(new string(' ', w), h).ToList()), m.All);
        int left = rng.Next(w - 3), top = rng.Next(h - 3), right = rng.Next(left + 2, w), bottom = rng.Next(top + 2, h);
        for (int i = 0; i < (right - left + 1) * (bottom - top + 1); i++)
            Place(m, rng, (top + rng.Next(bottom - top + 1)) * w + left + rng.Next(right - left + 1));
        return m;
    }

    private static void Place(Registry m, Random rng, int cell)
    {
        var p = Placements[rng.Next(Placements.Length)];
        if (m.Grid.PlacementProblem(cell, p) == null) m.Grid.Place(cell, p);
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
    public void DrawnLayersFindTheCellsAWholeCompareFindsWithTogglesInFlight()
    {
        var rng = new Random(9);
        for (int world = 0; world < 60; world++)
        {
            int w = rng.Next(6, 40), h = rng.Next(6, 24);
            var m = Ship(rng, w, h);
            var drawn = new DrawnLayers(w, h);
            int[][] shown = [new int[w * h], new int[w * h], new int[w * h], new int[w * h], new int[w * h]];
            for (int step = 0; step < 40; step++)
            {
                if (step > 0 && rng.Next(3) != 0) Edit(m, rng);
                var view = InFlight(m, rng, step);
                int[][] now =
                [
                    Shown(m.Grid.StructureLayer, view.Structure), m.Grid.MaterialLayer.ToArray(),
                    Shown(m.Grid.ConduitLayer, view.Conduit), m.Grid.BackLayer.ToArray(), m.Grid.BackMaterialLayer.ToArray(),
                ];
                var expected = Enumerable.Range(0, w * h).Where(c => now.Where((layer, k) => layer[c] != shown[k][c]).Any())
                    .Select(c => new DrawnLayers.Change(c, shown[0][c], shown[2][c])).ToList();
                var changes = drawn.Follow(m.Grid, view);
                Assert.Equal(expected, changes);
                Assert.Equal(now, new[] { drawn.Structure, drawn.Material, drawn.Conduit, drawn.Back, drawn.BackMaterial });
                shown = now;
            }
        }
    }

    [Fact]
    public void DrawnLayersCallStaleEachChangedCellAndItsNeighboursOnTheGridOnce()
    {
        var drawn = new DrawnLayers(4, 3);
        var stale = drawn.Stale([new DrawnLayers.Change(4, 0, 0), new DrawnLayers.Change(5, 0, 0), new DrawnLayers.Change(11, 0, 0)]);
        Assert.Equal(new[] { 0, 1, 4, 5, 6, 7, 8, 9, 10, 11 }, stale.Order());
    }

    /// <summary>Now and then some doors and valves toggled, as a peer's in flight; else nothing in flight.</summary>
    private static ShownGrid InFlight(Registry m, Random rng, int step)
    {
        if (rng.Next(2) == 0) return new ShownGrid($"{step}", null, null);
        var cells = Enumerable.Range(0, m.Grid.Width * m.Grid.Height)
            .Where(c => m.Grid.StructureAt(c) is Structure.DoorClosed or Structure.DoorOpen || m.Grid.ConduitAt(c) is Conduit.ValveOpen or Conduit.ValveClosed).ToList();
        var toggles = new List<Command>();
        for (int i = rng.Next(1, 4); i > 0 && cells.Count > 0; i--)
        {
            int cell = cells[rng.Next(cells.Count)];
            toggles.Add(rng.Next(2) == 0 ? new ToggleDoor(cell) : new ToggleValve(cell));
        }
        var (structure, conduit) = m.Grid.AfterToggles(toggles);
        return new ShownGrid($"{step}:toggles", structure, conduit);
    }

    private static int[] Shown(ReadOnlySpan<int> layer, IReadOnlyDictionary<int, int>? cells)
    {
        int[] shown = layer.ToArray();
        foreach (var (cell, code) in cells ?? new Dictionary<int, int>()) shown[cell] = code;
        return shown;
    }
}
