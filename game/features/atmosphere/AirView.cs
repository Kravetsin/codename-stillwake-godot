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
/// right side, every room losing oxygen into space pulsing red, air streaming out through its openings
/// while it holds any (<see cref="AirStream"/>), faint motes gliding in all air (<see cref="AirMotes"/>).
/// With O, the air overlay (<see cref="AirLook"/>): air dithered in the rooms, coming and going softly
/// (<see cref="AirField"/>), red and running out while a room vents, grey dots where a room holds none,
/// a ragged aura of the lifepod's air round it with motes in it; beside the pointer, what the sim says
/// of that room's air (<see cref="Explain"/>).
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
    /// <summary>Room motes show this much of themselves with the overlay off: there, but quiet.</summary>
    private const float QuietMotes = 0.45f;
    private static readonly Color Mote = AsciiArt.Colour(0xcfeaff), PodMote = AsciiArt.Colour(0xc8eeff);
    private static readonly Color Pulse = AsciiArt.Colour(0xe03030), Track = AsciiArt.Colour(0x16212d), Oxygen = AsciiArt.Colour(0x6fd3ff);

    private readonly Ui _ui;
    private readonly Sketch _pulse = new(Layers.Pulse), _gauges = new(Layers.Gauges), _specks = new(Layers.Effects);
    private readonly Sprite2D _look = new() { Centered = false, ZIndex = Layers.Air, Scale = new Vector2(Cell, Cell), Material = new ShaderMaterial { Shader = AirLook.Shader } };
    private AirField? _field;
    private byte[] _texels = [];
    private Image? _image;
    private ImageTexture? _texture;
    private readonly AirMotes _motes = new(Cell);
    private List<(double X, double Y)> _fillingVents = [];
    /// <summary>The lifepod's reach in pixels: left, top, right, bottom; null without a pod.</summary>
    private (double X0, double Y0, double X1, double Y1)? _reach;
    private List<(Rect2 Box, Color Colour)> _losing = [];
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
        foreach (var node in (Node2D[])[_look, _pulse, _gauges, _specks]) AddChild(node);
        _specks.Paint = PaintMotes;
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
        Stream(elapsedMs);
        if ((_since += elapsedMs) >= RefreshMs)
        {
            _since = 0;
            Survey(m);
        }
        Show(m, elapsedMs);
        if (On) Tell(m, pointer);
    }

    /// <summary>The field eased on and written out if it moved, the motes moved on.</summary>
    private void Show(Registry m, double elapsedMs)
    {
        if (_field == null) return;
        if (!_field.Settled)
        {
            _field.Ease(elapsedMs);
            _field.Write(_texels);
            _image!.SetData(_field.Width, _field.Height, false, Image.Format.Rgba8, _texels);
            _texture!.Update(_image);
        }
        ((ShaderMaterial)_look.Material).SetShaderParameter("overlay", On);
        _motes.Step(elapsedMs, _field, m.Atmosphere.AiredCells, _fillingVents, _reach);
        _specks.QueueRedraw();
    }

    private void PaintMotes(Sketch g)
    {
        float quiet = On ? 1 : QuietMotes;
        foreach (var mote in _motes.Live)
        {
            // The pod's motes belong to its aura, drawn with the overlay only.
            if (mote.Kind == MoteKind.Pod && !On) continue;
            var (colour, alpha, size) = mote.Kind == MoteKind.Room
                ? (Mote, (mote.Big ? 0.5f : 0.8f) * quiet, mote.Big ? 2 : 1)
                : (PodMote, 0.6f, 1);
            // Where it is to a fraction of a pixel: rounded to whole ones, a slow mote steps.
            g.DrawRect(new Rect2((float)mote.X, (float)mote.Y, size, size), colour with { A = alpha * (float)Math.Max(0, mote.Alpha) });
        }
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
        _pulse.QueueRedraw();
        Look(m, air, roomOf);

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

    /// <summary>What the field, the motes and the pod's reach are drawn from, as the sim has it now.</summary>
    private void Look(Registry m, IReadOnlyList<RoomAir> air, ReadOnlySpan<int> roomOf)
    {
        int width = m.Grid.Width, height = m.Grid.Height;
        if (_field == null || _field.Width != width || _field.Height != height)
        {
            _field = new AirField(width, height);
            _texels = new byte[width * height * 4];
            _image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, _texels);
            _texture = ImageTexture.CreateFromImage(_image);
            _look.Texture = _texture;
            ((ShaderMaterial)_look.Material).SetShaderParameter("cell", (float)Cell);
        }
        var venting = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var a in air)
            if (a.Status == RoomStatus.Venting && m.Rooms.RoomById(a.Room) is { } room) venting[a.Room] = room.Openings;
        _field.Survey(roomOf, m.Atmosphere.AiredAt, venting);
        _fillingVents = m.Grid.Machines
            .Where(x => x.Kind == "vent" && m.Rooms.RoomAt(x.Cell) is int room && _byRoom.TryGetValue(room, out var a) && a.Status == RoomStatus.Filling)
            .Select(x => ((x.Cell % width + 0.5) * Cell, (x.Cell / width + 0.5) * Cell)).ToList();
        var pod = m.Grid.Machines.FirstOrDefault(x => x.Kind == "lifepod");
        _reach = null;
        var reach = new Vector4();
        if (pod != null)
        {
            var spec = Machines.Get(pod.Kind);
            int left = pod.Cell % width, top = pod.Cell / width, r = Content.Crew.PodReach;
            var box = ((double)(left - r) * Cell, (double)(top - r) * Cell, (double)(left + spec.Width + r) * Cell, (double)(top + spec.Height + r) * Cell);
            _reach = box;
            reach = new Vector4((float)box.Item1, (float)box.Item2, (float)box.Item3, (float)box.Item4);
        }
        ((ShaderMaterial)_look.Material).SetShaderParameter("reach", reach);
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
