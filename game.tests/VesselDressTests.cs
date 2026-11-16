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
        // Its decor in reading order of its rooms: the pool's, the restaurant's, the hangar's pad and bay doors; then the fin's emblem, the
        // bow visor's chevrons and every container's two digits.
        var pictures = dress.Decors.Select(d => d.Picture).ToList();
        Assert.Equal(["vessel_pool_tiles", "vessel_lifebuoy", "vessel_wainscot", "vessel_bottles", "vessel_pad", .. BayDoors, "vessel_emblem"], pictures.Take(9));
        int chevrons = pictures.Skip(9).TakeWhile(p => p == VesselDress.Chevron).Count();
        Assert.True(chevrons > 0, "no chevrons");
        Assert.All(pictures.Skip(9 + chevrons), p => Assert.StartsWith("vessel_digit_", p));
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
        // Every tanker's name its own, its code its letters; no hangar, so its only decor its containers' numbers.
        for (uint seed = 1; seed <= 16; seed++)
        {
            var v = Of("tanker", seed);
            var other = new VesselDress(v);
            Assert.All(other.Decors, d => Assert.StartsWith("vessel_digit_", d.Picture));
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
        // Its rooms' decor and bay doors; the fin's emblem shows only with its fin, a machine, which this grid leaves out.
        Assert.All(dress.Decors, d => Assert.Equal(d.On != VesselDress.DecorOn.Machine, dress.Shows(m.Grid, d)));
        // The pool hall's tiles reach up past the floor of the deck it spans first: a cell on that upper deck stripped hides them.
        var tiles = dress.Decors.Single(d => d.Picture == "vessel_pool_tiles");
        var pool = v.Compartments.Single(c => c.Role == VesselRole.Pool);
        var high = tiles.Cells.First(c => c.Y < v.Decks[pool.Deck].Floor);
        m.Grid.Clear(dress.CellOf(high.X, high.Y, m.Grid.Width), "back");
        Assert.False(dress.Shows(m.Grid, tiles));
        Assert.All(dress.Decors.Where(d => d != tiles && d.On != VesselDress.DecorOn.Machine), d => Assert.True(dress.Shows(m.Grid, d), d.Picture));
        // The wainscot under a wall built over it.
        var wainscot = dress.Decors.Single(d => d.Picture == "vessel_wainscot");
        m.Grid.Place(dress.CellOf(wainscot.Cells[3].X, wainscot.Cells[3].Y, m.Grid.Width), Placement.Wall());
        Assert.False(dress.Shows(m.Grid, wainscot));
    }

    /// <summary>A hangar's bay doors: its ends and leaves, a picture each.</summary>
    private static readonly string[] BayDoors = ["vessel_bay_door_left", "vessel_bay_door", "vessel_bay_door_right"];

    [Fact]
    public void BayDoorsLieInTheBottomArmourUnderEachHangarAndGoOnceACellOfThemIsNoLongerAHullWall()
    {
        var v = Of("liner", 2);
        var m = Laid(v);
        var dress = new VesselDress(v);
        var hangar = v.Compartments.Single(c => c.Role == VesselRole.Hangar);
        var doors = dress.Decors.Where(d => d.On == VesselDress.DecorOn.Wall && d.Picture != VesselDress.Chevron).ToList();
        Assert.Equal(BayDoors, doors.Select(d => d.Picture));
        // Along the hangar's floor, inset at both ends, over the armour's rows; every cell of them a hull wall, as stamped.
        Assert.Equal((hangar.Left + VesselDress.BayDoorInset, hangar.Left + hangar.Width - VesselDress.BayDoorInset), (doors[0].X, doors[^1].X + doors[^1].Width));
        Assert.All(doors, d => Assert.Equal((hangar.Floor, V.Class("liner")!.ArmourDepth, d.Width * d.Height), (d.Y, d.Height, d.Cells.Count)));
        Assert.Equal(VesselDress.DecorTint.Hull, doors[1].Tint);
        Assert.All(doors, d => Assert.True(dress.Shows(m.Grid, d), d.Picture));
        // A cell of a leaf cut away: that leaf goes, the ends stay; a wall built back there of plain stuff does not bring it back.
        var cut = doors[1].Cells[5];
        int cell = dress.CellOf(cut.X, cut.Y, m.Grid.Width);
        m.Grid.Clear(cell, "structure");
        Assert.Equal([true, false, true], doors.Select(d => dress.Shows(m.Grid, d)));
        m.Grid.Place(cell, Placement.Wall());
        Assert.False(dress.Shows(m.Grid, doors[1]));
    }

    [Fact]
    public void TheFinsEmblemShowsInTheStripesColourWhileItsFinStands()
    {
        var v = Of("liner", 2);
        var m = Laid(v);
        var dress = new VesselDress(v);
        var fin = v.Outside.Single(f => f.Kind == V.Fin);
        var emblem = Assert.Single(dress.Decors, d => d.On == VesselDress.DecorOn.Machine && d.Machine == V.Fin);
        Assert.Equal(("vessel_emblem", fin.X, fin.Y, V.Fin, VesselDress.DecorTint.Stripe), (emblem.Picture, emblem.X, emblem.Y, emblem.Machine, emblem.Tint));
        int cell = dress.CellOf(fin.X, fin.Y, m.Grid.Width);
        Assert.False(dress.Shows(m.Grid, emblem));
        m.Grid.Place(cell, new Placement("machine", V.Fin));
        Assert.True(dress.Shows(m.Grid, emblem));
        m.Grid.Clear(cell, "machine");
        Assert.False(dress.Shows(m.Grid, emblem));
        // No fin on a tanker, so no emblem.
        Assert.DoesNotContain(new VesselDress(Of("tanker", 1)).Decors, d => d.Picture == "vessel_emblem");
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

    [Fact]
    public void ALinersBowUnderTheBeamOverItsHangarIsDrawnAsAHingedVisorWithChevronsAlongItsEdge()
    {
        for (uint seed = 1; seed <= 6; seed++)
        {
            var v = Of("liner", seed);
            var m = Laid(v);
            var dress = new VesselDress(v);
            var bow = v.Sections.Single(s => s.Kind == Vessel.SectionKind.Bow);
            var hangar = v.Compartments.Single(c => c.Role == VesselRole.Hangar);
            var (top, lip) = VesselDress.VisorOf(v)!.Value;
            Assert.Equal((hangar.Top - 1, hangar.Floor, bow.Left), (top, lip, hangar.Left + hangar.Width));
            var drawn = Dressed(dress, v, m.Grid).Where(d => d.X >= bow.Left && d.Dress.Wall is { } p && !p.Sheet.StartsWith("vessel_bow_")).ToList();
            // Every plate from the beam down a visor's piece, every one over it the hull's; hinges on the beam, its aft frame, the lip.
            Assert.All(drawn, d => Assert.Equal(d.Y >= top ? VesselDress.VisorSheet : VesselDress.HullSheet, d.Dress.Wall!.Value.Sheet));
            var pieces = drawn.Where(d => d.Y >= top).Select(d => (VesselDress.Visor)d.Dress.Wall!.Value.X).ToHashSet();
            Assert.Superset(new HashSet<VesselDress.Visor> { VesselDress.Visor.Corner, VesselDress.Visor.Beam, VesselDress.Visor.Hinge, VesselDress.Visor.Frame, VesselDress.Visor.Lip, VesselDress.Visor.Plate }, pieces);
            // Chevrons a cell inside the hull's outermost along the visor's bottom and slope, each on a hull wall that shows.
            var chevrons = dress.Decors.Where(d => d.Picture == VesselDress.Chevron).ToList();
            Assert.NotEmpty(chevrons);
            Assert.All(chevrons, c => Assert.True(c.X >= bow.Left && c.Y > top && dress.Shows(m.Grid, c), $"({c.X}, {c.Y})"));
        }
        // A tanker has no hangar: its bow is plates.
        Assert.All(Enumerable.Range(1, 8), s => Assert.Null(VesselDress.VisorOf(Of("tanker", (uint)s))));
    }

    [Theory]
    [InlineData("liner")]
    [InlineData("tanker")]
    [InlineData("freighter")]
    public void EveryContainerShowsATwoDigitNumberFromItsSeedWhileItStands(string cls)
    {
        var numbers = new HashSet<int>();
        for (uint seed = 1; seed <= 12; seed++)
        {
            var v = Of(cls, seed);
            VesselDress a = new(v), b = new(Of(cls, seed));
            var containers = v.Fittings.Concat(v.Outside).Where(f => V.FirstLook(f.Kind) is V.Container or V.Cargo).ToList();
            var digits = a.Decors.Where(d => d.Picture.StartsWith("vessel_digit_")).ToList();
            Assert.Equal(2 * containers.Count, digits.Count);
            static (string, int, int, string?, (int, int)) Key(VesselDress.Decor d) => (d.Picture, d.X, d.Y, d.Machine, d.Cells[0]);
            Assert.Equal(digits.Select(Key), b.Decors.Where(d => d.Picture.StartsWith("vessel_digit_")).Select(Key));
            foreach (var f in containers)
            {
                int n = VesselDress.ContainerNumber(v.Seed, f.X, f.Y, v.Width);
                Assert.InRange(n, 1, 99);
                numbers.Add(n);
                var (x, y) = (f.X + VesselDress.NumberAt.X, f.Y + VesselDress.NumberAt.Y);
                Assert.Contains(digits, d => (d.Picture, d.X, d.Y, d.Machine, d.Cells[0]) == (VesselDress.Digit(n / 10), x, y, f.Kind, (f.X, f.Y)));
                Assert.Contains(digits, d => (d.Picture, d.X, d.Y, d.Machine, d.Cells[0]) == (VesselDress.Digit(n % 10), x + 1, y, f.Kind, (f.X, f.Y)));
                // Over its plate, within its picture.
                var spec = Machines.Get(f.Kind);
                Assert.True(x + 2 <= f.X + spec.Width && y < f.Y + spec.Height);
            }
        }
        Assert.True(numbers.Count > 10, $"{numbers.Count} numbers");
        // Shown while its container stands, its top-left where it was stamped.
        var liner = Of("liner", 1);
        var m = Laid(liner);
        var dress = new VesselDress(liner);
        var digit = dress.Decors.First(d => d.Picture.StartsWith("vessel_digit_"));
        int cell = dress.CellOf(digit.Cells[0].X, digit.Cells[0].Y, m.Grid.Width);
        Assert.False(dress.Shows(m.Grid, digit));
        m.Grid.Place(cell, new Placement("machine", digit.Machine!));
        Assert.True(dress.Shows(m.Grid, digit));
    }
}
