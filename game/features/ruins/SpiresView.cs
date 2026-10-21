using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Ruins;

/// <summary>
/// The spires alight round their relics (0095): each spire whose cradle the sim keeps standing (<see cref="BuildingModule.Relics"/>)
/// burns — the shard sparking in its cradle, the lit shaft up its light well, the lens in its tip throwing a pillar of light into the
/// sky, its roots glowing in the rock round its chamber — and a taken one goes dark: watched being taken, its cradle sparks out over
/// a few seconds first; found taken, after a load or by a friend joining, it is dark from the start. A hit on the cradle arcs into the
/// hero that struck. The cradle's picture is the homes' view's, as any piece's; this draws what is round it and the empty cradle left.
/// </summary>
public partial class SpiresView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const double StepSeconds = 1.0 / 24;

    private readonly IGridQueries _grid;
    private readonly BuildingModule _building;
    private readonly Func<IReadOnlyList<Vector2>> _heroes;
    private readonly Painter _pictures, _light, _roots;
    /// <summary>What the roots showed when they were last drawn: they are drawn again only when the grid or a spire's light changes.</summary>
    private long _rootsDrawn = -1;
    private readonly Heard2D _zap;
    private readonly List<Spire> _spires = [];
    private readonly List<LightSource> _lights = [];
    private readonly Random _random = new();
    private readonly Texture2D _empty = PixelArt.Picture($"homes/{Spec.Ruins.Cradle}_0_dark");
    private readonly Texture2D _lens = PixelArt.Picture("ruins/spireLens_lit"), _lensGlow = PixelArt.Picture("ruins/spireLens_glow");
    private int _count = -1;
    private double _seconds, _owed, _spentMs;
    private int _frames;

    /// <summary>The light of the spires near the view this frame: their relics, their shafts, their lenses.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    /// <param name="heroes">Where each hero's body is on this surface, in pixels of the world: a struck cradle arcs into the nearest.</param>
    public SpiresView(IGridQueries grid, BuildingModule building, Func<IReadOnlyList<Vector2>> heroes)
    {
        (_grid, _building, _heroes) = (grid, building, heroes);
        AddChild(_pictures = new Painter(this, Coat.Pictures) { ZIndex = Layers.Flora });
        Lit.Set(_pictures);
        AddChild(_light = new Painter(this, Coat.Light) { ZIndex = Layers.Flora, Material = Additive });
        AddChild(_roots = new Painter(this, Coat.Roots) { ZIndex = Layers.Flora, Material = Additive });
        AddChild(_zap = Cues.Player2D(Game.Sound.Zap, Game.Sound.World, voices: 3));
    }

    /// <summary>The mean time this view took a frame since it was last asked, in milliseconds: for <c>--measure</c>.</summary>
    public double TakeCostMs()
    {
        double ms = _frames > 0 ? _spentMs / _frames : 0;
        (_spentMs, _frames) = (0, 0);
        return ms;
    }

    public override void _Process(double delta)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        _seconds += delta;
        if (_building.Plants.Count != _count) Follow();
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        var near = view.Grow(8 * Cell);
        foreach (var s in _spires) s.InView = near.Intersects(s.Bounds);
        Strikes();
        _owed = Math.Min(_owed + delta, 0.25);
        for (; _owed >= StepSeconds; _owed -= StepSeconds)
            foreach (var s in _spires)
                if (s.InView) Step(s);
        Light(view);
        Hum();
        _pictures.QueueRedraw();
        _light.QueueRedraw();
        long roots = _grid.Version;
        for (int i = 0; i < _spires.Count; i++)
            if (_spires[i].InView) roots = roots * 31 + i * 64 + (int)(Burns(_spires[i]) * 40);
        if (roots != _rootsDrawn)
        {
            _rootsDrawn = roots;
            _roots.QueueRedraw();
        }
        _spentMs += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _frames++;
    }

    /// <summary>The spires by their cradles, found once; each told when its relic is taken, seen now or found so.</summary>
    private void Follow()
    {
        bool first = _count < 0;
        _count = _building.Plants.Count;
        foreach (var relic in _building.Relics)
        {
            if (relic.Kind != Spec.Ruins.Cradle) continue;
            var spire = _spires.FirstOrDefault(s => s.Relic == relic.Cell);
            if (spire == null) _spires.Add(spire = Place(relic));
            if (!relic.Taken || spire.Taken) continue;
            spire.Taken = true;
            // Taken before this peer saw it (a load, a friend joining): dark from the start.
            spire.TakenAt = first || !spire.Seen ? double.NegativeInfinity : _seconds;
            if (spire.TakenAt > double.NegativeInfinity) Zap(spire);
        }
        foreach (var s in _spires) s.Seen = true;
    }

    private Spire Place(Relic relic)
    {
        int w = _grid.Width;
        var (_, fx, fy) = Spec.Ruins.SpireFurnishing.First(p => p.Kind == Spec.Ruins.Cradle);
        int x0 = relic.Cell % w - fx, y0 = relic.Cell / w - fy;
        var kind = Spec.Flora.Get(Spec.Ruins.Cradle);
        var (bx, by) = HomesLooks.BoxAt(Spec.Ruins.Cradle, kind.On, relic.Cell % w, relic.Cell / w, kind.Across, kind.Tall);
        var corner = new Vector2(bx, by);
        int chamberFloor = (y0 + Spec.Ruins.Spire.Count - 2) * Cell;
        var far = new List<Vector2>();
        // The chamber's metal round the cradle: its walls' inner faces, the ledge over its right, the ladder's foot, the cache.
        for (int y = (y0 + 23) * Cell + 2; y < chamberFloor; y += 6)
        {
            far.Add(new Vector2((x0 + 1) * Cell, y));
            far.Add(new Vector2((x0 + 8) * Cell, y));
        }
        for (int x = (x0 + 5) * Cell; x < (x0 + 8) * Cell; x += 8) far.Add(new Vector2(x, (y0 + 23) * Cell));
        int ladder = (x0 + 4) * Cell;
        far.Add(new Vector2(ladder + 3, (y0 + 25) * Cell - 1));
        far.Add(new Vector2(ladder + 13, (y0 + 25) * Cell - 1));
        far.Add(new Vector2(ladder + 3, (y0 + 24) * Cell + 6));
        far.Add(new Vector2((x0 + 1) * Cell + 8, chamberFloor - 12));
        var sparks = new RelicSparks(_random, RelicArt.Cradle, corner, far, chamberFloor, small: false);
        var spire = new Spire(relic.Cell, x0, y0, corner, sparks, Roots(relic.Cell, sparks.Core.X, chamberFloor + 6))
        {
            Bounds = new Rect2(x0 * Cell - 8 * Cell, (y0 - RelicLooks.PillarCells) * Cell, 25 * Cell, (RelicLooks.PillarCells + Spec.Ruins.Spire.Count + 9) * Cell),
        };
        return spire;
    }

    /// <summary>
    /// The relic's roots in the rock (V1's fulgurites): veins of fused glass from where it struck, out and down, a few branching;
    /// each pixel with its cell and how bright it glows. A spire's own, by its cell.
    /// </summary>
    private static List<Root> Roots(int seed, float x0, float y0)
    {
        var random = new Random(seed);
        var roots = new List<Root>();
        for (int n = 0; n < RelicLooks.Roots; n++)
        {
            double a = Math.PI * (0.05 + 0.9 * n / (RelicLooks.Roots - 1)) + (random.NextDouble() - 0.5) * 0.3;
            double x = x0, y = y0;
            int length = random.Next(RelicLooks.RootLength.From, RelicLooks.RootLength.To);
            for (int s = 0; s < length; s++)
            {
                a += (random.NextDouble() - 0.5) * 0.5;
                x += Math.Cos(a);
                y += Math.Sin(a) * 0.9 + 0.15;
                float f = 1 - (float)s / length;
                roots.Add(new Root(new Vector2((float)Math.Floor(x), (float)Math.Floor(y)), f > 0.6 ? RelicLooks.Mid : (117, 82, 224), 0.25f + 0.5f * f));
                if (random.NextDouble() >= 0.05) continue;
                double b = a + 1.2, bx = x, by = y;
                for (int t = 0; t < 10; t++)
                {
                    bx += Math.Cos(b);
                    by += Math.Sin(b);
                    roots.Add(new Root(new Vector2((float)Math.Floor(bx), (float)Math.Floor(by)), (117, 82, 224), 0.25f * f));
                }
            }
        }
        return roots;
    }

    /// <summary>How much a spire still burns: 1 while its relic stands and while it sparks out, then going dark.</summary>
    private double Burns(Spire s)
    {
        if (!s.Taken) return 1;
        double since = _seconds - s.TakenAt;
        return since < RelicLooks.DyingSeconds ? 1 : Math.Max(0, 1 - (since - RelicLooks.DyingSeconds) / RelicLooks.DarkSeconds);
    }

    private double Hum(double seconds) =>
        RelicLooks.HumLow + (1 - RelicLooks.HumLow) * (0.5 + 0.5 * Math.Sin(seconds * Math.Tau / RelicLooks.HumSeconds) * (0.85 + 0.15 * Math.Sin(seconds * 13.1)));

    /// <summary>One page step of a spire in view: its relic's sparks, thinning once taken, its last discharge, its beam's motes.</summary>
    private void Step(Spire s)
    {
        if (s.Taken)
        {
            double since = _seconds - s.TakenAt;
            s.Sparks.Fade = Math.Max(0, 1 - since / RelicLooks.DyingSeconds);
            if (!s.Last && since >= RelicLooks.LastAt && since < RelicLooks.DyingSeconds)
            {
                s.Last = true;
                var core = s.Sparks.Core;
                s.Sparks.Discharge(new Vector2((s.X0 + 4) * Cell + 3, (s.Y0 + 25) * Cell - 1));
                Flash(s, core, 0.8);
            }
        }
        if (s.Sparks.Step(_seconds)) Flash(s, s.Sparks.Core, 1);
        if (Burns(s) > 0 && _random.NextDouble() < RelicLooks.Motes)
        {
            float cx = (s.X0 + 4) * Cell + Cell / 2f;
            s.Motes.Add(new Mote(cx + (float)(_random.NextDouble() - 0.5) * RelicLooks.BeamWide * 0.8f, s.Sparks.Core.Y - (float)_random.NextDouble() * 20,
                -(float)(10 + _random.NextDouble() * 16), (float)(2 + _random.NextDouble() * 3)));
        }
        for (int i = s.Motes.Count - 1; i >= 0; i--)
        {
            var m = s.Motes[i];
            m.Age += (float)StepSeconds;
            m.Y += m.Vy * (float)StepSeconds;
            if (m.Age > m.Life || m.Y < s.Y0 * Cell) s.Motes.RemoveAt(i);
        }
    }

    private void Flash(Spire s, Vector2 at, double share)
    {
        Flares.Shared.Flash(at.X, at.Y, RelicLooks.Hot, RelicLooks.FlashStrength * share, RelicLooks.FlashReach, RelicLooks.LightKeep, RelicLooks.FlashDecay);
        _zap.PlayAt(at, 0.8f + (float)_random.NextDouble() * 0.2f, 2);
    }

    /// <summary>A hit on a standing cradle (its crack deepening) arcs into the nearest hero, as the sim shocks whoever struck.</summary>
    private void Strikes()
    {
        foreach (var crack in _building.Cracks)
        {
            var s = _spires.FirstOrDefault(p => p.Relic == crack.Cell);
            if (s == null || crack.Damage <= s.Damage) continue;
            s.Damage = crack.Damage;
            Zap(s);
        }
        foreach (var s in _spires)
            if (s.Damage > 0 && !_building.Cracks.Any(c => c.Cell == s.Relic)) s.Damage = 0;
    }

    private void Zap(Spire s)
    {
        var heroes = _heroes();
        var core = s.Sparks.Core;
        Vector2? nearest = null;
        foreach (var h in heroes)
            if (h.X >= 0 && h.DistanceTo(core) < (Spec.Hero.Reach / 1000f + 2) * Cell && (nearest == null || h.DistanceTo(core) < nearest.Value.DistanceTo(core))) nearest = h;
        if (nearest is { } hero) s.Sparks.Zap(hero + new Vector2((float)(_random.NextDouble() * 6 - 3), -(float)(_random.NextDouble() * 10)));
        _zap.PlayAt(nearest ?? core, 1 + (float)(_random.NextDouble() * 0.16 - 0.08), 0);
    }

    /// <summary>The light of the spires near the view: each burning one's relic breathing, its flashes, its shaft, its lens.</summary>
    private void Light(Rect2 view)
    {
        _lights.Clear();
        double hum = Hum(_seconds);
        foreach (var s in _spires)
        {
            if (!s.InView) continue;
            float burns = (float)Burns(s);
            if (burns <= 0) continue;
            var core = s.Sparks.Core;
            float relic = s.Taken ? burns * (float)s.Sparks.Fade : burns;
            if (relic > 0) _lights.Add(new LightSource(core.X, core.Y, RelicLooks.Light, RelicLooks.CradleStrength * (float)hum * relic, RelicLooks.CradleReach, RelicLooks.LightKeep, 1, RelicLooks.LightSize));
            float cx = (s.X0 + 4) * Cell + Cell / 2f;
            for (float y = core.Y - 30; y > s.Y0 * Cell + Cell; y -= RelicLooks.ShaftEvery)
                if (view.Grow(RelicLooks.ShaftReach * Cell).HasPoint(new Vector2(cx, y)))
                    _lights.Add(new LightSource(cx, y, RelicLooks.Light, RelicLooks.ShaftStrength * burns * (float)(0.85 + 0.15 * hum), RelicLooks.ShaftReach, RelicLooks.ShaftKeep, 1, RelicLooks.LightSize));
            if (Lens(s) && view.Grow(RelicLooks.LensReach * Cell).HasPoint(new Vector2(cx, s.Y0 * Cell + 8)))
                _lights.Add(new LightSource(cx, s.Y0 * Cell + 8, RelicLooks.Light, RelicLooks.LensStrength * burns, RelicLooks.LensReach, RelicLooks.LightKeep, 1, RelicLooks.LightSize));
        }
    }

    /// <summary>Each burning relic near the view hums (a placeholder), fading with it as a taken one sparks out.</summary>
    private void Hum()
    {
        foreach (var s in _spires)
        {
            double loud = s.InView ? (s.Taken ? s.Sparks.Fade : 1) * Burns(s) : 0;
            if (loud <= 0)
            {
                if (s.Hum is { Playing: true } quiet) quiet.Stop();
                continue;
            }
            if (s.Hum == null) AddChild(s.Hum = Cues.Player2D(Game.Sound.RelicHum, Game.Sound.World));
            s.Hum.Position = s.Sparks.Core;
            s.Hum.OffsetDb = Mathf.LinearToDb((float)loud);
            if (!s.Hum.Playing) s.Hum.Play((float)(_random.NextDouble() * 3));
        }
    }

    /// <summary>Whether a spire's lens still stands in its tip: dug out, it throws no pillar.</summary>
    private bool Lens(Spire s) => _building.PlantAt(s.Y0 * _grid.Width + s.X0 + 4)?.Kind == Spec.Ruins.SpireLamp;

    private void Paint(Painter on, Coat coat)
    {
        foreach (var s in _spires)
        {
            if (!s.InView) continue;
            if (coat == Coat.Pictures)
            {
                // The empty cradle a taken relic leaves, lit as the world is.
                if (s.Taken)
                {
                    var art = HomesArt.Of[Spec.Ruins.Cradle];
                    on.DrawTexture(_empty, s.Corner - new Vector2(art.OriginX, art.OriginY));
                }
                continue;
            }
            float burns = (float)Burns(s);
            if (burns <= 0) continue;
            if (coat == Coat.Roots)
            {
                Roots(on, s, burns);
                continue;
            }
            Beam(on, s, burns);
            if (Lens(s)) Pillar(on, s, burns);
        }
    }

    /// <summary>The roots glowing where the rock still stands round them.</summary>
    private void Roots(Painter on, Spire s, float burns)
    {
        int w = _grid.Width, h = _grid.Height;
        foreach (var r in s.Roots)
        {
            int x = (int)r.At.X / Cell, y = (int)r.At.Y / Cell;
            if (x < 0 || y < 0 || x >= w || y >= h || _grid.StructureAt(y * w + x) != Structure.Wall) continue;
            on.DrawRect(new Rect2(r.At, Vector2.One), Rgb(r.Colour, r.Bright * burns));
        }
    }

    /// <summary>The beam up the light well, from over the relic to the lens: one lit column, its motes rising.</summary>
    private void Beam(Painter on, Spire s, float burns)
    {
        float cx = (s.X0 + 4) * Cell + Cell / 2f, bottom = s.Sparks.Core.Y - 12, top = s.Y0 * Cell + 2;
        float half = RelicLooks.BeamWide / 2f, a = RelicLooks.BeamAlpha * burns * (float)(0.8 + 0.2 * Hum(_seconds));
        float[] at = [-half, -half * 0.3f, 0, half * 0.3f, half];
        float[] alpha = [0, a * 0.7f, a, a * 0.7f, 0];
        for (int i = 0; i < 4; i++)
        {
            var (c0, c1) = (Rgb(i == 1 || i == 2 ? (240, 232, 255) : (170, 135, 255), alpha[i]), Rgb(i == 1 || i == 2 ? (240, 232, 255) : (170, 135, 255), alpha[i + 1]));
            on.DrawPolygon([new(cx + at[i], top), new(cx + at[i + 1], top), new(cx + at[i + 1], bottom), new(cx + at[i], bottom)], [c0, c1, c1, c0]);
        }
        foreach (var m in s.Motes)
            on.DrawRect(new Rect2(new Vector2(Mathf.Round(m.X), Mathf.Round(m.Y)), new Vector2(0.6f, 0.6f)), new Color(230 / 255f, 220 / 255f, 1, 0.8f * Mathf.Sin(Mathf.Pi * m.Age / m.Life) * burns));
    }

    /// <summary>The lens alight and the pillar it throws into the sky, seen from any hill round.</summary>
    private void Pillar(Painter on, Spire s, float burns)
    {
        var lens = new Vector2((s.X0 + 4) * Cell - 4, s.Y0 * Cell - 4);
        on.DrawTexture(_lensGlow, lens, new Color(1, 1, 1, burns));
        on.DrawTexture(_lens, lens, new Color(1, 1, 1, burns));
        float cx = (s.X0 + 4) * Cell + Cell / 2f, foot = s.Y0 * Cell + 1, top = (s.Y0 - RelicLooks.PillarCells) * Cell;
        float flicker = burns * (float)(0.75 + 0.25 * Hum(_seconds) + (_random.NextDouble() - 0.5) * 0.1);
        float mid = foot + (top - foot) * 0.3f, wf = RelicLooks.PillarFoot / 2f, wt = RelicLooks.PillarTop / 2f, wm = wf + (wt - wf) * 0.3f;
        var c0 = Rgb((220, 205, 255), RelicLooks.PillarAlpha * flicker);
        var c1 = Rgb((170, 135, 255), RelicLooks.PillarAlpha * 0.55f * flicker);
        var c2 = Rgb((150, 110, 255), 0.06f * flicker);
        var clear = Rgb((150, 110, 255), 0);
        // Its body fading up, and soft at either side: a core band with a clear rim round it.
        Cone(on, cx, foot, mid, wf * 0.6f, wm * 0.6f, c0, c1, c0, c1);
        Cone(on, cx, mid, top, wm * 0.6f, wt * 0.6f, c1, c2, c1, c2);
        foreach (int side in (int[])[-1, 1])
        {
            Side(on, cx, foot, mid, wf * 0.6f, wm * 0.6f, wf, wm, side, c0, c1, clear);
            Side(on, cx, mid, top, wm * 0.6f, wt * 0.6f, wm, wt, side, c1, c2, clear);
        }
        on.DrawRect(new Rect2(cx - 0.5f, top, 1, foot - top), Rgb((255, 255, 255), RelicLooks.CoreAlpha * flicker * 0.5f));
        on.DrawPolygon([new(cx - 0.5f, foot), new(cx + 0.5f, foot), new(cx + 0.5f, mid), new(cx - 0.5f, mid)],
            [Rgb((255, 255, 255), RelicLooks.CoreAlpha * flicker), Rgb((255, 255, 255), RelicLooks.CoreAlpha * flicker), Rgb((255, 255, 255), 0), Rgb((255, 255, 255), 0)]);
    }

    private static void Cone(Painter on, float cx, float y0, float y1, float w0, float w1, Color a0, Color a1, Color b0, Color b1) =>
        on.DrawPolygon([new(cx - w0, y0), new(cx + w0, y0), new(cx + w1, y1), new(cx - w1, y1)], [a0, b0, b1, a1]);

    private static void Side(Painter on, float cx, float y0, float y1, float in0, float in1, float out0, float out1, int side, Color c0, Color c1, Color clear) =>
        on.DrawPolygon([new(cx + side * in0, y0), new(cx + side * out0, y0), new(cx + side * out1, y1), new(cx + side * in1, y1)], [c0, clear, clear, c1]);

    private static Color Rgb((int R, int G, int B) c, float alpha) => new(c.R / 255f, c.G / 255f, c.B / 255f, alpha);

    private static readonly CanvasItemMaterial Additive = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    private sealed class Spire(int relic, int x0, int y0, Vector2 corner, RelicSparks sparks, List<Root> roots)
    {
        public int Relic { get; } = relic;
        public int X0 { get; } = x0;
        public int Y0 { get; } = y0;
        public Vector2 Corner { get; } = corner;
        public RelicSparks Sparks { get; } = sparks;
        public List<Root> Roots { get; } = roots;
        public List<Mote> Motes { get; } = [];
        public Rect2 Bounds { get; init; }
        public bool Taken, Seen, InView, Last;
        public double TakenAt;
        public int Damage;
        public Heard2D? Hum;
    }

    private readonly record struct Root(Vector2 At, (int R, int G, int B) Colour, float Bright);

    private sealed class Mote(float x, float y, float vy, float life)
    {
        public float X = x, Y = y, Vy = vy, Life = life, Age;
    }

    /// <summary>What a canvas item of the view draws: the empty cradles lit as the world is, the light added each frame, the roots.</summary>
    private enum Coat { Pictures, Light, Roots }

    private sealed partial class Painter(SpiresView view, Coat coat) : Node2D
    {
        public override void _Draw() => view.Paint(this, coat);
    }
}
