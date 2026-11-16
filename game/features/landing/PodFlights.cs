using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using static Stillwake.Game.Features.Landing.FlightTimeline;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// A drop pod in the world while it moves (the descent's slice 2), seen by everyone on its surface. Aboard, at a launch: the
/// deck's halves sliding apart under it and shut again behind it, the pod falling through the hole on a thruster's kick. On a
/// planet: a pod just come down, lowered from above on its retro flame into the dust it blows sideways, with a little bounce; and
/// one lifting off, rising on its flame out of a cloud of dust. The grid draws the run open and no flying pod, and holds an
/// arriving one back (<see cref="Arrivals"/>): this draws over it. Lit as the ship is; the flames and dust unlit.
/// The crew shuttle's trip as «A — Под днищем» (<see cref="ShuttleTimeline"/>): aboard, its run sliding open, the craft sinking
/// through it, skids folding, and flying off right under the belly; arriving, in from the far left on its retros, skids out, and
/// stopped by the vessel, or home rising through its run drawn open over the grid's shut one. Its riders' panes lit over it.
/// </summary>
public partial class PodFlights : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The pod's picture's height in pixels: its bottom, where its flame comes out and its legs stand.</summary>
    private const float Tall = 56;
    private static readonly Texture2D Pod = PixelArt.Lit("machine_dropPod");
    private static readonly Color Flame = new(1f, 0.71f, 0.35f), Core = new(1f, 0.94f, 0.78f), Dust = new(0.75f, 0.63f, 0.47f);
    /// <summary>The shuttle's kind, and its picture with its skids down, half up and up.</summary>
    private const string Shuttle = "crewShuttle";
    private static readonly Texture2D[] Craft = [PixelArt.Lit($"machine_{Shuttle}"), PixelArt.Lit($"{Shuttle}_fold_1"), PixelArt.Lit($"{Shuttle}_fold_2")];
    private static readonly Texture2D[] Panes = [.. Enumerable.Range(0, 4).Select(i => PixelArt.Picture($"{Shuttle}_seat_{i}"))];
    /// <summary>The shuttle's stern nozzles' mouths and its nose retros', in its picture's pixels, and each flame's size (the pick's NOZ, RETRO; the retros longer, to read at the far frame).</summary>
    private static readonly (Vector2 At, float K)[] Nozzles = [(new(2, 19.25f), 0.8f), (new(11, 42.2f), 0.75f), (new(0.6f, 62), 1)];
    private static readonly (Vector2 At, float K)[] Retros = [(new(183.4f, 67.4f), 0.7f), (new(133, 20.6f), 0.5f)];
    /// <summary>A flame's layers from its rim to its core, each as wide and long as a share of the whole (the pick's tongue).</summary>
    private static readonly (Color Colour, float Wide, float Long)[] Tongue =
        [(new(0.78f, 0.23f, 0.08f, 0.9f), 1, 1), (new(0.94f, 0.42f, 0.13f), 0.72f, 0.82f), (new(1f, 0.69f, 0.4f), 0.46f, 0.6f), (new(1f, 0.96f, 0.78f), 0.22f, 0.34f)];
    private static readonly Vector2[] Jet = new Vector2[5];

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly CrewModule _crew;
    private readonly Func<double> _progress;
    private readonly Arrivals? _arrivals;
    private readonly Sketch _lit, _flames, _jets;
    /// <summary>This frame's pods in motion, kept between frames: falling from home, coming down, lifting off.</summary>
    private readonly List<(Flight Flight, double S)> _falling = [];
    private readonly List<(int Cell, double A)> _coming = [];
    private readonly List<(int Cell, double S)> _lifting = [];
    /// <summary>This frame's shuttles in motion: the cell each stands from, the run under it, its moment and seconds, its panes lit, its run drawn open.</summary>
    private readonly List<(int Cell, int Under, ShuttleTimeline.Moment M, double T, int[] Panes, bool Opened)> _shuttles = [];
    private readonly PodSounds _sounds = new();

    public PodFlights(IGridQueries grid, LandingModule landing, CrewModule crew, Func<double> progress, Arrivals? arrivals = null)
    {
        (_grid, _landing, _crew, _progress, _arrivals) = (grid, landing, crew, progress, arrivals);
        _lit = new Sketch(Layers.Screens, lit: true) { Paint = PaintLit };
        Lit.Set(_lit);
        _flames = new Sketch(Layers.Screens) { Paint = PaintFlames };
        // The shuttle's flames under its hull, coming out of its nozzles' mouths.
        _jets = new Sketch(Layers.Screens) { Paint = PaintJets };
        AddChild(_jets);
        AddChild(_lit);
        AddChild(_flames);
        AddChild(_sounds);
    }

    public override void _Process(double delta)
    {
        bool were = _falling.Count + _coming.Count + _lifting.Count + _shuttles.Count > 0;
        _falling.Clear();
        _coming.Clear();
        _lifting.Clear();
        _shuttles.Clear();
        double progress = _progress();
        // A flight is drawn on the surface it left until it ends: its hero is in this surface's crew till then.
        foreach (var hero in _crew.Heroes)
        {
            if (_landing.FlightOf(hero.Id) is not { } f) continue;
            double s = Seconds(f, progress);
            if (!f.Up && s < FallShownTo) _falling.Add((f, s));
            else if (f.Up && s < LiftS) _lifting.Add((PodCellOf(f.Floor, _grid.Width), s));
        }
        if (_arrivals != null)
            foreach (var pod in _grid.MachinesWith(Content.MachineTrait.Lands).Where(p => Content.Machines.Get(p.Kind).Riders == 1))
                if (_arrivals.SecondsSince(pod.Id) is double a) _coming.Add((pod.Cell, a));
        SeeShuttles(progress);
        Hear(delta);
        if (!were && _falling.Count + _coming.Count + _lifting.Count + _shuttles.Count == 0) return;
        _lit.QueueRedraw();
        _flames.QueueRedraw();
        _jets.QueueRedraw();
    }

    /// <summary>
    /// The shuttles moving on this surface: each leaving it, till it is gone under the belly (off the ship, standing till its riders'
    /// screens cut away); each just come, held by <see cref="Arrivals"/>.
    /// </summary>
    private void SeeShuttles(double progress)
    {
        int tall = Content.Machines.Get(Shuttle).Height * _grid.Width;
        foreach (var v in _landing.Voyages)
        {
            double s = FlightTimeline.Seconds(Ride.Of(v), progress);
            if (v.TicksLeft == 0 || s >= ShuttleTimeline.PassTo) continue;
            _shuttles.Add((v.From, v.From + tall, ShuttleTimeline.Leaving(s, v.Out != null), s, [.. Enumerable.Range(0, Math.Min(v.Riders.Count, Panes.Length))], false));
        }
        if (_arrivals == null) return;
        foreach (var m in _grid.MachinesOf(Shuttle))
            if (_arrivals.ShuttleSince(m.Id) is var (a, aboard))
                _shuttles.Add((m.Cell, m.Cell + tall, ShuttleTimeline.Arriving(a, aboard), a, [.. _landing.RidersOf(m.Id).Select(r => r.Place)], aboard));
    }

    /// <summary>Each moving pod's engine and knocks, placed at its bottom, where its flame comes out.</summary>
    private void Hear(double delta)
    {
        var bottom = new Vector2(16, Tall);
        foreach (var (f, s) in _falling)
            _sounds.Heard(PodSounds.Motion.Falling, f.Peer, PodAt(f.Home) + bottom + new Vector2(0, (float)Drop(s)), s, s >= DropFrom, 0, delta);
        foreach (var (cell, a) in _coming)
        {
            double high = Touchdown(a);
            _sounds.Heard(PodSounds.Motion.Coming, cell, PodAt(cell) + bottom - new Vector2(0, (float)high), a, a < BounceFrom,
                a < BounceFrom ? Blast(high, Flame(high / FromPx * 22)) : 0, delta);
        }
        foreach (var (cell, s) in _lifting)
            _sounds.Heard(PodSounds.Motion.Lifting, cell, PodAt(cell) + bottom - new Vector2(0, (float)Lift(s)), s, true, Blast(Lift(s), Flame(18)), delta);
        // Docked aboard, not drawn: a burst of its engine blasting the deck, cut as it settles.
        if (_arrivals != null)
            foreach (var pod in _grid.MachinesWith(Content.MachineTrait.Lands).Where(p => Content.Machines.Get(p.Kind).Riders == 1))
                if (_arrivals.DockedSince(pod.Id) is double d)
                    _sounds.Heard(PodSounds.Motion.Docking, pod.Cell, PodAt(pod.Cell) + bottom, d, d < DockBurstS, d < DockBurstS ? 1 - d / DockBurstS : 0, delta);
        _sounds.Settle(delta);
    }

    /// <summary>Where a pod standing from <paramref name="cell"/> is drawn: its picture rises a cell over its footprint (0099).</summary>
    private Vector2 PodAt(int cell) => new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf(LandingModule.PodSpec.Kind)) * Cell;

    private void PaintLit(Sketch g)
    {
        foreach (var (f, s) in _falling)
        {
            g.DrawTexture(Pod, PodAt(f.Home) + new Vector2(0, (float)Drop(s)));
            if (s < DropTo) Deck(g, f.Home + LandingModule.PodSpec.Height * _grid.Width, Open(s));
        }
        foreach (var (cell, a) in _coming) g.DrawTexture(Pod, PodAt(cell) - new Vector2(0, (float)Touchdown(a)));
        foreach (var (cell, s) in _lifting) g.DrawTexture(Pod, PodAt(cell) - new Vector2(0, (float)Lift(s)));
        foreach (var (cell, under, m, _, _, opened) in _shuttles)
        {
            if (opened && m.Open != null) Opened(g, under);
            g.DrawTexture(Craft[m.Fold], ShuttleAt(cell, m));
            if (m.Open is double open) Deck(g, under, open);
        }
    }

    /// <summary>Where a shuttle standing from <paramref name="cell"/> is drawn at moment <paramref name="m"/>: its picture's corner.</summary>
    private Vector2 ShuttleAt(int cell, ShuttleTimeline.Moment m) =>
        new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf(Shuttle)) * Cell + new Vector2((float)m.X, (float)m.Y);

    /// <summary>The run under <paramref name="under"/> drawn open over the grid's shut one: what a shuttle home rises through.</summary>
    private void Opened(Sketch g, int under)
    {
        var (first, length) = LandingModule.RunOf(_grid, under);
        int width = _grid.Width, x0 = first % width * Cell, y = first / width * Cell;
        for (int piece = 0; piece < length; piece += 2)
        {
            int links = (piece > 0 ? 8 : 0) | (piece + 2 < length ? 2 : 0);
            g.DrawTexture(PixelArt.Lit(ShipTiles.JoinedName("platform_open", links)), new Vector2(x0 + piece * Cell, y));
        }
    }

    /// <summary>The shuttles' flames, drawn under their hulls: the stern's pushing it on, the nose retros' braking it.</summary>
    private void PaintJets(Sketch g)
    {
        foreach (var (cell, _, m, t, _, _) in _shuttles)
        {
            var at = ShuttleAt(cell, m);
            Stern(g, at, t, m.Stern0, m.Stern1, m.Stern2);
            if (m.Retro > 0) foreach (var (nozzle, k) in Retros) Burn(g, at + nozzle, 1, k, m.Retro, t);
        }
    }

    /// <summary>
    /// A shuttle's stern flames, its picture's corner at <paramref name="at"/>, each nozzle's 0 to 1 long, flickering by
    /// <paramref name="t"/>: for the world and its rider's flight screen alike.
    /// </summary>
    public static void Stern(CanvasItem g, Vector2 at, double t, double s0, double s1, double s2)
    {
        for (int i = 0; i < Nozzles.Length; i++)
            if ((i == 0 ? s0 : i == 1 ? s1 : s2) is var strength and > 0) Burn(g, at + Nozzles[i].At, -1, Nozzles[i].K, strength, t + i * 0.37);
    }

    /// <summary>A flame <paramref name="strength"/> long, 0 to 1, out of a nozzle's mouth at <paramref name="mouth"/>, along <paramref name="way"/> (−1 astern, 1 ahead).</summary>
    private static void Burn(CanvasItem g, Vector2 mouth, float way, float k, double strength, double t)
    {
        float flicker = 0.9f + 0.2f * Hash(Math.Floor(t * 30)), wide = 12 * k, along = (float)(38 * strength * k) * flicker * way;
        foreach (var (colour, w, l) in Tongue)
        {
            float half = wide * w / 2, reach = along * l;
            Jet[0] = mouth + new Vector2(0, -half);
            Jet[1] = mouth + new Vector2(reach * 0.55f, -half * 0.75f);
            Jet[2] = mouth + new Vector2(reach, 0);
            Jet[3] = mouth + new Vector2(reach * 0.55f, half * 0.75f);
            Jet[4] = mouth + new Vector2(0, half);
            g.DrawColoredPolygon(Jet, colour);
        }
    }

    /// <summary>
    /// The run under <paramref name="under"/> drawn shut, its halves slid apart by <paramref name="open"/> (0 shut, 1 apart), each
    /// piece cut to the run so nothing slides over the hull beside it.
    /// </summary>
    private void Deck(Sketch g, int under, double open)
    {
        if (open >= 1) return;
        var (first, length) = LandingModule.RunOf(_grid, under);
        int width = _grid.Width, x0 = first % width * Cell, y = first / width * Cell, x1 = x0 + length * Cell;
        float half = length * Cell / 2f, slid = (float)(open * half);
        for (int piece = 0; piece < length; piece += 2)
        {
            int links = (piece > 0 ? 8 : 0) | (piece + 2 < length ? 2 : 0);
            var texture = PixelArt.Lit(ShipTiles.JoinedName("platform", links));
            float at = x0 + piece * Cell, middle = at + Cell;
            at += middle < x0 + half ? -slid : slid;
            float from = Math.Max(at, x0), to = Math.Min(at + 2 * Cell, x1);
            if (to <= from) continue;
            g.DrawTextureRectRegion(texture, new Rect2(from, y, to - from, Cell), new Rect2(from - at, 0, to - from, Cell));
        }
    }

    private void PaintFlames(Sketch g)
    {
        // The shuttles' riders' panes, lit from inside over the craft.
        foreach (var (cell, _, m, _, panes, _) in _shuttles)
            foreach (int pane in panes)
                if (pane < Panes.Length) g.DrawTexture(Panes[pane], ShuttleAt(cell, m));
        foreach (var (f, s) in _falling)
            if (s >= DropFrom) Thrust(g, PodAt(f.Home) + new Vector2(0, (float)Drop(s)), s, 0);
        foreach (var (cell, a) in _coming)
        {
            var at = PodAt(cell);
            double high = Touchdown(a);
            // The retro flame, long while it brakes high, short as it settles, out at the bounce.
            if (a < BounceFrom) Thrust(g, at - new Vector2(0, (float)high), a, (float)(high / FromPx * 22));
            // The dust it blows sideways near the ground, settling once it stands.
            if (high < 18 || a >= BounceFrom) Blow(g, at, a, a < BounceFrom ? 1 : 1 - (a - BounceFrom) / (Release - BounceFrom));
        }
        foreach (var (cell, s) in _lifting)
        {
            var at = PodAt(cell);
            Thrust(g, at - new Vector2(0, (float)Lift(s)), s, 18);
            Blow(g, at, s, 1 - s / RiseS);
        }
    }

    /// <summary>A flame under a pod drawn at <paramref name="pod"/>, <paramref name="extra"/> pixels longer than its kick, flickering.</summary>
    private static void Thrust(Sketch g, Vector2 pod, double t, float extra)
    {
        float flicker = 0.6f + 0.4f * Hash(Math.Floor(t * 30)), len = 4 + flicker * 4 + extra * flicker, bottom = pod.Y + Tall;
        g.DrawRect(new Rect2(pod.X + 12, bottom, 8, len), Flame with { A = 0.85f * flicker });
        g.DrawRect(new Rect2(pod.X + 14, bottom, 4, len * 0.6f), Core with { A = 0.95f });
    }

    /// <summary>Dust thrown out both ways along the ground under a pod standing at <paramref name="pod"/>, <paramref name="strength"/> 0 to 1.</summary>
    private static void Blow(Sketch g, Vector2 pod, double t, double strength)
    {
        if (strength <= 0) return;
        float ground = pod.Y + Tall, middle = pod.X + 16;
        for (int i = 0; i < 30; i++)
        {
            double life = (t * 1.4 + Hash(i + 300)) % 1;
            int side = i % 2 == 1 ? 1 : -1;
            float x = middle + side * (float)(6 + life * 60), y = ground - 2 - (float)(Hash(i) * 8 * life);
            g.DrawRect(new Rect2(x, y, 3, 2), Dust with { A = (float)((1 - life) * 0.6 * strength) });
        }
    }

    /// <summary>The storyboard's seeded number in 0..1: the same flicker or puff for a moment on every peer.</summary>
    private static float Hash(double n)
    {
        double x = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
        return (float)(x - Math.Floor(x));
    }
}
