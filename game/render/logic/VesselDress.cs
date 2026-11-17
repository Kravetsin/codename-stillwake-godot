using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Looks = Stillwake.Game.VesselLooks;

namespace Stillwake.Game.Render;

/// <summary>
/// How an abandoned vessel's hull is painted (derelict-looks), a rule of drawing, never the sim's: plates by their place in an
/// eight-cell panel and their row (lit top, middle, shaded bottom), rusted or pitted by <see cref="Looks.LookRng"/>; ribs in their
/// own tone; one diagonal tile on each cell of the bow's slopes; the livery's stripe along the bottom armour; the name and the code
/// painted in pieces a cell; a heavy ink line where a side faces space, a light one where it faces a room. What may be painted comes
/// only from the vessel its seed describes, stamped; the live grid decides only whether each shows: a hull wall of it still there.
/// </summary>
public sealed class VesselDress
{
    /// <summary>The sides of a cell, as <c>GridView</c> counts them.</summary>
    private const int Up = 1, Right = 2, Down = 4, Left = 8;

    /// <summary>Cells of a panel along the hull, its seam at its left.</summary>
    public const int Panel = 8;

    /// <summary>The plates' sheet: <see cref="Panel"/> columns, a row for each role and its variant (<see cref="Role"/> × 3 + variant).</summary>
    public const string HullSheet = "vessel_hull";

    /// <summary>The ribs' sheet: two columns, a row a role.</summary>
    public const string RibSheet = "vessel_rib";

    /// <summary>The livery stripe's sheet: a cell plain, then scuffed ones.</summary>
    public const string StripeSheet = "vessel_stripe";

    /// <summary>A bow visor's sheet (big-vessels), a cell a piece of it in one row (<see cref="Visor"/>), tinted as the hull is.</summary>
    public const string VisorSheet = "vessel_visor";

    /// <summary>A bow visor's pieces: a plate, one over a lap seam, the beam it hangs from, a hinge on the beam, its aft frame, the two met, the ramp's lip.</summary>
    public enum Visor : byte { Plate, Seam, Beam, Hinge, Frame, Corner, Lip }

    /// <summary>A bow visor's hinges stand on its beam every this many columns.</summary>
    public const int HingeEvery = 4;

    /// <summary>A bow visor's plates lap every this many rows under its beam.</summary>
    public const int VisorSeamEvery = 4;

    /// <summary>How many stripe cells the sheet holds: plain, then scuffs.</summary>
    public const int Stripes = 4;

    /// <summary>A plate's row: lit along a top open to space or a room, in shade along a bottom over space, plain between.</summary>
    public enum Role { Top, Middle, Bottom }

    /// <summary>A bow slope's diagonal over <paramref name="run"/> cells, its top's or its bottom's, of hull or of the cockpit's glass.</summary>
    public static string BowName(int run, bool bottom, bool glass) => $"vessel_bow_{run}_{(bottom ? "bottom" : "top")}{(glass ? "_glass" : "")}";

    /// <summary>The ink round a hull cell: heavy on the sides <paramref name="space"/>, light on the sides <paramref name="room"/>.</summary>
    public static string InkName(int space, int room) => $"vessel_ink_{space}_{room}";

    /// <summary>The side pictures an ink tile is laid from: heavy or light, a side each.</summary>
    public static string InkSideName(bool heavy, int side) => $"vessel_ink_{(heavy ? "heavy" : "light")}_{side}";

    /// <summary>Every ink tile: each way of sides heavy and light, none on both.</summary>
    public static IEnumerable<(int Space, int Room)> Inks() =>
        from space in Enumerable.Range(0, 16) from room in Enumerable.Range(0, 16) where (space & room) == 0 && (space | room) != 0 select (space, room);

    /// <summary>The runs a bow's slope may take, over every class: a diagonal picture each.</summary>
    public static IEnumerable<int> Runs() => Vessels.Classes.SelectMany(c => Enumerable.Range(c.BowRun.Min, c.BowRun.Max - c.BowRun.Min + 1)).Distinct().Order();

    public static readonly int HullCode = Materials.Get(Vessels.Hull).Code, GlassCode = Materials.Get(Vessels.Glass).Code;

    /// <summary>A cell of a sheet in the atlas.</summary>
    public readonly record struct Piece(string Sheet, int X, int Y);

    /// <summary>
    /// What a wall cell draws: its plate in place of its wall's picture (<paramref name="Tinted"/> by the livery's hull, or as it is for
    /// glass), its ink in place of its edges, its stripe and its piece of paint over it; <paramref name="Cut"/> for a diagonal, which
    /// draws no back wall, the stars showing past its slope.
    /// </summary>
    public readonly record struct Dress(Piece? Wall, bool Tinted, string? Ink, Piece? Stripe, (int X, int Y)? Paint, bool Cut);

    /// <summary>
    /// A painted label: its text in the large letters or the small, over the cells <c>Left</c>, <c>Top</c>, <c>Width</c> by <c>Height</c> of
    /// the vessel's frame, laid in the paint sheet from its row <c>Row</c>; its letters from pixel <c>TextY</c> of its top.
    /// </summary>
    public sealed record Label(string Text, bool Large, int Left, int Top, int Width, int Height, int Row, int TextY);

    private enum Kind : byte { None, Plate, Rib, Visor }

    private struct Look
    {
        public Kind Kind;
        public byte Column, Row, Bow, Run, Piece;
        public bool Glass;
        public sbyte Stripe;
        public short PaintX, PaintY;
    }

    private readonly Look[] _looks;
    /// <summary>The cells of the frame in a doorway stamped open and jammed (<see cref="Jammed"/>).</summary>
    private readonly HashSet<int> _jammed = [];
    private readonly int _width, _height;
    private readonly (int X, int Y) _corner;

    /// <summary>The vessel's seed: what every look of it is drawn from.</summary>
    public uint Seed { get; }

    /// <summary>Its livery, by its place in its class's (<see cref="Looks.LiveriesOf"/>); <see cref="Colours"/> it.</summary>
    public int Livery { get; }

    public Looks.Livery Colours { get; }

    public string Name { get; }
    public string Code { get; }

    /// <summary>The labels painted on it, the code's first; none that fits nowhere.</summary>
    public IReadOnlyList<Label> Labels { get; }

    /// <summary>The paint sheet's size in cells: every label stacked, a clear row between.</summary>
    public (int Width, int Height) PaintCells { get; }

    /// <summary>
    /// The dress of the vessel a surface of <paramref name="rules"/> holds from <paramref name="seed"/> and its forced chain
    /// <paramref name="wreck"/> (vessel-wrecks); none off a vessel.
    /// </summary>
    public static VesselDress? For(Surfaces.Kind rules, uint seed, IReadOnlyList<WreckStep>? wreck = null) =>
        rules is VesselKind kind ? new VesselDress(Shipyard.Build(Vessels.Class(kind.Class)!, seed, wreck)) : null;

    /// <param name="made">The vessel as built, its frame's top-left at (<c>Vessels.Margin</c>, <c>Margin</c>) of the grid as a vessel world lays it.</param>
    public VesselDress(Built made)
    {
        var vessel = made.Vessel;
        Seed = vessel.Seed;
        Livery = Looks.LiveryOf(vessel.Class, Seed);
        Colours = Looks.LiveriesOf(vessel.Class)[Livery];
        // A scavenged wreck's paint long faded (vessel-wrecks).
        if (made.Wreck is { Scavenged: true })
            Colours = Looks.Faded(Colours);
        Name = Looks.NameOf(vessel.Class, Seed);
        Code = Looks.CodeOf(vessel.Class, Seed);
        _corner = (Vessels.Margin, Vessels.Margin);
        int w = _width = vessel.Width, h = _height = vessel.Height;
        _looks = new Look[w * h];

        // The built vessel: its walls, hull or glass, and what is no space (a structure or a back wall).
        var wall = new byte[w * h];
        var taken = new bool[w * h];
        var parts = made.Parts;
        foreach (var part in parts)
        {
            if (part.Layer == "machine") continue;
            foreach (var (x, y) in Shipyard.Covers(part))
                if (x >= 0 && y >= 0 && x < w && y < h) taken[y * w + x] = true;
            if (part.Kind == "wall" && part.X >= 0 && part.Y >= 0 && part.X < w && part.Y < h) wall[part.Y * w + part.X] = part.Material == Vessels.Glass ? (byte)2 : (byte)1;
        }
        bool Hull(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && wall[y * w + x] == 1;
        bool Walled(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && wall[y * w + x] != 0;
        bool Space(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || !taken[y * w + x];

        var body = vessel.Sections.First(s => s.Kind == Vessel.SectionKind.Body);
        var ribs = vessel.Sections.Where(s => s.Kind == Vessel.SectionKind.Rib).ToList();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int cell = y * w + x;
                if (!Hull(x, y)) continue;
                ref var look = ref _looks[cell];
                var role = !Walled(x, y - 1) ? Role.Top : Space(x, y + 1) ? Role.Bottom : Role.Middle;
                var rib = ribs.FirstOrDefault(r => x >= r.Left && x < r.Left + r.Width && y >= r.Top && y <= r.Bottom);
                ulong draw = Looks.LookRng(Seed, Looks.Salt.Panel, cell);
                int variant = draw % Looks.RustOneIn == 0 ? 1 : draw / Looks.RustOneIn % Looks.PitOneIn == 0 ? 2 : 0;
                (look.Kind, look.Column) = rib != null ? (Kind.Rib, (byte)(x - rib.Left)) : (Kind.Plate, (byte)(((x - body.Left) % Panel + Panel) % Panel));
                look.Row = (byte)(rib != null ? (int)role : (int)role * 3 + variant);
                look.Stripe = -1;
                look.PaintX = look.PaintY = -1;
            }

        // The bow's slopes: from Run columns before its first step, a cell a column on the hull's edge, the line through each step's corner.
        var bow = vessel.Sections.First(s => s.Kind == Vessel.SectionKind.Bow);
        foreach (bool bottom in (bool[])[false, true])
        {
            int steps = bottom ? bow.Rise : bow.Fall, start = bow.Left + bow.Flat - bow.Run;
            for (int i = 0; i < steps * bow.Run; i++)
            {
                int x = start + i, y = bottom ? bow.Bottom - i / bow.Run : bow.Top + i / bow.Run;
                if (!Walled(x, y) || Walled(x, bottom ? y + 1 : y - 1)) continue;
                ref var look = ref _looks[y * w + x];
                (look.Bow, look.Run, look.Piece, look.Glass) = ((byte)(bottom ? 2 : 1), (byte)bow.Run, (byte)(i % bow.Run), wall[y * w + x] == 2);
            }
        }

        // A bow visor over a hangar at the bow (big-vessels): the bow's hull from the beam over the hangar down, a piece by where it lies.
        if (VisorOf(vessel) is { } visor)
            for (int x = bow.Left; x < bow.Left + bow.Width; x++)
                for (int y = visor.Top; y <= bow.RowsAt(x).Bottom; y++)
                {
                    if (!Hull(x, y)) continue;
                    ref var look = ref _looks[y * w + x];
                    (look.Kind, look.Column, look.Row) = (Kind.Visor, (byte)VisorPiece(x - bow.Left, y - visor.Top, y == visor.Lip), 0);
                }

        // The stripe along the body's bottom armour, its inner row, to where the bow's slope takes it.
        int stripe = body.Bottom - 1;
        for (int x = body.Left; x < bow.Left + bow.Width; x++)
        {
            int cell = stripe * w + x;
            if (!Hull(x, stripe) || _looks[cell].Bow != 0) continue;
            _looks[cell].Stripe = (sbyte)(Looks.Below(Seed, Looks.Salt.Stripe, cell, Looks.StripeScuffOneIn) == 0
                ? 1 + Looks.Below(Seed, Looks.Salt.Stripe, cell + w * h, Stripes - 1) : 0);
        }

        // The doorways stamped open, each from its first piece, none of it over it: one in JamOneIn jammed, every cell of it.
        var open = parts.Where(p => p.Layer == "structure" && p.Kind == "door" && p.Open).ToList();
        var pieces = open.ToDictionary(p => (p.X, p.Y));
        foreach (var first in open.Where(p => !pieces.ContainsKey((p.X, p.Y - 2))))
        {
            if (Looks.Below(Seed, Looks.Salt.Jam, first.Y * w + first.X, Looks.JamOneIn) != 0) continue;
            for (int y = first.Y; pieces.TryGetValue((first.X, y), out var piece); y += 2)
                foreach (var (x, cy) in Shipyard.Covers(piece)) _jammed.Add(cy * w + x);
        }

        Labels = Lay(vessel, body);
        foreach (var label in Labels)
            for (int dy = 0; dy < label.Height; dy++)
                for (int dx = 0; dx < label.Width; dx++)
                {
                    int x = label.Left + dx, y = label.Top + dy;
                    if (!Hull(x, y)) continue;
                    ref var look = ref _looks[y * w + x];
                    (look.PaintX, look.PaintY) = ((short)dx, (short)(label.Row + dy));
                }
        PaintCells = Labels.Count == 0 ? (0, 0) : (Labels.Max(l => l.Width), Labels[^1].Row + Labels[^1].Height);

        var built = new bool[w * h];
        foreach (var part in parts.Where(p => p.Layer == "structure"))
            foreach (var (x, y) in Shipyard.Covers(part))
                if (x >= 0 && y >= 0 && x < w && y < h) built[y * w + x] = true;
        Decors = DecorOf(vessel, built, wall);
    }

    /// <summary>
    /// What a decor lies on (big-vessels): a room's back wall, the hull's walls (a hangar's bay doors in the bottom armour, drawn over
    /// them), a part outside (the fin's emblem, drawn over it).
    /// </summary>
    public enum DecorOn { Back, Wall, Machine }

    /// <summary>What a decor painted grey is tinted by: nothing, the livery's hull, its stripe.</summary>
    public enum DecorTint { None, Hull, Stripe }

    /// <summary>
    /// A vessel's dress by its rooms and parts (big-vessels): a picture over cells <c>X</c>, <c>Y</c>, <c>Width</c> by <c>Height</c> of the
    /// frame, repeated across when <c>Tiled</c>; shown while every cell of <c>Cells</c> stays as stamped (<see cref="Shows"/>) by what it
    /// lies <c>On</c>: bare of structure on a hull back wall, a hull wall, or the top-left of a <c>Machine</c> still standing.
    /// </summary>
    public sealed record Decor(string Picture, int X, int Y, int Width, int Height, bool Tiled, IReadOnlyList<(int X, int Y)> Cells,
        DecorOn On = DecorOn.Back, string? Machine = null, DecorTint Tint = DecorTint.None);

    /// <summary>
    /// The pictures the vessel is dressed with: in reading order of its rooms a pool's tiles and lifebuoy, a restaurant's wainscot and
    /// bottles, a hangar's pad and its bay doors under it, ends and leaves; then the emblem on each fin.
    /// </summary>
    public IReadOnlyList<Decor> Decors { get; }

    /// <summary>The pool hall's tiles rise this many rows from its floor, the restaurant's wainscot this many; its bottles' shelves this far up.</summary>
    public const int TileRows = 8, WainscotRows = 3, BottlesUp = 9;

    /// <summary>A hangar's bay doors stand this many columns in from each of its ends, their hazard-painted ends this wide.</summary>
    public const int BayDoorInset = 2, BayDoorEnd = 2;

    private static List<Decor> DecorOf(Vessel vessel, bool[] built, byte[] wall)
    {
        var decor = new List<Decor>();
        int armour = Vessels.Class(vessel.Class)!.ArmourDepth;
        void Add(string picture, int x, int y, int width, int height, bool tiled, DecorOn on = DecorOn.Back, DecorTint tint = DecorTint.None)
        {
            var cells = new List<(int, int)>();
            for (int cy = y; cy < y + height; cy++)
                for (int cx = x; cx < x + width; cx++)
                    if (on == DecorOn.Back ? !built[cy * vessel.Width + cx] : wall[cy * vessel.Width + cx] == 1) cells.Add((cx, cy));
            decor.Add(new Decor(picture, x, y, width, height, tiled, cells, on, Tint: tint));
        }
        foreach (var c in vessel.Compartments)
            switch (c.Role)
            {
                case VesselRole.Pool:
                    Add("vessel_pool_tiles", c.Left, c.Floor - TileRows, c.Width, TileRows, tiled: true);
                    Add("vessel_lifebuoy", c.Left + (c.Width - 10) / 2, c.Top + 2, 2, 2, tiled: false);
                    break;
                case VesselRole.Restaurant:
                    Add("vessel_wainscot", c.Left, c.Floor - WainscotRows, c.Width, WainscotRows, tiled: true);
                    Add("vessel_bottles", c.Left + 2, c.Floor - BottlesUp, 7, 4, tiled: false);
                    break;
                case VesselRole.Hangar:
                    if (vessel.Fittings.FirstOrDefault(f => Vessels.Shuttles.Contains(f.Kind) && c.Holds(f.X, f.Y)) is { } shuttle)
                    {
                        int left = Math.Max(c.Left, shuttle.X - 2), width = Math.Min(Machines.Get(shuttle.Kind).Width + 4, c.Left + c.Width - left);
                        Add("vessel_pad", left, c.Floor - 1, width, 1, tiled: true);
                    }
                    // The bay doors in the bottom armour under it, as the hull is tinted.
                    int from = c.Left + BayDoorInset, to = c.Left + c.Width - BayDoorInset;
                    Add("vessel_bay_door_left", from, c.Floor, BayDoorEnd, armour, tiled: false, DecorOn.Wall);
                    Add("vessel_bay_door", from + BayDoorEnd, c.Floor, to - from - 2 * BayDoorEnd, armour, tiled: true, DecorOn.Wall, DecorTint.Hull);
                    Add("vessel_bay_door_right", to - BayDoorEnd, c.Floor, BayDoorEnd, armour, tiled: false, DecorOn.Wall);
                    break;
            }
        // The owner's emblem on each fin, in the stripe's colour.
        foreach (var f in vessel.Outside.Where(f => f.Kind == Vessels.Fin))
        {
            var fin = Machines.Get(Vessels.Fin);
            decor.Add(new Decor("vessel_emblem", f.X, f.Y, fin.Width, fin.Height, false, [(f.X, f.Y)], DecorOn.Machine, Vessels.Fin, DecorTint.Stripe));
        }
        // Chevrons along a bow visor's edge: the cells over the hull's outermost along its bottom and its slope, in reading order.
        if (VisorOf(vessel) is { } visor)
        {
            var bow = vessel.Sections.First(s => s.Kind == Vessel.SectionKind.Bow);
            bool Wall(int x, int y) => x >= 0 && y >= 0 && x < vessel.Width && y < vessel.Height && wall[y * vessel.Width + x] == 1;
            bool Outer(int x, int y) => Wall(x, y) && !Wall(x, y + 1);
            for (int y = visor.Top + 1; y < vessel.Height; y++)
                for (int x = bow.Left; x < bow.Left + bow.Width; x++)
                    if (Wall(x, y) && !Outer(x, y) && (Outer(x, y + 1) || Outer(x + 1, y + 1) && !Wall(x + 1, y)))
                        Add(Chevron, x, y, 1, 1, tiled: false, DecorOn.Wall);
        }
        // Every cargo container's stencilled number, a digit a cell over its plate, while it stands.
        foreach (var f in vessel.Fittings.Concat(vessel.Outside))
        {
            if (Vessels.FirstLook(f.Kind) is not (Vessels.Container or Vessels.Cargo)) continue;
            int n = ContainerNumber(vessel.Seed, f.X, f.Y, vessel.Width);
            for (int k = 0; k < 2; k++)
                decor.Add(new Decor(Digit(k == 0 ? n / 10 : n % 10), f.X + NumberAt.X + k, f.Y + NumberAt.Y, 1, 1, false, [(f.X, f.Y)], DecorOn.Machine, f.Kind));
        }
        return decor;
    }

    /// <summary>A bow visor's chevrons, a cell along its edge.</summary>
    public const string Chevron = "vessel_visor_chevron";

    /// <summary>A stencilled digit, a cell, for a container's number.</summary>
    public static string Digit(int digit) => $"vessel_digit_{digit}";

    /// <summary>The cell of a container's number's first digit, from its top-left: over the bare plate its picture leaves.</summary>
    public static readonly (int X, int Y) NumberAt = (2, 1);

    /// <summary>The number stencilled on the container whose top-left is (<paramref name="x"/>, <paramref name="y"/>) of a frame <paramref name="width"/> wide: 1 to 99.</summary>
    public static int ContainerNumber(uint seed, int x, int y, int width) => 1 + Looks.Below(seed, Looks.Salt.Container, y * width + x, 99);

    /// <summary>
    /// A bow visor's rows (big-vessels), where a hangar meets the bow: from the floor row over the hangar, its beam, to the bow's bottom;
    /// <c>Lip</c> the hangar's floor row, the ramp's lip. Null without such a hangar.
    /// </summary>
    public static (int Top, int Lip)? VisorOf(Vessel vessel)
    {
        var bow = vessel.Sections.First(s => s.Kind == Vessel.SectionKind.Bow);
        var hangar = vessel.Compartments.FirstOrDefault(c => c.Role == VesselRole.Hangar && c.Left + c.Width == bow.Left);
        return hangar == null ? null : (hangar.Top - 1, hangar.Floor);
    }

    /// <summary>The piece of a bow visor <paramref name="fx"/> columns past the bow's start and <paramref name="fy"/> rows under its beam.</summary>
    public static Visor VisorPiece(int fx, int fy, bool lip) =>
        fy == 0 ? (fx == 0 ? Visor.Corner : fx % HingeEvery == HingeEvery / 2 ? Visor.Hinge : Visor.Beam)
        : fx == 0 ? Visor.Frame : lip ? Visor.Lip : fy % VisorSeamEvery == 0 ? Visor.Seam : Visor.Plate;

    /// <summary>
    /// Whether <paramref name="decor"/> still shows on <paramref name="grid"/>, by what it lies on: every cell it was stamped bare still a hull
    /// back wall with no structure; every cell it was stamped a hull wall still one; its machine still standing with its top-left there.
    /// </summary>
    public bool Shows(IGridQueries grid, Decor decor)
    {
        foreach (var (x, y) in decor.Cells)
        {
            int cell = CellOf(x, y, grid.Width);
            bool stands = decor.On switch
            {
                DecorOn.Back => grid.StructureAt(cell) == Structure.Empty && grid.BackAt(cell)?.Id == Vessels.Hull,
                DecorOn.Wall => Structures.Is(grid.StructureAt(cell), Trait.Terrain) && grid.MaterialLayer[cell] == HullCode,
                _ => grid.MachineAt(cell) is { } m && m.Kind == decor.Machine && m.Cell == cell,
            };
            if (!stands) return false;
        }
        return true;
    }

    /// <summary>
    /// Where the code and the name are painted: each in the top band of a module, its solid hull from the module's top to the body's
    /// top armour's last row; the code on the stern's module, the name on the next that holds it whole, else after the code.
    /// </summary>
    private IReadOnlyList<Label> Lay(Vessel vessel, Vessel.Section body)
    {
        int armour = Vessels.Class(vessel.Class)!.ArmourDepth;
        var modules = vessel.Sections.Where(s => s.Kind == Vessel.SectionKind.Module).OrderBy(s => s.Left).ToList();
        var labels = new List<Label>();
        int row = 0;
        Label? Fit(string text, bool large, Vessel.Section module, int left)
        {
            var (gw, gh) = large ? Looks.LargeGlyph : Looks.SmallGlyph;
            int width = (text.Length * gw + Camera.Cell - 1) / Camera.Cell, top = module.Top, height = body.Top + armour - module.Top;
            // Clear of the ribs at its ends.
            if (left + width > module.Left + module.Width - 2 || height * Camera.Cell < gh) return null;
            return new Label(text, large, left, top, width, height, row, (height * Camera.Cell - gh) / 2);
        }
        void Add(Label label)
        {
            labels.Add(label);
            row += label.Height + 1;
        }
        if (modules.Count == 0) return labels;
        if (Fit(Code, false, modules[0], modules[0].Left + Looks.CodeInset) is { } code) Add(code);
        var name = modules.Skip(1).Select(m => Fit(Name, true, m, m.Left + Looks.NameInset)).FirstOrDefault(l => l != null)
            ?? (labels.Count == 1 ? Fit(Name, true, modules[0], labels[0].Left + labels[0].Width + 2) : null);
        if (name != null) Add(name);
        return labels;
    }

    /// <summary>
    /// What a wall of material <paramref name="code"/> at grid cell (<paramref name="x"/>, <paramref name="y"/>) draws, its sides facing space
    /// <paramref name="space"/> and facing a room <paramref name="room"/> (<see cref="Sides"/>): nothing but for a hull wall the vessel was
    /// stamped with, still hull, or a cockpit window on the bow's slope. A diagonal shows only while the side it faces out of is space.
    /// </summary>
    public Dress Of(int code, int x, int y, int space, int room)
    {
        int fx = x - _corner.X, fy = y - _corner.Y;
        if (fx < 0 || fy < 0 || fx >= _width || fy >= _height) return default;
        var look = _looks[fy * _width + fx];
        bool glass = code == GlassCode;
        if (!glass && code != HullCode) return default;
        if (look.Bow != 0 && look.Glass == glass)
        {
            bool bottom = look.Bow == 2;
            int outward = bottom ? Down : Up;
            if ((space & outward) != 0)
            {
                int covered = outward | Right;
                return new Dress(new Piece(BowName(look.Run, bottom, glass), look.Piece, 0), !glass, InkOf(space & ~covered, room & ~covered), null, null, Cut: true);
            }
        }
        if (glass || look.Kind == Kind.None) return default;
        var wall = new Piece(look.Kind switch { Kind.Rib => RibSheet, Kind.Visor => VisorSheet, _ => HullSheet }, look.Column, look.Row);
        return new Dress(wall, true, InkOf(space, room), look.Stripe >= 0 ? new Piece(StripeSheet, look.Stripe, 0) : null,
            look.PaintX >= 0 ? (look.PaintX, look.PaintY) : null, Cut: false);
    }

    private static string? InkOf(int space, int room) => (space | room) == 0 ? null : InkName(space, room);

    /// <summary>
    /// The sides of <paramref name="cell"/> facing space (no structure and no back wall, or the grid's edge) and those facing a room (no
    /// wall, but a back wall or a structure), of layers <paramref name="width"/> by <paramref name="height"/>.
    /// </summary>
    public static (int Space, int Room) Sides(int cell, int width, int height, ReadOnlySpan<int> structure, ReadOnlySpan<int> back)
    {
        int x = cell % width, y = cell / width, space = 0, room = 0;
        foreach (var (side, dx, dy) in (ReadOnlySpan<(int, int, int)>)[(Up, 0, -1), (Right, 1, 0), (Down, 0, 1), (Left, -1, 0)])
        {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= width || ny >= height) { space |= side; continue; }
            int n = ny * width + nx;
            if (Structures.Is(structure[n], Trait.Terrain)) continue;
            if (structure[n] == Structure.Empty && back[n] == Back.None) space |= side;
            else room |= side;
        }
        return (space, room);
    }

    /// <summary>What <paramref name="cell"/> of <paramref name="grid"/> draws now (<see cref="Of"/>): nothing unless it is a wall.</summary>
    public Dress At(IGridQueries grid, int cell)
    {
        if (!Structures.Is(grid.StructureAt(cell), Trait.Terrain)) return default;
        var (space, room) = Sides(cell, grid.Width, grid.Height, grid.StructureLayer, grid.BackLayer);
        return Of(grid.MaterialLayer[cell], cell % grid.Width, cell / grid.Width, space, room);
    }

    /// <summary>
    /// Whether grid cell (<paramref name="x"/>, <paramref name="y"/>) lies in a doorway the vessel was stamped with open and that is
    /// jammed: drawn <see cref="Looks.JammedOpen"/> open while the grid says it is open; shut, it is drawn as any shut door.
    /// </summary>
    public bool Jammed(int x, int y)
    {
        int fx = x - _corner.X, fy = y - _corner.Y;
        return fx >= 0 && fy >= 0 && fx < _width && fy < _height && _jammed.Contains(fy * _width + fx);
    }

    /// <summary>The grid cell of the vessel's frame's cell (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public int CellOf(int x, int y, int gridWidth) => (y + _corner.Y) * gridWidth + x + _corner.X;
}
