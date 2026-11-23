using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Wrecked;

/// <summary>
/// A wrecked vessel as its looks read it (vessel-wrecks W-2): its cells in its frame (space, open, hull, glass), its doors, machines
/// and compartments, read once from its built parts and kept as the grid changes after. Godot-free; the view keeps it current.
/// </summary>
public sealed class WreckScene
{
    public const byte Space = 0, Air = 1, Wall = 2, Glass = 3;

    /// <summary>Cells kept round the frame: a break moves its halves a few cells off it.</summary>
    public const int Pad = 16;

    private readonly byte[] _cells;
    private readonly bool[] _skin;
    private readonly short[] _room;
    private readonly bool[] _ventedRoom;

    /// <summary>A machine of the vessel: its kind, top-left and size in cells, what it holds; whether it still stands.</summary>
    public sealed class Thing
    {
        public required string Kind;
        public int X, Y, W, H;
        public bool Stands = true;
    }

    /// <summary>A doorway: its top-left, rows, and whether it stood open as built.</summary>
    public readonly record struct Doorway(int X, int Y, int Rows, bool Open);

    public Built Built { get; }
    public VesselWreck Wreck { get; }
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<Thing> Machines { get; }
    public IReadOnlyList<Doorway> Doors { get; }

    /// <summary>Each compartment's boxes by its index: a compartment a break cut in two has two.</summary>
    public IReadOnlyList<IReadOnlyList<Vessel.Compartment>> Rooms { get; }

    /// <summary>The compartment a hero arrives in, by index.</summary>
    public int Arrival { get; }

    public WreckScene(Built built)
    {
        Built = built;
        Wreck = built.Wreck ?? throw new ArgumentException("an intact vessel has no wreck's looks", nameof(built));
        var v = built.Vessel;
        (Width, Height) = (v.Width, v.Height);
        int n = (Width + 2 * Pad) * (Height + 2 * Pad);
        _cells = new byte[n];
        _skin = new bool[n];
        _room = new short[n];
        Array.Fill(_room, (short)-1);
        var machines = new List<Thing>();
        var doorTops = new List<(int X, int Y, bool Open)>();
        var doorAt = new HashSet<(int, int)>();
        foreach (var p in built.Parts)
        {
            if (!In(p.X, p.Y)) continue;
            int i = I(p.X, p.Y);
            if (p.Layer == "back") _cells[i] = _cells[i] == Space ? Air : _cells[i];
            else if (p.Kind == "wall") _cells[i] = p.Material == Vessels.Glass ? Glass : Wall;
            else if (p.Layer == "machine")
            {
                var spec = Content.Machines.Get(p.Kind);
                machines.Add(new Thing { Kind = p.Kind, X = p.X, Y = p.Y, W = spec.Width, H = spec.Height });
            }
            else if (p.Kind == "door")
            {
                doorAt.Add((p.X, p.Y));
                doorTops.Add((p.X, p.Y, p.Open));
            }
        }
        Machines = machines;
        Doors = [.. doorTops.Where(d => !doorAt.Contains((d.X, d.Y - 2))).Select(d =>
        {
            int rows = 0;
            while (doorAt.Contains((d.X, d.Y + rows))) rows += 2;
            return new Doorway(d.X, d.Y, rows, d.Open);
        })];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (At(x, y) < Wall) continue;
                for (int dy = -2; dy <= 2 && !_skin[I(x, y)]; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                        if (Math.Abs(dx) + Math.Abs(dy) <= 2 && At(x + dx, y + dy) == Space)
                        {
                            _skin[I(x, y)] = true;
                            break;
                        }
            }
        var rooms = Enumerable.Range(0, v.Compartments.Count(c => c.Of < 0)).Select(_ => new List<Vessel.Compartment>()).ToList();
        for (int c = 0; c < v.Compartments.Count; c++)
        {
            var k = v.Compartments[c];
            int index = k.Of >= 0 ? k.Of : c;
            rooms[index].Add(k);
            for (int y = k.Top; y < k.Floor; y++)
                for (int x = k.Left; x < k.Left + k.Width; x++)
                    if (In(x, y)) _room[I(x, y)] = (short)index;
        }
        Rooms = rooms;
        _ventedRoom = new bool[rooms.Count];
        foreach (int r in Wreck.Vented) _ventedRoom[r] = true;
        var (sx, sy) = Shipyard.Door(built);
        Arrival = RoomAt(sx, sy);
    }

    private int I(int x, int y) => (y + Pad) * (Width + 2 * Pad) + x + Pad;

    public bool In(int x, int y) => x >= -Pad && y >= -Pad && x < Width + Pad && y < Height + Pad;

    /// <summary>The cell at (<paramref name="x"/>, <paramref name="y"/>): space past the room round the frame.</summary>
    public byte At(int x, int y) => In(x, y) ? _cells[I(x, y)] : Space;

    /// <summary>Whether a wall of the hull or its glass stands there.</summary>
    public bool Walled(int x, int y) => At(x, y) >= Wall;

    public bool Open(int x, int y) => At(x, y) == Air;

    /// <summary>Whether the hull stands there within two cells of space, as built: its skin.</summary>
    public bool Skin(int x, int y) => In(x, y) && _skin[I(x, y)] && At(x, y) == Wall;

    /// <summary>The compartment whose open cells hold (<paramref name="x"/>, <paramref name="y"/>), by index; -1 for none.</summary>
    public int RoomAt(int x, int y) => In(x, y) ? _room[I(x, y)] : -1;

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is open in a compartment the air left.</summary>
    public bool Vented(int x, int y) => Open(x, y) && RoomAt(x, y) is int r && r >= 0 && _ventedRoom[r];

    /// <summary>A cell changed on the grid: what stands there now.</summary>
    public void Set(int x, int y, byte cell)
    {
        if (In(x, y)) _cells[I(x, y)] = cell;
    }
}
