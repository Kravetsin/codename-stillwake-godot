using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The light of work on plans (0075): sparks fanning off the screwdriver's tip with a glint and a flicker of
/// light each burst, a molten spot where it works glowing hotter the longer it does and cooling after, with
/// faint smoke and shimmer over it; the line where a plan turns real a thin line of sky light bleeding into
/// what is built, motes rising off it; a band of light sweeping a finished plan, a glint on its corner. Drawn
/// a pixel of the screen a unit, added over the world, over the plans.
/// </summary>
public partial class BuildGlow : Node2D
{
    /// <summary>A burst of sparks: how many streaks, and the turn either side of the way to the hero, down-biased.</summary>
    private const int BurstSparks = 3;
    private static readonly (double, double) Fan = (-1.15, 0.9), Speed = (55, 150);
    private static readonly (int, int, int) TipGlint = (255, 200, 120), FlickerLight = (255, 170, 80), MoltenLight = (255, 120, 40);
    /// <summary>How much hotter the spot gets each 60 Hz frame of work, how much it cools each without, and how long after a burst it is still worked.</summary>
    private const double HeatPerFrame = 0.006, CoolPerFrame = 0.012, WorkedMs = 250;
    private const double SmokeMs = 1000.0 / 12, EdgeMotesPerSecond = 13;
    /// <summary>The finish: the band's sweep, its width either side, and when after it the corner glints.</summary>
    private const double SweepMs = 22 * 1000.0 / 60, GlintAfterMs = 6 * 1000.0 / 60, Band = 6;
    private const int FinishMotes = 16;
    private static readonly Color Sky = new(120 / 255f, 210 / 255f, 1), Line = new(220 / 255f, 248 / 255f, 1), SweepColour = new(230 / 255f, 248 / 255f, 1);

    /// <summary>A plan being worked or built: where the tip works it, how hot, its cut, its shimmer.</summary>
    private sealed class Worked
    {
        public Vector2 Tip;
        public double Heat, SinceMs = double.MaxValue, SmokeMs;
        public Rect2 Box;
        public PlanCut? Cut;
        public Shimmer? Shimmer;
    }

    private readonly Dictionary<int, Worked> _worked = [];
    private readonly List<(Rect2 Box, bool FromLeft, double AgeMs, bool Glinted)> _finished = [];
    private readonly List<int> _gone = [];
    private readonly Random _random = new();
    private float _zoom = 1;
    private double _edgeMotes;
    private readonly Vector2[] _quad = new Vector2[4], _line = new Vector2[4];
    private readonly Color[] _quadColours = new Color[4], _lineColours = new Color[4];

    public BuildGlow()
    {
        ZIndex = Layers.PlanHands;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
        TextureFilter = TextureFilterEnum.Linear;
    }

    private double Between((double From, double To) r) => r.From + (r.To - r.From) * _random.NextDouble();

    private Worked Of(int plan)
    {
        if (!_worked.TryGetValue(plan, out var w)) _worked[plan] = w = new Worked();
        return w;
    }

    /// <summary>
    /// <paramref name="bursts"/> bursts of sparks off plan <paramref name="plan"/>'s work at <paramref name="tip"/>, toward
    /// <paramref name="heading"/>: falling aboard onto <paramref name="floor"/>, drifting in space.
    /// </summary>
    public void Sparked(int plan, Vector2 tip, double heading, int bursts, bool inSpace, float? floor)
    {
        var w = Of(plan);
        (w.Tip, w.SinceMs) = (tip, 0);
        var look = inSpace ? Luminous.BuildSparkInSpace : Luminous.BuildSpark;
        // Fanned down-biased either way: the turns mirrored for a hero on the right.
        var fan = Math.Cos(heading) < 0 ? Fan : (-Fan.Item2, -Fan.Item1);
        Glims.Shared.Spray(look, tip.X, tip.Y, bursts * BurstSparks, heading, fan, Speed, inSpace ? null : floor);
        Flares.Shared.Glint(tip.X, tip.Y, 4.5, TipGlint, _random.Next(2) * 0.25);
        Flares.Shared.Flash(tip.X, tip.Y, FlickerLight, 0.45 * Fx.Glow, 2, 0.3f, 0.7);
    }

    /// <summary>Where plan <paramref name="plan"/>, over <paramref name="box"/>, turns real now; none before any work.</summary>
    public void Cut(int plan, Rect2 box, PlanCut? cut)
    {
        var w = Of(plan);
        (w.Box, w.Cut) = (box, cut);
    }

    /// <summary>Plan <paramref name="plan"/> gone: built, if <paramref name="finished"/>, swept with light.</summary>
    public void Gone(int plan, bool finished)
    {
        // Its molten spot's light, no longer held, dies away by itself.
        if (!_worked.Remove(plan, out var w)) return;
        w.Shimmer?.QueueFree();
        if (!finished) return;
        bool fromLeft = w.Cut?.FromLeft ?? false;
        _finished.Add((w.Box, fromLeft, 0, false));
        var b = w.Box;
        var middle = b.GetCenter();
        for (int i = 0; i < Math.Round(FinishMotes * Fx.Count); i++)
        {
            // Round its edge, thrown outward.
            double u = _random.NextDouble() * 2 * (b.Size.X + b.Size.Y);
            var at = u < b.Size.X ? new Vector2((float)u, 0)
                : u < b.Size.X + b.Size.Y ? new Vector2(b.Size.X, (float)(u - b.Size.X))
                : u < 2 * b.Size.X + b.Size.Y ? new Vector2((float)(u - b.Size.X - b.Size.Y), b.Size.Y)
                : new Vector2(0, (float)(u - 2 * b.Size.X - b.Size.Y));
            at += b.Position;
            var away = (at - middle).Normalized();
            double s = Between((4, 12));
            Glims.Shared.Add(Luminous.FinishMote, at.X, at.Y, away.X * s, away.Y * s - 3);
        }
    }

    public override void _Process(double delta)
    {
        double ms = delta * 1000, frames = ms / Glim.SampleMs;
        _zoom = Math.Max(0.01f, GetViewport().GetCanvasTransform().X.Length());
        Scale = Vector2.One / _zoom;
        _edgeMotes += EdgeMotesPerSecond * Fx.Count * ms / 1000;
        int motes = (int)_edgeMotes;
        _edgeMotes -= motes;
        int cutting = 0;
        foreach (var w in _worked.Values) if (w.Cut != null && w.SinceMs < WorkedMs) cutting++;
        foreach (var w in _worked.Values)
        {
            w.SinceMs += ms;
            bool working = w.SinceMs < WorkedMs;
            w.Heat = working ? Math.Min(1, w.Heat + HeatPerFrame * frames) : Math.Max(0, w.Heat - CoolPerFrame * frames);
            if (w.Heat > 0.2)
            {
                w.SmokeMs += ms;
                for (; w.SmokeMs >= SmokeMs; w.SmokeMs -= SmokeMs)
                    Glims.Shared.Add(Luminous.WeldSmoke, w.Tip.X + Between((-1, 1)), w.Tip.Y - 1, Between((-2, 2)), -Between((5, 10)), bright: w.Heat);
            }
            if (w.Heat > 0.02 || w.Shimmer != null)
            {
                if (w.Shimmer == null)
                {
                    w.Shimmer = new Shimmer { Rise = 9 };
                    // In the world's own frame, not this node's screen-scaled one.
                    GetParent().AddChild(w.Shimmer);
                }
                w.Shimmer.Box = new Rect2(w.Tip.X - 8, w.Tip.Y - 22, 16, 22);
                w.Shimmer.Amplitude = (float)(0.4 * w.Heat);
                Flares.Shared.Hold(w, w.Tip.X, w.Tip.Y, MoltenLight, 0.5 * w.Heat * Fx.Glow, 2, 0.3f, 0.6);
            }
            // Motes rising off the line where it turns real, shared among the plans being worked.
            if (working && cutting > 0 && w.Cut is { } cut && motes > 0)
            {
                for (int i = 0; i < Math.Max(1, motes / cutting); i++)
                {
                    var b = w.Box;
                    double x = cut.FromLeft ? cut.Line + Between((-0.5, 0.5)) : b.Position.X + _random.NextDouble() * b.Size.X;
                    double y = cut.FromLeft ? b.Position.Y + _random.NextDouble() * b.Size.Y : cut.Line + Between((-0.5, 0.5));
                    Glims.Shared.Add(Luminous.EdgeMote, x, y, Between((-3, 3)), -Between((3, 9)));
                }
            }
        }
        for (int i = _finished.Count - 1; i >= 0; i--)
        {
            var f = _finished[i];
            f.AgeMs += ms;
            if (!f.Glinted && f.AgeMs >= GlintAfterMs)
            {
                f.Glinted = true;
                Flares.Shared.Glint(f.Box.End.X, f.Box.Position.Y, 6, (170, 230, 255));
            }
            if (f.AgeMs >= SweepMs && f.Glinted) _finished.RemoveAt(i);
            else _finished[i] = f;
        }
        QueueRedraw();
    }

    private Vector2 Screen(float x, float y) => new(x * _zoom, y * _zoom);

    public override void _Draw()
    {
        foreach (var w in _worked.Values)
        {
            if (w.Cut is { } cut) Edge(w.Box, cut);
            if (w.Heat > 0.02)
            {
                float h = (float)w.Heat;
                var tip = w.Tip + new Vector2(0.5f, 0);
                Blob(tip, 3 + 4 * h, new Color(1, 150 / 255f, 60 / 255f, 0.75f * h));
                Blob(tip, 1.5f + 1.5f * h, new Color(1, 240 / 255f, 200 / 255f, 0.9f * h));
            }
        }
        foreach (var f in _finished) Sweep(f.Box, f.FromLeft, Math.Min(1, f.AgeMs / SweepMs));
    }

    private void Blob(Vector2 at, float size, Color colour)
    {
        float s = size * _zoom;
        var c = Screen(at.X, at.Y);
        DrawTextureRect(PixelArt.Glow, new Rect2(c.X - s / 2, c.Y - s / 2, s, s), false, colour);
    }

    /// <summary>The line where a plan turns real: thin sky light, its ends faded, a glow bleeding a few pixels into what is built.</summary>
    private void Edge(Rect2 box, PlanCut cut)
    {
        float glow = (float)(0.38 * Fx.Glow), bleed = 5;
        if (cut.FromLeft)
        {
            float x = cut.Line, top = box.Position.Y, bottom = box.End.Y, w = Math.Min(bleed, x - box.Position.X);
            if (x <= box.Position.X || x >= box.End.X) return;
            Quad(new Rect2(x - w, top, w, box.Size.Y), Sky with { A = 0 }, Sky with { A = glow }, across: true);
            Rule(Screen(x, top), Screen(x, bottom));
        }
        else
        {
            float y = cut.Line, left = box.Position.X, right = box.End.X, h = Math.Min(bleed, box.End.Y - y);
            if (y <= box.Position.Y || y >= box.End.Y) return;
            Quad(new Rect2(left, y, box.Size.X, h), Sky with { A = glow }, Sky with { A = 0 }, across: false);
            Rule(Screen(left, y), Screen(right, y));
        }
    }

    /// <summary>A rectangle of the world filled from <paramref name="from"/> to <paramref name="to"/>, left to right or top to bottom.</summary>
    private void Quad(Rect2 r, Color from, Color to, bool across)
    {
        (_quad[0], _quad[1], _quad[2], _quad[3]) = (Screen(r.Position.X, r.Position.Y), Screen(r.End.X, r.Position.Y), Screen(r.End.X, r.End.Y), Screen(r.Position.X, r.End.Y));
        (_quadColours[0], _quadColours[1], _quadColours[2], _quadColours[3]) = across ? (from, to, to, from) : (from, from, to, to);
        DrawPolygon(_quad, _quadColours);
    }

    private void Rule(Vector2 a, Vector2 b)
    {
        for (int i = 0; i < 4; i++) _line[i] = a.Lerp(b, i switch { 0 => 0, 1 => 0.15f, 2 => 0.85f, _ => 1 });
        (_lineColours[0], _lineColours[1], _lineColours[2], _lineColours[3]) = (Line with { A = 0 }, Line with { A = 0.9f }, Line with { A = 0.9f }, Line with { A = 0 });
        DrawPolylineColors(_line, _lineColours, 1.1f, true);
    }

    /// <summary>A band of light across a finished plan, <paramref name="q"/> of the way along the way it was built.</summary>
    private void Sweep(Rect2 box, bool fromLeft, double q)
    {
        float eased = (float)(1 - (1 - q) * (1 - q)), a = (float)(0.55 * (1 - q * 0.5) * Fx.Glow);
        if (fromLeft)
        {
            float at = box.Position.X - (float)Band + (box.Size.X + 2 * (float)Band) * eased;
            Half(new Rect2(at - (float)Band, box.Position.Y, (float)Band, box.Size.Y), box, a, rising: true, across: true);
            Half(new Rect2(at, box.Position.Y, (float)Band, box.Size.Y), box, a, rising: false, across: true);
        }
        else
        {
            float at = box.End.Y + (float)Band - (box.Size.Y + 2 * (float)Band) * eased;
            Half(new Rect2(box.Position.X, at - (float)Band, box.Size.X, (float)Band), box, a, rising: true, across: false);
            Half(new Rect2(box.Position.X, at, box.Size.X, (float)Band), box, a, rising: false, across: false);
        }
    }

    /// <summary>Half the sweep's band, clipped to the plan: fading in toward the middle, or out from it.</summary>
    private void Half(Rect2 half, Rect2 box, float a, bool rising, bool across)
    {
        var clipped = half.Intersection(box);
        if (clipped.Size.X <= 0 || clipped.Size.Y <= 0) return;
        float At(float p) => rising ? a * p : a * (1 - p);
        float from = across ? (clipped.Position.X - half.Position.X) / half.Size.X : (clipped.Position.Y - half.Position.Y) / half.Size.Y;
        float to = across ? (clipped.End.X - half.Position.X) / half.Size.X : (clipped.End.Y - half.Position.Y) / half.Size.Y;
        Quad(clipped, SweepColour with { A = At(from) }, SweepColour with { A = At(to) }, across);
    }
}
