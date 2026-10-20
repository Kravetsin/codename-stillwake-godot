using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// A stack sent across with shift flying to the slot it went to, as a picked-up one flies home (0075): its icon glides from
/// the slot it left along a slight arc in a quarter of a second, trailing a thin ribbon of light, and lands in a glint, a
/// pulse round the slot and a brief flash inside it. Several landings set off one after another; at most
/// <see cref="Fx.MostRibbons"/> fly at once. Drawn only, on the HUD, in its pixels; it starts as the move is sent and
/// shown, never waiting for the host.
/// </summary>
public partial class SlotFlights : Node2D
{
    /// <summary>How long a flight takes, ms; how long its landing's pulse lasts, ms; how far that pulse grows past the slot, pixels.</summary>
    private const double FlightMs = 240, PulseMs = 260, Grow = 6;
    /// <summary>A canister breathed from a slot pulses it longer than a landing and grows a little farther: it is the moment's own (0081).</summary>
    private const double BreathMs = 520, BreathGrow = 9;
    /// <summary>How many 60 Hz frames after the one before it each landing of one move sets off.</summary>
    private const double Stagger = 3;
    /// <summary>How high its arc bows, a share of the way, and at most, pixels; how much larger the icon is at the top.</summary>
    private const double Bow = 0.16, MostBow = 26, Lift = 0.1;
    /// <summary>The ribbon's points, one each 60th of a second.</summary>
    private const int Kept = 8;
    private static readonly Color Ribbon = Rgb(150, 215, 255), Landed = Rgb(170, 225, 255);

    private sealed class Flight(Texture2D icon, Vector2 from, Rect2 to, Place place, bool fresh, int nth)
    {
        public Texture2D Icon { get; } = icon;
        public Vector2 From { get; } = from;
        public Rect2 To { get; } = to;
        public Place Place { get; } = place;
        /// <summary>Whether its slot was empty before: its icon there waits until this one lands.</summary>
        public bool Fresh { get; } = fresh;
        public Vector2 At = from;
        /// <summary>How long it has flown, ms; below 0 while it waits its turn.</summary>
        public double Ms = -nth * Stagger * Glim.SampleMs, Since;
        public readonly Vector2[] Trail = new Vector2[Kept];
        public int Points;
    }

    private sealed class Landing(Rect2 slot, double lasts = PulseMs, double grow = Grow)
    {
        public Rect2 Slot { get; } = slot;
        public double Lasts { get; } = lasts;
        public double Grows { get; } = grow;
        public double Ms;
    }

    private readonly List<Flight> _flights = [];
    private readonly List<Landing> _landings = [];
    private readonly Node2D _under = new(), _icons = new(), _over = new();
    private readonly Vector2[] _line = new Vector2[Kept];
    private readonly Color[] _colours = new Color[Kept];
    private readonly Vector2[] _spike = new Vector2[5];
    private readonly Color[] _spikeColours = new Color[5];

    public SlotFlights()
    {
        var add = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _under.Material = add;
        _over.Material = add;
        AddChild(_under);
        AddChild(_icons);
        AddChild(_over);
        _under.Draw += DrawUnder;
        _icons.Draw += () => DrawIcons(_icons, Colors.White);
        _over.Draw += DrawOver;
    }

    /// <summary>Whether anything still flies or lands: the slots' icons waiting for it are hidden while it does.</summary>
    public bool Busy => _flights.Count > 0 || _landings.Count > 0;

    /// <summary>
    /// A stack shown as <paramref name="icon"/> flying from <paramref name="from"/> to the slot at <paramref name="to"/>, the
    /// <paramref name="nth"/> landing of one move; <paramref name="fresh"/> when that slot was empty. None past the cap.
    /// </summary>
    public void Fly(Texture2D icon, Vector2 from, Rect2 to, Place place, bool fresh, int nth = 0)
    {
        if (_flights.Count < Fx.MostRibbons) _flights.Add(new Flight(icon, from, to, place, fresh, nth));
    }

    /// <summary>A landing's pulse round the slot at <paramref name="slot"/> with nothing flying to it: a canister breathed from it (0081).</summary>
    public void Pulse(Rect2 slot) => _landings.Add(new Landing(slot, BreathMs, BreathGrow));

    /// <summary>Whether the slot at <paramref name="place"/> waits for a flight to land in it, empty before: its icon is hidden till then.</summary>
    public bool Awaits(Place place) => _flights.Exists(f => f.Fresh && f.Place == place);

    public override void _Process(double delta)
    {
        if (!Busy) return;
        double ms = delta * 1000;
        for (int i = _flights.Count - 1; i >= 0; i--)
        {
            var f = _flights[i];
            f.Ms += ms;
            if (f.Ms < 0) continue;
            double q = Math.Min(1, f.Ms / FlightMs);
            f.At = Along(f, q);
            if ((f.Since += ms) >= Glim.SampleMs || f.Points == 0)
            {
                f.Since = 0;
                Array.Copy(f.Trail, 0, f.Trail, 1, Kept - 1);
                f.Points = Math.Min(Kept, f.Points + 1);
            }
            f.Trail[0] = f.At;
            if (q < 1) continue;
            _landings.Add(new Landing(f.To));
            _flights.RemoveAt(i);
        }
        foreach (var l in _landings) l.Ms += ms;
        _landings.RemoveAll(l => l.Ms >= l.Lasts);
        _under.QueueRedraw();
        _icons.QueueRedraw();
        _over.QueueRedraw();
    }

    /// <summary>Where a flight is <paramref name="q"/> of its way: eased in and out, bowed up off the straight line.</summary>
    private static Vector2 Along(Flight f, double q)
    {
        double z = q * q * (3 - 2 * q);
        var to = f.To.GetCenter();
        var way = to - f.From;
        // The bow's side: upward, or left on a move straight up or down.
        var side = new Vector2(way.Y, -way.X).Normalized();
        if (side.Y > 0 || (Math.Abs(side.Y) < 0.01f && side.X > 0)) side = -side;
        double bow = Math.Min(MostBow, way.Length() * Bow) * Math.Sin(Math.PI * z);
        return f.From.Lerp(to, (float)z) + side * (float)bow;
    }

    /// <summary>The ribbons and the soft glow round each icon, then each landing's flash inside its slot and its pulse round it.</summary>
    private void DrawUnder()
    {
        foreach (var f in _flights)
        {
            if (f.Ms < 0 || f.Points < 2) continue;
            int n = Math.Min(f.Points, Kept);
            for (int i = 0; i < n; i++)
            {
                double s = (double)i / (n - 1);
                _line[i] = f.Trail[i];
                // White-hot at the head, the ribbon's blue by three tenths along, nothing at its end.
                _colours[i] = s < 0.3 ? Colors.White.Lerp(Ribbon, (float)(s / 0.3)) with { A = (float)(0.85 - s) } : Ribbon with { A = (float)(0.6 * (1 - (s - 0.3) / 0.7)) };
            }
            var line = _line.AsSpan(0, n).ToArray();
            var colours = _colours.AsSpan(0, n).ToArray();
            _under.DrawPolylineColors(line, Faint(colours, 0.3f), 6, true);
            _under.DrawPolylineColors(line, colours, 2, true);
        }
        foreach (var f in _flights)
            if (f.Ms >= 0) Glow(_under, f.At, (float)(f.To.Size.X * 1.6), Ribbon with { A = 0.35f });
        foreach (var l in _landings)
        {
            double q = l.Ms / l.Lasts, fade = 1 - q;
            _under.DrawRect(l.Slot, Landed with { A = (float)(0.28 * fade * fade) });
            float grow = (float)(l.Grows * (1 - fade * fade));
            _under.DrawRect(l.Slot.Grow(grow), Landed with { A = (float)(0.7 * fade) }, false, 2);
        }
    }

    /// <summary>Each flying icon, a little larger at the top of its arc; drawn again added over itself, pale, so it reads as light.</summary>
    private void DrawIcons(Node2D on, Color colour)
    {
        foreach (var f in _flights)
        {
            if (f.Ms < 0) continue;
            double q = Math.Min(1, f.Ms / FlightMs);
            float size = (float)((f.To.Size.X - 2 * Slots.Inset) * (1 + Lift * Math.Sin(Math.PI * q)));
            on.DrawTextureRect(f.Icon, new Rect2(f.At - Vector2.One * size / 2, Vector2.One * size), false, colour);
        }
    }

    /// <summary>The pale copy of each icon, and each landing's glint while it holds: a cross of light over the slot.</summary>
    private void DrawOver()
    {
        DrawIcons(_over, new Color(0.81f, 0.92f, 1, 0.2f));
        foreach (var l in _landings)
        {
            double a = 1 - l.Ms / (l.Lasts * 0.45);
            if (a <= 0) continue;
            var c = l.Slot.GetCenter();
            float r = l.Slot.Size.X * 0.6f;
            Glow(_over, c, r * 1.6f, Landed with { A = (float)(0.45 * a) });
            Spike(c - new Vector2(r, 0), c + new Vector2(r, 0), a, 1.5f);
            Spike(c - new Vector2(0, r * 0.6f), c + new Vector2(0, r * 0.6f), a * 0.85, 1.2f);
        }
    }

    /// <summary>One spike of a glint, clear at its ends and white-hot in the middle.</summary>
    private void Spike(Vector2 a, Vector2 b, double alpha, float width)
    {
        ReadOnlySpan<float> at = [0, 0.4f, 0.5f, 0.6f, 1], strength = [0, 0.45f, 1, 0.45f, 0];
        for (int i = 0; i < 5; i++)
        {
            _spike[i] = a.Lerp(b, at[i]);
            _spikeColours[i] = (i == 2 ? Colors.White : Landed) with { A = (float)(alpha * strength[i]) };
        }
        _over.DrawPolylineColors(_spike, _spikeColours, width, true);
    }

    private static void Glow(Node2D on, Vector2 at, float size, Color colour) =>
        on.DrawTextureRect(PixelArt.Glow, new Rect2(at - Vector2.One * size / 2, Vector2.One * size), false, colour);

    private static Color[] Faint(Color[] colours, float share)
    {
        var faint = new Color[colours.Length];
        for (int i = 0; i < colours.Length; i++) faint[i] = colours[i] with { A = colours[i].A * share };
        return faint;
    }

    private static Color Rgb(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);
}
