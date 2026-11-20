using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What a drone's meeting shows this frame, pixels of the world: <see cref="U"/> seconds in, the engine's clock and its hero's
/// figure's (what the drone's soot moves by), the core where it lay and the side its hero kneels on, the hero's palm, where the core
/// or drone is now and its lens's turn, the drone's own lens colour, and whether the meeting draws the soot left in the pit, the
/// core's picture gone for this player.
/// </summary>
public readonly record struct MeetingFrame(double U, double ClockS, double SwarmS, Vector2 Core, int Side, Vector2 Palm, Vector2 Drone,
    float Angle, Color Lens, bool Pit);

/// <summary>
/// A drone's first meeting with its hero (drone-meeting), drawn as <see cref="Meeting"/> times it: the blood's ring, the core
/// soaking and cracking, its soot rising into the cloud, the lens lighting, the plates snapping home along ember arcs. One per
/// meeting, in the world's frame, unlit; what lies behind the hero on the tethers' layer, what rises over it on the drones', the
/// light added on the glows'. The drone it becomes is the hero's own <see cref="LightDrone"/> from the rise on.
/// </summary>
public partial class MeetingView : Node2D
{
    /// <summary>The pick's widths and radii, its pixels: a crack's glow and line, an arc's glow and line, a landing's flash, a pit ember's halo, the lens's halo.</summary>
    private const float CrackGlow = 1.7f, CrackLine = 0.55f, ArcGlow = 3, ArcLine = 0.8f, Flash = 9, PitEmber = 3, LensHalo = 14 * 0.82f;
    /// <summary>A drop's rim past its radius and its tail's past its width, the pick's pixels.</summary>
    private const float DropRim = 0.35f, TailRim = 0.8f;

    private static readonly Color Dull = Rgb((95, 83, 66));

    private readonly Node2D _away = new(), _under = new(), _underAdd = new(), _over = new(), _overAdd = new();
    private readonly Triangles _tri = new(2048, 3072);
    private readonly Vec[] _points = new Vec[41];
    private readonly Plate[] _homes = new Plate[DroneSwarm.Tune.Plates];
    private readonly MeetingPlate[] _plates = new MeetingPlate[DroneSwarm.Tune.Plates];
    private MeetingFrame _f;

    public MeetingView()
    {
        TopLevel = true;
        foreach (var (node, z, adds) in new (Node2D, int, bool)[]
        {
            (_away, Layers.Tethers, false), (_under, Layers.Tethers, false), (_underAdd, Layers.Tethers, true),
            (_over, Layers.Drones, false), (_overAdd, Layers.Glows, true),
        })
        {
            (node.ZAsRelative, node.ZIndex) = (false, z);
            node.Material = new CanvasItemMaterial
            {
                LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
                BlendMode = adds ? CanvasItemMaterial.BlendModeEnum.Add : CanvasItemMaterial.BlendModeEnum.Mix,
            };
            AddChild(node);
        }
        _away.Draw += DrawAway;
        _under.Draw += DrawUnder;
        _underAdd.Draw += DrawUnderAdd;
        _over.Draw += DrawOver;
        _overAdd.Draw += DrawOverAdd;
    }

    /// <summary>Shows <paramref name="frame"/>.</summary>
    public void Show(MeetingFrame frame)
    {
        _f = frame;
        float aim = frame.Angle, t = (float)frame.SwarmS;
        for (int i = 0; i < _plates.Length; i++)
        {
            _homes[i] = DroneSwarm.PlateIn(SwarmShape.Idle, i, aim, t);
            _plates[i] = Meeting.PlateOf(i, frame.U, frame.ClockS, V(frame.Core), V(frame.Drone), _homes[i]);
        }
        _away.Modulate = new Color(1, 1, 1, (float)Meeting.DebrisIn(frame.U));
        foreach (var node in (ReadOnlySpan<Node2D>)[_away, _under, _underAdd, _over, _overAdd]) node.QueueRedraw();
    }

    private bool Lifted => _f.U >= Meeting.Times.LiftFrom;
    private bool Forms => Meeting.FormsDrone(_f.U);

    /// <summary>The plates still out among the debris.</summary>
    private void DrawAway()
    {
        for (int i = 0; i < _plates.Length; i++)
            if (_plates[i] is { Stage: PlateStage.Away } plate)
                LightDrone.DrawPlate(_away, i, new Plate(0, 0, (float)plate.Turn, true, 0, 1, (float)plate.Flip), W(plate.At));
    }

    /// <summary>Behind the hero: the drops behind the core, the soot left in the pit, the blood soaking the core while it lies.</summary>
    private void DrawUnder()
    {
        _tri.Clear();
        Drops(front: false);
        if (Lifted && _f.Pit)
            for (int j = 0; j < Meeting.HeapCount; j++)
            {
                if (Meeting.MoteRise(j, _f.U) > 0) continue;
                var mote = Meeting.HeapAt(j);
                var at = Box + W(mote.At);
                LightDrone.Mote(_tri, at.X, at.Y, (float)mote.Side, mote.Dark);
                if (mote.Hot) Square(at, (float)mote.Side, new Color(LightDrone.Ember, 0.9f));
            }
        if (!Lifted) Soak(_f.Core);
        _tri.Flush(_under.GetCanvasItem());
    }

    private void DrawUnderAdd()
    {
        _tri.Clear();
        if (!Lifted) Cracks(_f.Core);
        if (Lifted && _f.Pit)
            for (int j = 0; j < Meeting.HeapCount; j++)
                if (Meeting.HeapAt(j) is { Hot: true } mote && Meeting.MoteRise(j, _f.U) <= 0)
                    Halo(Box + W(mote.At), PitEmber, LightDrone.Ember, 0.3f);
        _tri.Flush(_underAdd.GetCanvasItem());
    }

    /// <summary>Over the hero: the core lifted with its soot rising round it and its plates home, the drops before it, the plates in flight.</summary>
    private void DrawOver()
    {
        if (Forms)
        {
            _tri.Clear();
            Motes(front: false);
            _tri.Flush(_over.GetCanvasItem());
            Homes(front: false);
            var (on, _) = Meeting.Lens(_f.U);
            var lit = Rgb(Meeting.Sign).Lerp(_f.Lens, (float)Meeting.LensShift(_f.U));
            LightDrone.DrawCore(_over, _f.Drone, _f.Angle, Dull.Lerp(lit, (float)on));
            _tri.Clear();
            Soak(_f.Drone);
            Motes(front: true);
            _tri.Flush(_over.GetCanvasItem());
            Homes(front: true);
        }
        _tri.Clear();
        Drops(front: true);
        _tri.Flush(_over.GetCanvasItem());
        for (int i = 0; i < _plates.Length; i++)
            if (_plates[i] is { Stage: PlateStage.Flying } plate)
                LightDrone.DrawPlate(_over, i, new Plate(0, 0, (float)plate.Turn, plate.Front, 0, 1, (float)plate.Flip), W(plate.At));
    }

    /// <summary>The light: the cracks on the lifted core, its lens's halo and ember halo, the soot's embers, the red glow as it drinks, the arcs and the landings' flashes.</summary>
    private void DrawOverAdd()
    {
        _tri.Clear();
        double u = _f.U;
        if (Forms)
        {
            Cracks(_f.Drone);
            var (on, glow) = Meeting.Lens(u);
            var lit = Rgb(Meeting.Sign).Lerp(_f.Lens, (float)Meeting.LensShift(u));
            Halo(LightDrone.LensOf(_f.Drone, _f.Angle), LensHalo, lit, (float)(0.55 * glow * on));
            // The core's own glow stays on it for a player who still sees it lying; else it rises with it.
            if (_f.Pit) Halo(_f.Drone, (float)(Meeting.HaloRadius / Meeting.Px), Rgb(Meeting.Ember), (float)Meeting.Halo(u, Meeting.Breath(_f.ClockS)));
            Embers();
        }
        Halo(_f.Core, (float)(Meeting.DrinkingRadius / Meeting.Px), Rgb(Meeting.Drinking), (float)Meeting.DrinkGlow(u));
        for (int i = 0; i < _plates.Length; i++)
        {
            var plate = _plates[i];
            float a = (float)Meeting.ArcAlpha(i, u);
            if (a > 0.003f)
            {
                var end = plate.Stage == PlateStage.Away ? Meeting.DebrisAt(i, _f.ClockS, V(_f.Core)).At : plate.At;
                var (first, second) = Meeting.ArcSeeds(i, u);
                Arc(V(_f.Drone), end, first, a);
                Arc(V(_f.Drone), end, second, 0.6f * a);
            }
            Halo(W(plate.At), Flash, Rgb(Meeting.Ember), (float)Meeting.LandFlash(i, u));
        }
        _tri.Flush(_overAdd.GetCanvasItem());
    }

    /// <summary>Where the lying core's picture's top-left is.</summary>
    private Vector2 Box => _f.Core - W(Meeting.InPicture);

    /// <summary>The ring's drops on one side of the core: a short tail, a darker rim, the drop, its sheen.</summary>
    private void Drops(bool front)
    {
        var palm = V(_f.Palm);
        var core = V(_f.Core);
        Color dark = Rgb(Meeting.BloodDark), blood = Rgb(Meeting.Blood), light = Rgb(Meeting.BloodLight);
        for (int i = 0; i < Meeting.Drops; i++)
        {
            if (Meeting.DropAt(i, _f.U, palm, core, _f.Side) is not { } drop || drop.Front != front) continue;
            float r = (float)drop.Radius, a = (float)drop.Alpha, w = r * 1.3f;
            Vector2 at = W(drop.At), tail = W(drop.Tail);
            Line(tail, at, w + TailRim * (float)Meeting.Px, new Color(dark, 0.9f * 0.8f * a));
            Line(tail, at, w, new Color(blood, 0.8f * a));
            Disc(at, r + DropRim * (float)Meeting.Px, new Color(dark, 0.9f * a));
            Disc(at, r, new Color(blood, a));
            Disc(at + new Vector2(-0.3f * r, -0.35f * r), 0.38f * r, new Color(light, 0.8f * a));
        }
    }

    /// <summary>The blood soaked into the core's crust at <paramref name="at"/>.</summary>
    private void Soak(Vector2 at)
    {
        float a = (float)Meeting.Soak(_f.U);
        if (a > 0.003f) Disc(at, (float)Meeting.CoreRadius, new Color(Rgb(Meeting.Blood), a));
    }

    /// <summary>The cracks open over the core at <paramref name="at"/>: a wide glow, then a pale line.</summary>
    private void Cracks(Vector2 at)
    {
        var ((r, g, b), alpha) = Meeting.CrackLook(_f.U);
        if (alpha <= 0.003) return;
        var colour = Rgb((r, g, b));
        var pale = colour.Lerp(Colors.White, 0.35f);
        Span<Vec> points = stackalloc Vec[Meeting.CrackPoints];
        for (int k = 0; k < Meeting.CrackCount; k++)
        {
            int n = Meeting.CrackAt(k, _f.U, V(at), _f.Angle, _f.Side, points);
            for (int j = 0; j + 1 < n; j++) Line(W(points[j]), W(points[j + 1]), CrackGlow * (float)Meeting.Px, new Color(colour, 0.28f * (float)alpha));
            for (int j = 0; j + 1 < n; j++) Line(W(points[j]), W(points[j + 1]), CrackLine * (float)Meeting.Px, new Color(pale, 0.95f * (float)alpha));
        }
    }

    /// <summary>The soot on one side of the lifted core: rising from the heap to its orbit, or in it once formed.</summary>
    private void Motes(bool front)
    {
        float aim = _f.Angle, t = (float)_f.SwarmS;
        bool formed = Meeting.Form(_f.U) >= 1;
        for (int i = 0; i < Meeting.Motes; i++)
        {
            var home = DroneSwarm.MoteIn(SwarmShape.Idle, i, aim, t);
            if (home.Front != front) continue;
            if (formed)
            {
                LightDrone.Mote(_tri, _f.Drone.X + home.X, _f.Drone.Y + home.Y, home.Side, home.Dark);
                continue;
            }
            if (Meeting.MoteAt(i, _f.U, V(_f.Core), new Vec(_f.Drone.X + home.X, _f.Drone.Y + home.Y), home.Side) is not { } mote) continue;
            var at = W(mote.At);
            var heap = Meeting.HeapAt(i % Meeting.HeapCount);
            bool dark = mote.Heaped ? heap.Dark : home.Dark;
            LightDrone.Mote(_tri, at.X, at.Y, (float)mote.Side, dark, (float)mote.Alpha);
            if (mote.Heaped && heap.Hot) Square(at, (float)mote.Side, new Color(LightDrone.Ember, 0.9f * (float)mote.Alpha));
        }
    }

    /// <summary>The hot motes' heat, once in their orbit, as the drone's own glow adds it.</summary>
    private void Embers()
    {
        float aim = _f.Angle, t = (float)_f.SwarmS;
        for (int i = 0; i < Meeting.Motes; i++)
        {
            var home = DroneSwarm.MoteIn(SwarmShape.Idle, i, aim, t);
            if (home.Heat <= 0.05f || Meeting.MoteRise(i, _f.U) < 0.5) continue;
            Square(new Vector2(_f.Drone.X + home.X, _f.Drone.Y + home.Y), home.Side, new Color(LightDrone.Ember.Lerp(Colors.White, 0.25f * home.Heat), 0.95f * home.Heat));
        }
    }

    /// <summary>The plates home on one side of the drone.</summary>
    private void Homes(bool front)
    {
        for (int i = 0; i < _plates.Length; i++)
            if (_plates[i].Stage == PlateStage.Home && _homes[i].Front == front) LightDrone.DrawPlate(_over, i, _homes[i], _f.Drone);
    }

    /// <summary>A flickering ember arc: a wide glow, then a pale line.</summary>
    private void Arc(Vec from, Vec to, int seed, float alpha)
    {
        int n = Meeting.Arc(from, to, seed, _points);
        var ember = Rgb(Meeting.Ember);
        var pale = ember.Lerp(Colors.White, 0.5f);
        for (int j = 0; j + 1 < n; j++) Line(W(_points[j]), W(_points[j + 1]), ArcGlow * (float)Meeting.Px, new Color(ember, 0.25f * alpha));
        for (int j = 0; j + 1 < n; j++) Line(W(_points[j]), W(_points[j + 1]), ArcLine * (float)Meeting.Px, new Color(pale, 0.9f * alpha));
    }

    private void Line(Vector2 a, Vector2 b, float width, Color colour)
    {
        if (colour.A <= 0.003f || a.IsEqualApprox(b)) return;
        _tri.Quad(a, b, width, colour);
    }

    private void Disc(Vector2 at, float radius, Color colour) => _tri.Fan(at, radius, colour, colour);

    private void Square(Vector2 at, float side, Color colour) => _tri.Rect(at.X - side / 2, at.Y - side / 2, side, side, colour);

    /// <summary>The pick's <c>halo</c>: <paramref name="colour"/> at <paramref name="alpha"/> in the middle, fading out to <paramref name="radius"/> of its pixels.</summary>
    private void Halo(Vector2 at, float radius, Color colour, float alpha)
    {
        if (alpha > 0.005f) _tri.Fan(at, radius * (float)Meeting.Px, new Color(colour, alpha), new Color(colour, 0));
    }

    private static Color Rgb((double R, double G, double B) c) => new((float)(c.R / 255), (float)(c.G / 255), (float)(c.B / 255));

    private static Vector2 W(Vec v) => new((float)v.X, (float)v.Y);

    private static Vec V(Vector2 v) => new(v.X, v.Y);
}
