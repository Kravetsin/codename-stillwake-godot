using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The drone's soot framing the open drone window (drone-frame, the picks «Угли» and «Сеть по краю»), one per client, over the
/// windows: the motes flying from the drone to the outline the window handed <see cref="Ui.Soot"/> and home, kindling into the
/// nodes of a glowing net along it, the crackle and the faint film (<see cref="SootRun"/>). It tells the rest whether a drone window is out (<see cref="Ui.DroneWindow"/>),
/// so the drone goes bare. Screen px.
/// </summary>
public partial class DroneFrame : CanvasLayer
{
    private static readonly Color Ember = new(SootRun.Tune.Warm), ArcColour = new(SootRun.Tune.Hot), ArcCore = ArcColour.Lerp(Colors.White, SootRun.Tune.ArcPale);
    private static readonly Color Light = new(HoloLook.Drone.Light), LightPale = Light.Lerp(Colors.White, SootRun.Tune.FilmPale);
    private static readonly Color Rim = new("16171b"), SootDark = new("4c4f55"), SootLight = new("606369");

    private readonly Ui _ui;
    private readonly SootRun _run = new(window: true, new Random());
    /// <summary>The film, spans and crackle, added; the motes over them; the hot motes' glow, added over those.</summary>
    private readonly Node2D _light = new(), _motes = new(), _embers = new();
    private readonly Triangles _lit = new(), _soot = new(), _hot = new();
    private readonly (float X, float Y)[] _jag = new (float, float)[SootRun.Tune.ArcSegments + 1];
    /// <summary>The spots round the outline as it was last laid, the outline moved to start at nought, its near corner.</summary>
    private SootSpots? _spots;
    private (float X, float Y)[] _shape = [];
    private int _near = -1;
    private (float X, float Y) _at;
    private bool _open, _wasOpen, _drawn;
    /// <summary>The film's polygon and its colours as last drawn, and the cut of the whole outline, kept while it stands.</summary>
    private readonly List<Vector2> _filmPoints = [];
    private readonly List<Color> _filmColours = [];
    private int[] _wholeCut = [];
    private SootSpots? _cutOf;

    public DroneFrame(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.DroneFrame;
        // The motes under the light, so the frame's line runs whole through them.
        foreach (var (node, added, draw) in (ValueTuple<Node2D, bool, Action>[])[(_motes, false, DrawMotes), (_light, true, DrawLight), (_embers, true, DrawEmbers)])
        {
            if (added) node.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            node.Draw += draw;
            AddChild(node);
        }
    }

    public override void _Process(double delta)
    {
        var soot = _ui.Soot;
        var canvas = GetViewport().GetCanvasTransform();
        (float X, float Y)? from = null;
        if (_ui.DroneLens is var (x, y, w, h))
        {
            var lens = canvas * new Vector2(x + w / 2, y + h / 2);
            from = (lens.X, lens.Y);
        }
        bool open = _open = soot.Open && soot.Outline.Count >= 3;
        if (soot.Outline.Count >= 3) Lay(soot.Outline, from, open);
        _run.Step(delta, open, _spots, _at, from, canvas.X.Length());
        _ui.DroneWindow = (open, _run.Busy ? Math.Max(_run.Out, 1e-3) : 0);
        bool drawing = _run.Shows;
        if (drawing || _drawn)
        {
            _light.QueueRedraw();
            _motes.QueueRedraw();
            _embers.QueueRedraw();
        }
        _drawn = drawing;
    }

    /// <summary>
    /// The spots round <paramref name="outline"/>, laid again only when its shape changes or it opens with another corner nearest
    /// the drone, so its order never shifts mid-flight; it is only moved as it sways.
    /// </summary>
    private void Lay(IReadOnlyList<(float X, float Y)> outline, (float X, float Y)? from, bool open)
    {
        var origin = outline[0];
        bool same = _shape.Length == outline.Count;
        for (int k = 0; same && k < outline.Count; k++)
            same = MathF.Abs(outline[k].X - origin.X - _shape[k].X) < 0.5f && MathF.Abs(outline[k].Y - origin.Y - _shape[k].Y) < 0.5f;
        int near = _near;
        if (!same || open && !_wasOpen) near = from is { } drone ? SootFrame.NearCorner(outline, drone) : 0;
        _wasOpen = open;
        _at = origin;
        if (same && near == _near && _spots != null) return;
        _shape = new (float X, float Y)[outline.Count];
        for (int k = 0; k < outline.Count; k++) _shape[k] = (outline[k].X - origin.X, outline[k].Y - origin.Y);
        _near = near;
        _spots = SootFrame.Spots(_shape, near);
    }

    /// <summary>The film, the net's links and its kindled nodes over them, and the crackle, all added.</summary>
    private void DrawLight()
    {
        _lit.Clear();
        if (_spots != null && _run.Shows)
        {
            Film();
            float alpha = _run.Alpha;
            for (int j = 0; j < _run.Links; j++)
            {
                var (a, b) = _spots.Link(j);
                var (lit, heat) = _run.LinkLook(j);
                NetLook.Line(_lit, At(a), At(b), alpha * lit, heat);
            }
            for (int i = 0; i < _run.Count; i++)
                NetLook.Node(_lit, At(i), _run.NodeRadius(i), alpha * SootRun.Tune.NodeAlpha * _run.Kindled(i), _run.NodeHeat(i));
            if (_run.Crackle is { } crackle && OnLink(crackle.Span, crackle.Seed) is int link)
            {
                var (ia, ib) = _spots.Link(link);
                Vector2 from = At(ia), to = At(ib);
                float a = SootRun.ArcAlpha(crackle.Seed, crackle.Age, _run.Time) * SootRun.Tune.ArcAlpha;
                SootRun.Jag(from.X, from.Y, to.X, to.Y, crackle.Seed, _run.Time, SootRun.Tune.ArcAmp, SootRun.Tune.ArcBow, _jag);
                for (int k = 0; k + 1 < _jag.Length; k++)
                {
                    Vector2 p = new(_jag[k].X, _jag[k].Y), q = new(_jag[k + 1].X, _jag[k + 1].Y);
                    _lit.Quad(p, q, SootRun.Tune.ArcHalo, new Color(ArcColour, SootRun.Tune.ArcHaloAlpha * a));
                    _lit.Quad(p, q, SootRun.Tune.ArcCore, new Color(ArcCore, a));
                }
                foreach (var end in (ReadOnlySpan<Vector2>)[from, to])
                    _lit.Fan(end, SootRun.Tune.ArcEnd, new Color(ArcColour, SootRun.Tune.ArcEndAlpha * a), new Color(ArcColour, 0));
            }
        }
        _lit.Flush(_light.GetCanvasItem());
    }

    private Vector2 At(int i) => new(_run.X(i), _run.Y(i));

    private (int Seed, int Link)? _crackleOn;

    /// <summary>
    /// The link a crackle drawn at <paramref name="from"/> runs on: the first from there bright enough to carry one, held for the
    /// crackle's life; none if no link is.
    /// </summary>
    private int? OnLink(int from, int seed)
    {
        if (_crackleOn is { } on && on.Seed == seed) return on.Link < 0 ? null : on.Link;
        int found = -1;
        for (int k = 0; k < _run.Links && found < 0; k++)
        {
            int j = (from + k) % _run.Links;
            if (_run.LinkLook(j).Alpha > SootRun.Tune.ArcOn) found = j;
        }
        _crackleOn = (seed, found);
        return found < 0 ? null : found;
    }

    /// <summary>
    /// The film between the landed motes (the pick's <c>drawFilm</c>, without its ripple): still, nearly unseen, the hologram's
    /// light slanting down across it, palest at the top left.
    /// </summary>
    private void Film()
    {
        float film = _run.Film * _run.Alpha;
        if (film <= 0 || _spots is not { } spots) return;
        bool whole = true;
        _filmPoints.Clear();
        _filmColours.Clear();
        float ox = _at.X + spots.MinX, oy = _at.Y + spots.MinY, gx = (spots.MaxX - spots.MinX) * SootRun.Tune.FilmSlant, gy = spots.MaxY - spots.MinY;
        float across = gx * gx + gy * gy;
        for (int i = 0; i < _run.Count; i++)
        {
            // Opening, only the landed motes hold it: the outline round them is the part framed so far.
            if (_open && !_run.Landed(i))
            {
                whole = false;
                continue;
            }
            // On the outline itself, not the nodes off it: the film is the window's shape.
            var spot = spots.Spot(i);
            var at = new Vector2(_at.X + spot.X, _at.Y + spot.Y);
            float u = across > 0 ? Math.Clamp(((at.X - ox) * gx + (at.Y - oy) * gy) / across, 0, 1) : 0;
            var colour = u < SootRun.Tune.FilmMid
                ? (LightPale with { A = SootRun.Tune.FilmTop * film }).Lerp(Light with { A = SootRun.Tune.FilmMid * film }, u / SootRun.Tune.FilmMid)
                : (Light with { A = SootRun.Tune.FilmMid * film }).Lerp(Light with { A = film }, (u - SootRun.Tune.FilmMid) / (1 - SootRun.Tune.FilmMid));
            _filmPoints.Add(at);
            _filmColours.Add(colour);
        }
        if (_filmPoints.Count < 3) return;
        int[] cut;
        if (whole)
        {
            // The whole outline's cut stands while it does: moved by the sway, it is cut the same.
            if (!ReferenceEquals(_cutOf, spots) || _wholeCut.Length == 0) (_wholeCut, _cutOf) = (Geometry2D.TriangulatePolygon([.. _filmPoints]), spots);
            cut = _wholeCut;
        }
        else cut = Geometry2D.TriangulatePolygon([.. _filmPoints]);
        if (cut.Length > 0) _lit.Polygon(CollectionsMarshal.AsSpan(_filmPoints), CollectionsMarshal.AsSpan(_filmColours), cut);
    }

    /// <summary>The motes in flight: dark soot, each its tone in a dark rim, fading as it kindles into its node.</summary>
    private void DrawMotes()
    {
        _soot.Clear();
        float alpha = _run.Alpha;
        for (int i = 0; _run.Shows && i < _run.Count; i++)
        {
            float dark = alpha * (1 - _run.Kindled(i));
            if (dark <= 0.02f) continue;
            float side = _run.Size(i), x = _run.X(i) - side / 2, y = _run.Y(i) - side / 2, rim = SootRun.Tune.MoteRim / 2;
            _soot.Rect(x - rim, y - rim, side + 2 * rim, side + 2 * rim, Rim with { A = dark });
            _soot.Rect(x, y, side, side, (_run.Dark(i) ? SootDark : SootLight) with { A = dark });
        }
        _soot.Flush(_motes.GetCanvasItem());
    }

    /// <summary>The hot motes' glow, flickering.</summary>
    private void DrawEmbers()
    {
        _hot.Clear();
        float alpha = _run.Alpha;
        for (int i = 0; _run.Shows && i < _run.Count; i++)
        {
            float heat = _run.Heat(i);
            if (heat <= 0) continue;
            float side = _run.Size(i);
            _hot.Rect(_run.X(i) - side / 2, _run.Y(i) - side / 2, side, side, Ember.Lerp(Colors.White, 0.25f * heat) with { A = 0.9f * heat * alpha });
        }
        _hot.Flush(_embers.GetCanvasItem());
    }
}
