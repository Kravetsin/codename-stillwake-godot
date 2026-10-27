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
/// A drop pod in the world while it flies (the descent's slice 2), seen by everyone on the surface it left: the deck's halves
/// sliding apart under it and shut again behind it, the pod falling through the hole on a thruster's kick. The grid draws the
/// run open from the launch tick and no pod at all: this draws over it, from the flight's ticks, for its first second. Lit as
/// the ship is; the flame unlit, a light of its own.
/// </summary>
public partial class PodFlights : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private static readonly Texture2D Pod = PixelArt.Lit("machine_dropPod");
    private static readonly Color Flame = new(1f, 0.71f, 0.35f), Core = new(1f, 0.94f, 0.78f);

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly CrewModule _crew;
    private readonly Func<double> _progress;
    private readonly Sketch _lit, _flames;
    /// <summary>The flights in their first second this frame, with their seconds; drawn from it, kept between frames.</summary>
    private readonly List<(Flight Flight, double S)> _falling = [];

    public PodFlights(IGridQueries grid, LandingModule landing, CrewModule crew, Func<double> progress)
    {
        (_grid, _landing, _crew, _progress) = (grid, landing, crew, progress);
        _lit = new Sketch(Layers.Screens, lit: true) { Paint = PaintLit };
        Lit.Set(_lit);
        _flames = new Sketch(Layers.Screens) { Paint = PaintFlames };
        AddChild(_lit);
        AddChild(_flames);
    }

    public override void _Process(double delta)
    {
        bool were = _falling.Count > 0;
        _falling.Clear();
        double progress = _progress();
        // A flight is drawn on the surface it left until it ends: its hero is in this surface's crew till then.
        foreach (var hero in _crew.Heroes)
            if (_landing.FlightOf(hero.Id) is { Up: false } f && Seconds(f, progress) is var s && s < DropTo)
                _falling.Add((f, s));
        if (!were && _falling.Count == 0) return;
        _lit.QueueRedraw();
        _flames.QueueRedraw();
    }

    /// <summary>Where a pod standing from <paramref name="cell"/> is drawn: its picture rises a cell over its footprint (0099).</summary>
    private Vector2 PodAt(int cell) => new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf("dropPod")) * Cell;

    private void PaintLit(Sketch g)
    {
        foreach (var (f, s) in _falling)
        {
            g.DrawTexture(Pod, PodAt(f.Home) + new Vector2(0, (float)Drop(s)));
            Deck(g, f.Home + Content.Machines.Get("dropPod").Height * _grid.Width, Open(s));
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
        foreach (var (f, s) in _falling)
        {
            if (s < DropFrom) continue;
            // A short kick as it drops, flickering a few times a second.
            float flicker = 0.6f + 0.4f * Flicker(s * 30), bottom = PodAt(f.Home).Y + 56 + (float)Drop(s), x = PodAt(f.Home).X;
            g.DrawRect(new Rect2(x + 13, bottom, 6, 4 + flicker * 4), Flame with { A = 0.8f * flicker });
            g.DrawRect(new Rect2(x + 15, bottom, 2, 3 + flicker * 2), Core with { A = 0.9f * flicker });
        }
    }

    /// <summary>A flicker from 0 to 1 held for each whole step of <paramref name="t"/>: the same for a moment on every peer.</summary>
    private static float Flicker(double t)
    {
        double x = Math.Sin(Math.Floor(t) * 127.1 + 311.7) * 43758.5453;
        return (float)(x - Math.Floor(x));
    }
}
