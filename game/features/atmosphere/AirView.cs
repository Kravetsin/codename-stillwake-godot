using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// The ship's air (features/atmosphere/index.ts, air-overlay.ts). Without a word: each tank's gauge up its
/// right side, every room losing oxygen into space pulsing red, and air streaming out through its
/// openings while it holds any (<see cref="AirStream"/>). With O, the air overlay: each cell
/// holding air coloured, each room cell without it grey, a room open to space red, a filling room filling
/// in before the eyes; beside the pointer, what the sim says of that room's air (<see cref="Explain"/>).
/// </summary>
public partial class AirView : Node2D
{
    /// <summary>The overlay's id: the feature's.</summary>
    public const string Id = "atmosphere";
    private const int Cell = ShipCamera.Cell;
    /// <summary>The whole ship's air is surveyed at most this often: a big ship has hundreds of rooms.</summary>
    private const double RefreshMs = 250;
    /// <summary>One pulse of a venting room, in milliseconds of real time.</summary>
    private const double PulseMs = 900;
    private const int GaugeWidth = 6;
    private static readonly Color Aired = AsciiArt.Colour(0x3fc7a0), Empty = AsciiArt.Colour(0x6a7488), Venting = AsciiArt.Colour(0xe05050);
    private static readonly Color Pulse = AsciiArt.Colour(0xe03030), Track = AsciiArt.Colour(0x16212d), Oxygen = AsciiArt.Colour(0x6fd3ff);

    private readonly Ui _ui;
    private readonly Sketch _tint = new(20) { Modulate = new Color(1, 1, 1, 0.35f) };
    private readonly Sketch _pulse = new(19), _gauges = new(8);
    private List<(Rect2 Box, Color Colour)> _cells = [], _losing = [];
    private List<(Rect2 Track, Rect2 Level)> _tanks = [];
    private Dictionary<int, RoomAir> _byRoom = new();
    private double _since = RefreshMs, _clock;
    private List<AirStream> _streams = [];
    /// <summary>Particles owed to each stream, carried over between frames.</summary>
    private double[] _owed = [];
    private readonly Random _random = new();

    public AirView(Ui ui)
    {
        _ui = ui;
        foreach (var node in (Node2D[])[_tint, _pulse, _gauges]) AddChild(node);
        _tint.Paint = g => { foreach (var (box, colour) in _cells) g.DrawRect(box, colour); };
        _pulse.Paint = g => { foreach (var (box, colour) in _losing) g.DrawRect(box, colour); };
        _gauges.Paint = g =>
        {
            foreach (var (track, level) in _tanks)
            {
                g.DrawRect(track, Track);
                if (level.Size.Y > 0) g.DrawRect(level, Oxygen);
            }
        };
    }

    private bool On => _ui.OverlayOn.Get() == Id;

    public void Frame(Client client, Registry m, (double X, double Y)? pointer, double elapsedMs)
    {
        _clock += elapsedMs;
        _pulse.Modulate = new Color(1, 1, 1, (float)(0.15 + 0.3 * (0.5 + 0.5 * Math.Sin(_clock / PulseMs * 2 * Math.PI))));
        _tint.Visible = On;
        Stream(elapsedMs);
        if ((_since += elapsedMs) >= RefreshMs)
        {
            _since = 0;
            Survey(m);
        }
        if (On) Tell(m, pointer);
    }

    /// <summary>Air streaming out of every venting room's openings, the particles owed carried over between frames.</summary>
    private void Stream(double elapsedMs)
    {
        for (int i = 0; i < _streams.Count; i++)
        {
            var stream = _streams[i];
            double due = _owed[i] + stream.PerSecond * elapsedMs / 1000;
            int count = (int)Math.Floor(due);
            _owed[i] = due - count;
            if (count == 0) continue;
            // Anywhere across the opening; through a missing back wall, out every way.
            double x = (stream.X + _random.NextDouble() - 0.5) * Cell, y = (stream.Y + _random.NextDouble() - 0.5) * Cell;
            Particles.Shared.Emit(Effect.Air, x, y, count, stream.Heading ?? _random.NextDouble() * 2 * Math.PI, inSpace: true);
        }
    }

    private void Survey(Registry m)
    {
        int width = m.Grid.Width;
        var air = m.Atmosphere.RoomsAir();
        _byRoom = air.ToDictionary(a => a.Room);
        var roomOf = m.Rooms.RoomOfCells;
        var losing = Explain.Draining(air);
        var next = AirStream.Of(air, room => m.Rooms.RoomById(room)?.Openings ?? [], width, m.Grid.Height, cell => m.Rooms.RoomAt(cell) == RoomsModule.Space);
        if (!next.SequenceEqual(_streams))
        {
            _streams = next;
            _owed = new double[next.Count];
        }
        _losing = Runs(roomOf, width, (_, room) => losing.Contains(room) ? Pulse : null);
        if (On)
            _cells = Runs(roomOf, width, (cell, room) =>
                _byRoom.TryGetValue(room, out var a) && a.Status == RoomStatus.Venting ? Venting
                : m.Atmosphere.AiredAt(cell) ? Aired : Empty);
        _pulse.QueueRedraw();
        _tint.QueueRedraw();

        var tank = Machines.Get("tank");
        float full = tank.Height * Cell - 8;
        _tanks = m.Grid.Machines.Where(x => x.Kind == "tank").Select(x =>
        {
            float left = x.Cell % width * Cell + tank.Width * Cell - GaugeWidth - 4, top = x.Cell / width * Cell + 4;
            float level = MathF.Round(full * m.Atmosphere.OxygenIn(x.Id) / Air.TankCapacity);
            return (new Rect2(left, top, GaugeWidth, full), new Rect2(left, top + full - level, GaugeWidth, level));
        }).ToList();
        _gauges.QueueRedraw();
    }

    /// <summary>
    /// Rooms' cells in the colour <paramref name="colourOf"/> gives each cell of a room, none where it gives
    /// none: a rectangle per run of one colour along a row.
    /// </summary>
    private static List<(Rect2, Color)> Runs(ReadOnlySpan<int> roomOf, int width, Func<int, int, Color?> colourOf)
    {
        var runs = new List<(Rect2, Color)>();
        Color? ColourAt(ReadOnlySpan<int> rooms, int cell) => rooms[cell] >= 0 ? colourOf(cell, rooms[cell]) : null;
        for (int start = 0; start < roomOf.Length;)
        {
            var here = ColourAt(roomOf, start);
            int end = start + 1;
            while (end < roomOf.Length && end % width != 0 && ColourAt(roomOf, end) == here) end++;
            if (here is { } colour) runs.Add((new Rect2(start % width * Cell, start / width * Cell, (end - start) * Cell, Cell), colour));
            start = end;
        }
        return runs;
    }

    /// <summary>Beside the pointer, what the sim says of the air of the room under it.</summary>
    private void Tell(Registry m, (double X, double Y)? pointer)
    {
        IReadOnlyList<string>? lines = null;
        if (pointer is { } p && p.X >= 0 && p.Y >= 0 && p.X < m.Grid.Width && p.Y < m.Grid.Height
            && m.Rooms.RoomAt((int)p.Y * m.Grid.Width + (int)p.X) is int room && room != RoomsModule.Space
            && _byRoom.TryGetValue(room, out var air) && m.Rooms.RoomById(room) is { } r)
            // Heroes awake in the room breathe its air: the forecast counts them.
            lines = Explain.Of(air, r.Volume, m.Crew.Heroes.Count(h => h.Out == 0 && m.Rooms.RoomAt(h.Cell) == room));
        _ui.WorldTip.SetLines(lines);
    }
}
