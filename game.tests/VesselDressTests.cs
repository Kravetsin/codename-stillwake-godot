using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>An abandoned vessel's skin (derelict-looks): drawn from its seed alone, shown where its hull still stands.</summary>
public class VesselDressTests
{
    private const int Up = 1, Right = 2, Down = 4, Left = 8;

    private static Vessel Of(string cls, uint seed) => Shipyard.Describe(V.Class(cls)!, seed);

    /// <summary>A vessel world's grid with <paramref name="v"/> laid as <c>Places</c> lays it, its frame at the margin; machines left out.</summary>
    internal static Registry Laid(Vessel v)
    {
        var m = new Registry();
        Stillwake.Sim.World.Create(1, Shipyard.Space(v), m.All);
        int w = m.Grid.Width;
        foreach (var part in Shipyard.Stamp(v))
        {
            int cell = (V.Margin + part.Y) * w + V.Margin + part.X;
            if (part.Layer == "machine" || part.Layer == "back" && m.Grid.BackAt(cell) != null) continue;
            m.Grid.Place(cell, new Placement(part.Layer, part.Kind, part.Material));
        }
        return m;
    }

    /// <summary>Every cell of the vessel's frame in the grid, with what it draws.</summary>
    private static IEnumerable<(int Cell, int X, int Y, VesselDress.Dress Dress)> Dressed(VesselDress dress, Vessel v, IGridQueries grid)
    {
        for (int y = 0; y < v.Height; y++)
            for (int x = 0; x < v.Width; x++)
            {
                int cell = dress.CellOf(x, y, grid.Width);
                yield return (cell, x, y, dress.At(grid, cell));
            }
    }

    [Fact]
    public void OneSeedIsPaintedAsPinned()
    {
        var dress = new VesselDress(Of("freighter", 1));
        Assert.Equal(("LOON", "TN-4769", 3), (dress.Name, dress.Code, dress.Livery));
    }

    [Fact]
    public void OneLinerIsPaintedAsPinnedInItsOwnLightLiveryAndDressedRoomByRoom()
    {
        var dress = new VesselDress(Of("liner", 1));
        Assert.Equal(("LOON", "TN-4769", 1), (dress.Name, dress.Code, dress.Livery));
        Assert.Same(VesselLooks.LinerLiveries[dress.Livery], dress.Colours);
        // Its decor in reading order of its rooms: the pool's, the restaurant's, the hangar's pad.
        Assert.Equal(["vessel_pool_tiles", "vessel_lifebuoy", "vessel_wainscot", "vessel_bottles", "vessel_pad"], dress.Decors.Select(d => d.Picture));
        // The old classes keep their four, in order.
        Assert.Same(VesselLooks.Liveries, VesselLooks.LiveriesOf("freighter"));
        Assert.Equal(4, VesselLooks.Liveries.Count);
    }

    [Fact]
    public void OneTankerIsPaintedAsPinnedInItsOwnLiveryNameAndLetters()
    {
        var dress = new VesselDress(Of("tanker", 1));
        Assert.Equal(("GRAVITAS", "TK-4769", 1), (dress.Name, dress.Code, dress.Livery));
        Assert.Same(VesselLooks.TankerLiveries[dress.Livery], dress.Colours);
        Assert.Same(VesselLooks.TankerLiveries, VesselLooks.LiveriesOf("tanker"));
        // Every tanker's name its own, its code its letters; its one decor a hangar's pad, where it has a hangar.
        for (uint seed = 1; seed <= 16; seed++)
        {
            var v = Of("tanker", seed);
            var other = new VesselDress(v);
            Assert.Equal(v.Compartments.Any(c => c.Role == VesselRole.Hangar) ? ["vessel_pad"] : [], other.Decors.Select(d => d.Picture));
            Assert.Contains(other.Name, VesselLooks.TankerNames);
            Assert.StartsWith(VesselLooks.TankerPrefix + "-", other.Code);
        }
    }

    [Fact]
    public void ADecorIsHiddenOnceOneOfItsCellsChangesEvenOnOneDeckOfATallRoom()
    {
        var v = Of("liner", 2);
        var m = Laid(v);
        var dress = new VesselDress(v);
        Assert.All(dress.Decors, d => Assert.True(dress.Shows(m.Grid, d), d.Picture));
        // The pool hall's tiles reach up past the floor of the deck it spans first: a cell on that upper deck stripped hides them.
        var tiles = dress.Decors.Single(d => d.Picture == "vessel_pool_tiles");
        var pool = v.Compartments.Single(c => c.Role == VesselRole.Pool);
        var high = tiles.Cells.First(c => c.Y < v.Decks[pool.Deck].Floor);
        m.Grid.Clear(dress.CellOf(high.X, high.Y, m.Grid.Width), "back");
        Assert.False(dress.Shows(m.Grid, tiles));
        Assert.All(dress.Decors.Where(d => d != tiles), d => Assert.True(dress.Shows(m.Grid, d), d.Picture));
        // The wainscot under a wall built over it.
        var wainscot = dress.Decors.Single(d => d.Picture == "vessel_wainscot");
        m.Grid.Place(dress.CellOf(wainscot.Cells[3].X, wainscot.Cells[3].Y, m.Grid.Width), Placement.Wall());
        Assert.False(dress.Shows(m.Grid, wainscot));
    }

    [Theory]
    [InlineData("freighter", 5u)]
    [InlineData("shuttle", 9u)]
    [InlineData("liner", 3u)]
    [InlineData("tanker", 4u)]
    public void TheSameSeedIsDressedAlikeTwice(string cls, uint seed)
    {
        var m = Laid(Of(cls, seed));
        VesselDress a = new(Of(cls, seed)), b = new(Of(cls, seed));
        Assert.Equal((a.Name, a.Code, a.Livery), (b.Name, b.Code, b.Livery));
        Assert.Equal(a.Labels, b.Labels);
        var v = Of(cls, seed);
        Assert.Equal(Dressed(a, v, m.Grid).Select(d => d.Dress), Dressed(b, v, m.Grid).Select(d => d.Dress));
        // Painted at all: plates, the stripe, the name and the code.
        Assert.Contains(Dressed(a, v, m.Grid), d => d.Dress.Stripe != null);
        Assert.Equal(2, a.Labels.Count);
        Assert.Contains(Dressed(a, v, m.Grid), d => d.Dress.Paint != null);
    }

    [Fact]
    public void ANamePieceIsHiddenOnceItsCellIsNoLongerAHullWall()
    {
        var v = Of("freighter", 3);
        var m = Laid(v);
        var dress = new VesselDress(v);
        var painted = Dressed(dress, v, m.Grid).First(d => d.Dress.Paint != null);
        m.Grid.Clear(painted.Cell, "structure");
        Assert.Null(dress.At(m.Grid, painted.Cell).Paint);
        // A wall built there again, of the player's own: still no paint, nor plate.
        m.Grid.Place(painted.Cell, Placement.Wall());
        Assert.Equal(default, dress.At(m.Grid, painted.Cell));
    }

    [Theory]
    [InlineData("freighter", 1u)]
    [InlineData("freighter", 2u)]
    [InlineData("shuttle", 4u)]
    [InlineData("shuttle", 6u)]
    public void BowDiagonalsLieOnlyOnTheBowsSlopeCells(string cls, uint seed)
    {
        var v = Of(cls, seed);
        var m = Laid(v);
        var bow = v.Sections.Single(s => s.Kind == Vessel.SectionKind.Bow);
        // The slope: from Run columns before the first step, the hull's edge row of each column while it still falls (or rises).
        var slope = new HashSet<(int, int)>();
        int start = bow.Left + bow.Flat - bow.Run;
        for (int i = 0; i < bow.Fall * bow.Run; i++) slope.Add((start + i, bow.Top + i / bow.Run));
        for (int i = 0; i < bow.Rise * bow.Run; i++) slope.Add((start + i, bow.Bottom - i / bow.Run));
        var diagonal = Dressed(new VesselDress(v), v, m.Grid).Where(d => d.Dress.Wall?.Sheet.StartsWith("vessel_bow_") == true).Select(d => (d.X, d.Y)).ToHashSet();
        Assert.Equal(slope.Order(), diagonal.Order());
    }

    [Theory]
    [InlineData("freighter", 1u)]
    [InlineData("shuttle", 2u)]
    public void HeavyInkLiesOnlyOnSidesFacingSpaceAndLightOnSidesFacingARoom(string cls, uint seed)
    {
        var v = Of(cls, seed);
        var m = Laid(v);
        var g = m.Grid;
        int w = g.Width;
        bool Space(int cell) => g.StructureAt(cell) == Structure.Empty && g.BackAt(cell) == null;
        int heavy = 0, light = 0;
        foreach (var (cell, _, _, d) in Dressed(new VesselDress(v), v, g))
        {
            if (d.Ink is not { } ink) continue;
            var sides = ink.Split('_');
            int space = int.Parse(sides[2]), room = int.Parse(sides[3]);
            foreach (var (side, n) in (ReadOnlySpan<(int, int)>)[(Up, cell - w), (Right, cell + 1), (Down, cell + w), (Left, cell - 1)])
            {
                if ((space & side) != 0) { Assert.True(Space(n), $"heavy ink at {cell} side {side} over no space"); heavy++; }
                if ((room & side) != 0) { Assert.False(Space(n) || Structures.Is(g.StructureAt(n), Trait.Terrain), $"light ink at {cell} side {side}"); light++; }
            }
        }
        Assert.True(heavy > 0 && light > 0);
    }
}
