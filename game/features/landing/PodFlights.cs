using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
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
/// </summary>
public partial class PodFlights : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The pod's picture's height in pixels: its bottom, where its flame comes out and its legs stand.</summary>
    private const float Tall = 56;
    private static readonly Texture2D Pod = PixelArt.Lit("machine_dropPod");
    private static readonly Color Flame = new(1f, 0.71f, 0.35f), Core = new(1f, 0.94f, 0.78f), Dust = new(0.75f, 0.63f, 0.47f);

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly CrewModule _crew;
    private readonly Func<double> _progress;
    private readonly Arrivals? _arrivals;
    private readonly Sketch _lit, _flames;
    /// <summary>This frame's pods in motion, kept between frames: falling from home, coming down, lifting off.</summary>
    private readonly List<(Flight Flight, double S)> _falling = [];
    private readonly List<(int Cell, double A)> _coming = [];
    private readonly List<(int Cell, double S)> _lifting = [];

    public PodFlights(IGridQueries grid, LandingModule landing, CrewModule crew, Func<double> progress, Arrivals? arrivals = null)
    {
        (_grid, _landing, _crew, _progress, _arrivals) = (grid, landing, crew, progress, arrivals);
        _lit = new Sketch(Layers.Screens, lit: true) { Paint = PaintLit };
        Lit.Set(_lit);
        _flames = new Sketch(Layers.Screens) { Paint = PaintFlames };
        AddChild(_lit);
        AddChild(_flames);
    }

    public override void _Process(double delta)
    {
        bool were = _falling.Count + _coming.Count + _lifting.Count > 0;
        _falling.Clear();
        _coming.Clear();
        _lifting.Clear();
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
            foreach (var pod in _grid.MachinesWith(Content.MachineTrait.Seats))
                if (_arrivals.SecondsSince(pod.Id) is double a) _coming.Add((pod.Cell, a));
        if (!were && _falling.Count + _coming.Count + _lifting.Count == 0) return;
        _lit.QueueRedraw();
        _flames.QueueRedraw();
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
        foreach (var (f, s) in _falling)
            if (s >= DropFrom) Thrust(g, PodAt(f.Home) + new Vector2(0, (float)Drop(s)), s, 0);
        foreach (var (cell, a) in _coming)
        {
            var at = PodAt(cell);
            double high = Touchdown(a);
            // The retro flame, long while it brakes high, short as it settles, out at the bounce.
            if (a < BounceFrom) Thrust(g, at - new Vector2(0, (float)high), a, (float)(high / FromPx) * 22);
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
