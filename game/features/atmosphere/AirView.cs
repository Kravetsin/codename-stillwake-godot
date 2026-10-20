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
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// The ship's air (features/atmosphere/index.ts, air-overlay.ts). Without a word: every room losing
/// oxygen into space lit red on the beat (each tank's sight glass is <see cref="TankLooks"/>), air streaming out through its openings
/// while it holds any, in light (<see cref="AirStream"/>, <see cref="VentLight"/>, 0075), faint soft motes gliding in all air
/// (<see cref="AirMotes"/>), swept toward a breach as wind lines, and fresh air breathed out of filling vents.
/// With O, the air overlay (<see cref="AirLook"/>), in building too, under the pipes or cables of a tab: air dithered in the rooms, coming and going softly
/// (<see cref="AirField"/>), red and running out while a room vents, grey dots where a room holds none,
/// a ragged aura of the lifepod's air round it with motes in it; beside the pointer, what the sim says
/// of that room's air (<see cref="Explain"/>). The nearest stream hisses: one, however many vent.
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
    /// <summary>Room motes show this much of themselves with the overlay off: there, but quiet.</summary>
    private const float QuietMotes = 0.45f;
    private static readonly Color Mote = AsciiArt.Colour(0xcfeaff), PodMote = AsciiArt.Colour(0xc8eeff);
    private static readonly Color Pulse = AsciiArt.Colour(0xe03030);

    private readonly Ui _ui;
    private readonly Sketch _pulse = new(Layers.Pulse), _specks = new(Layers.Effects);
    private readonly Sprite2D _look = new() { Centered = false, ZIndex = Layers.Air, Scale = new Vector2(Cell, Cell), Material = new ShaderMaterial { Shader = AirLook.Shader } };
    private AirField? _field;
    private Image? _image;
    private ImageTexture? _texture;
    /// <summary>The cells the texture covers.</summary>
    private Stillwake.Sim.Box _shown;
    /// <summary>The rooms and their version the pulse was laid out for, and the rooms draining then.</summary>
    private (IRoomsQueries? Rooms, int Version) _losingFor;
    private HashSet<int> _losingRooms = [];
    private readonly AirMotes _motes = new(Cell);
    private List<(double X, double Y)> _fillingVents = [];
    /// <summary>The lifepod's reach in pixels: left, top, right, bottom; null without a pod.</summary>
    private (double X0, double Y0, double X1, double Y1)? _reach;
    private List<(Rect2 Box, Color Colour)> _losing = [];
    private Dictionary<int, RoomAir> _byRoom = new();
    private double _since = RefreshMs, _clock;
    private List<AirStream> _streams = [];
    /// <summary>The breaches' jets, condensation, shimmer and frost (0075).</summary>
    private readonly VentLight _vents = new();
    /// <summary>Each venting room's red light: its id, its middle in pixels, how far it reaches in cells.</summary>
    private List<(int Room, Vector2 At, int Reach)> _alarms = [];
    private static readonly object Alarm = new();
    private static readonly (int, int, int) AlarmLight = (255, 50, 44);
    /// <summary>Fresh air owed to each filling vent on the screen, a faint veil now and then.</summary>
    private double _breaths;
    private const double BreathsPerSecond = 1.6;
    private readonly Random _random = new();
    private readonly AudioStreamPlayer2D _hiss = Cues.Player2D(S.Hiss, S.World), _hum = Cues.Player2D(S.VentHum, S.World);
    /// <summary>Vents with oxygen behind them, in pixels: the ship's hum comes from them.</summary>
    private List<Vector2> _blowing = [];

    public AirView(Ui ui)
    {
        _ui = ui;
        foreach (var node in (Node2D[])[_look, _pulse, _specks, _vents, _hiss, _hum]) AddChild(node);
        _specks.TextureFilter = TextureFilterEnum.Linear;
        _specks.Paint = PaintMotes;
        _pulse.Paint = g => { foreach (var (box, colour) in _losing) g.DrawRect(box, colour); };
    }

    /// <summary>Whether the air overlay is drawn: wanted with O, under a build tab's own overlay too, which draws over it.</summary>
    private bool Shown => _ui.AirOn.Get();

    /// <summary>Whether it is the overlay that speaks for the pointer: under a tab with one of its own, that one does.</summary>
    private bool On => _ui.OverlayOn == Id;

    public void Frame(Client client, Registry m, (double X, double Y)? pointer, double elapsedMs)
    {
        _clock += elapsedMs;
        double pulse = 0.5 + 0.5 * Math.Sin(_clock / PulseMs * 2 * Math.PI);
        // The alarm is light (0075): each venting room lit red on the beat, its cells only faintly painted over.
        _pulse.Modulate = new Color(1, 1, 1, (float)(0.06 + 0.12 * pulse));
        foreach (var (room, at, reach) in _alarms)
            Flares.Shared.Hold((Alarm, room), at.X, at.Y, AlarmLight, (0.12 + 0.3 * pulse) * Fx.Glow, reach, 0.75f, 0.6);
        _vents.Step(_streams, elapsedMs);
        Breathe(elapsedMs);
        Nearest(_hiss, _streams.Select(s => new Vector2((float)s.X, (float)s.Y) * Cell));
        Nearest(_hum, _blowing);
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
            if (_field.Write()) Upload(_field);
        }
        ((ShaderMaterial)_look.Material).SetShaderParameter("overlay", Shown);
        _motes.Step(elapsedMs, _field, m.Atmosphere.AiredCells, _fillingVents, _reach);
        Winds();
        _specks.QueueRedraw();
    }

    /// <summary>The field's texels to the texture, made anew, and the sprite moved over the field, when it grew.</summary>
    private void Upload(AirField field)
    {
        var region = field.Region;
        if (_image == null || region != _shown)
        {
            _shown = region;
            _image = Image.CreateFromData(region.Width, region.Height, false, Image.Format.Rgba8, field.Texels);
            _texture = ImageTexture.CreateFromImage(_image);
            _look.Texture = _texture;
            _look.Position = new Vector2(region.Left * Cell, region.Top * Cell);
            ((ShaderMaterial)_look.Material).SetShaderParameter("origin", _look.Position);
            return;
        }
        _image.SetData(region.Width, region.Height, false, Image.Format.Rgba8, field.Texels);
        _texture!.Update(_image);
    }

    /// <summary>
    /// Motes swept fast toward a breach drawn as thin pale wind lines (0075), stretching as they speed up: a trail back along
    /// their way, as long as their speed says.
    /// </summary>
    private void Winds()
    {
        foreach (var mote in _motes.Live)
        {
            if (mote.Kind != MoteKind.Room) continue;
            double speed = Math.Sqrt(mote.Vx * mote.Vx + mote.Vy * mote.Vy);
            if (speed < WindFrom) continue;
            double length = Math.Min(14, speed * 0.075 * Fx.Length) / speed, a = 0.42 * Math.Min(1, (speed - WindFrom) / WindFrom) * Math.Max(0, mote.Alpha);
            Flares.Shared.Trail(mote.X, mote.Y, mote.X - mote.Vx * length, mote.Y - mote.Vy * length, Wind, 0.4, a, 0.22);
        }
    }

    /// <summary>A mote this fast, pixels a second, is drawn as a wind line, full past twice it.</summary>
    private const double WindFrom = 30;
    private static readonly (int, int, int) Wind = (228, 242, 255);

    /// <summary>
    /// Each filling vent on the screen breathes a faint veil of fresh air now and then (0075), drifting up into its room
    /// and settling, as its motes are blown out of it.
    /// </summary>
    private void Breathe(double elapsedMs)
    {
        if (_fillingVents.Count == 0) return;
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        var glims = Glims.Shared;
        for (_breaths += BreathsPerSecond * Fx.Count * elapsedMs / 1000; _breaths >= 1; _breaths--)
            foreach (var (x, y) in _fillingVents)
            {
                if (!view.HasPoint(new Vector2((float)x, (float)y))) continue;
                glims.Add(Luminous.FreshAir, x + glims.Between(-4, 4), y - 2, glims.Between(-6, 6), -glims.Between(8, 18), size: glims.Between(1.2, 2));
            }
    }

    private void PaintMotes(Sketch g)
    {
        float quiet = Shown ? 1 : QuietMotes;
        foreach (var mote in _motes.Live)
        {
            // The pod's motes belong to its aura, drawn with the overlay only.
            if (mote.Kind == MoteKind.Pod && !Shown) continue;
            // A fine soft speck (0075), where it is to a fraction of a pixel, so a slow one glides.
            var (colour, alpha, size) = mote.Kind == MoteKind.Room
                ? (Mote, (mote.Big ? 0.4f : 0.65f) * quiet, mote.Big ? 2.6f : 1.6f)
                : (PodMote, 0.55f, 1.6f);
            g.DrawTextureRect(SoftDots.Dot, new Rect2((float)mote.X - size / 2, (float)mote.Y - size / 2, size, size), false,
                colour with { A = alpha * (float)Math.Max(0, mote.Alpha) });
        }
    }

    /// <summary>A loop at the source nearest the middle of the screen, while there is any; stopped without: the hiss of venting, the hum of vents.</summary>
    private void Nearest(AudioStreamPlayer2D loop, IEnumerable<Vector2> sources)
    {
        var ear = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? Vector2.Zero;
        Vector2? nearest = null;
        float best = float.MaxValue;
        foreach (var at in sources)
        {
            float apart = at.DistanceSquaredTo(ear);
            if (apart < best) (best, nearest) = (apart, at);
        }
        if (nearest is not Vector2 found)
        {
            if (loop.Playing) loop.Stop();
            return;
        }
        loop.GlobalPosition = found;
        if (!loop.Playing) loop.Play();
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
        }
        // Laid out again only when the rooms or those draining changed.
        if (_losingFor != (m.Rooms, m.Rooms.Version) || !losing.SetEquals(_losingRooms))
        {
            (_losingFor, _losingRooms) = ((m.Rooms, m.Rooms.Version), losing);
            _losing = Runs(roomOf, width, m.Grid.Built, (_, room) => losing.Contains(room) ? Pulse : null);
            _alarms = Middles(roomOf, width, m.Grid.Built, losing);
            _pulse.QueueRedraw();
        }
        Look(m, air, roomOf);
    }

    /// <summary>What the field, the motes and the pod's reach are drawn from, as the sim has it now.</summary>
    private void Look(Registry m, IReadOnlyList<RoomAir> air, ReadOnlySpan<int> roomOf)
    {
        int width = m.Grid.Width, height = m.Grid.Height;
        if (_field == null || _field.Width != width || _field.Height != height)
        {
            _field = new AirField(width, height);
            ((ShaderMaterial)_look.Material).SetShaderParameter("cell", (float)Cell);
        }
        var venting = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var a in air)
            if (a.Status == RoomStatus.Venting && m.Rooms.RoomById(a.Room) is { } room) venting[a.Room] = room.Openings;
        _field.Survey(roomOf, m.Atmosphere.AiredCells, venting, m.Grid.Built);
        _fillingVents = m.Grid.Machines
            .Where(x => x.Kind == "vent" && m.Rooms.RoomAt(x.Cell) is int room && _byRoom.TryGetValue(room, out var a) && a.Status == RoomStatus.Filling)
            .Select(x => ((x.Cell % width + 0.5) * Cell, (x.Cell / width + 0.5) * Cell)).ToList();
        _blowing = m.Grid.Machines
            .Where(x => x.Kind == "vent" && m.Rooms.RoomAt(x.Cell) is int room && _byRoom.TryGetValue(room, out var a) && (a.Oxygen > 0 || a.Made > 0))
            .Select(x => new Vector2((x.Cell % width + 0.5f) * Cell, (x.Cell / width + 0.5f) * Cell)).ToList();
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
    /// none: a rectangle per run of one colour along a row. Only <paramref name="built"/> holds rooms.
    /// </summary>
    private static List<(Rect2, Color)> Runs(ReadOnlySpan<int> roomOf, int width, Stillwake.Sim.Box built, Func<int, int, Color?> colourOf)
    {
        var runs = new List<(Rect2, Color)>();
        Color? ColourAt(ReadOnlySpan<int> rooms, int cell) => rooms[cell] >= 0 ? colourOf(cell, rooms[cell]) : null;
        for (int y = built.Top; y <= built.Bottom; y++)
            for (int start = y * width + built.Left, last = y * width + built.Right; start <= last;)
            {
                var here = ColourAt(roomOf, start);
                int end = start + 1;
                while (end <= last && ColourAt(roomOf, end) == here) end++;
                if (here is { } colour) runs.Add((new Rect2(start % width * Cell, start / width * Cell, (end - start) * Cell, Cell), colour));
                start = end;
            }
        return runs;
    }

    /// <summary>
    /// The middle of each of <paramref name="rooms"/>, in pixels, and how far its light reaches to fill it, in cells: about the
    /// side of a square of its size, three to eight. Only <paramref name="built"/> holds rooms.
    /// </summary>
    private static List<(int Room, Vector2 At, int Reach)> Middles(ReadOnlySpan<int> roomOf, int width, Stillwake.Sim.Box built, HashSet<int> rooms)
    {
        var sums = new Dictionary<int, (double X, double Y, int N)>();
        for (int y = built.Top; y <= built.Bottom; y++)
            for (int x = built.Left; x <= built.Right; x++)
            {
                int room = roomOf[y * width + x];
                if (!rooms.Contains(room)) continue;
                var (sx, sy, n) = sums.GetValueOrDefault(room);
                sums[room] = (sx + x + 0.5, sy + y + 0.5, n + 1);
            }
        var middles = new List<(int, Vector2, int)>();
        foreach (int room in rooms.Order())
            if (sums.TryGetValue(room, out var s))
                middles.Add((room, new Vector2((float)(s.X / s.N), (float)(s.Y / s.N)) * Cell, Math.Clamp((int)Math.Ceiling(Math.Sqrt(s.N)), 3, 8)));
        return middles;
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
