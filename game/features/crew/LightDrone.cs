using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone (light-drone, drone-swarm), the picked «Пепел в латах»: an iron core floating by the hero's head, its lens turned
/// where it shines, its body a cloud of soot with four scrap plates riding it as armour, in a shape per mode (<see cref="DroneSwarm"/>).
/// The core and the plates are pictures (<c>drone_core</c>, <c>drone_plates</c>, tools/anime-art drone.js); the soot, the lens, its
/// haze, the field's threads, the embers and the glow are drawn here, as the pick drew them. Drawn unlit, in the world's frame; its
/// light is the head lamp's.
/// </summary>
public partial class LightDrone : Node2D
{
    /// <summary>The core's picture's side; how many angles each plate is drawn at, face and back, the side of each, in world px (drone.js's <c>CoreBox</c>, <c>Turns</c>, <c>PlateBox</c>).</summary>
    private const int CoreBox = 4, Turns = 8, PlateBox = 6;

    /// <summary>How far the lens sits ahead of the core's middle, its bezel's and glass's half-axes and its ink, in world px (the pick's, scaled 0.82, a quarter of its pixels).</summary>
    private const float LensAhead = 0.55f, BezelX = 0.636f, BezelY = 0.902f, BezelInk = 0.267f, GlassX = 0.43f, GlassY = 0.677f;

    /// <summary>A glowing plate's halo, its radius and its alpha at the middle; a thread's width and its alpha mid-flight.</summary>
    private const float HaloRadius = 1.75f, HaloAlpha = 0.45f, ThreadWidth = 0.175f, ThreadAlpha = 0.6f;

    /// <summary>The lens's added haze: its radius in world px, its alpha at the glass, its rings (the pick's 14 px, scaled 0.82).</summary>
    private const float LensHaze = 1.9f, LensHazeAlpha = 0.28f;
    private const int LensHazeRings = 10;

    /// <summary>How thin a plate turned edge-on is drawn at least, of its face (the pick's <c>plate</c>).</summary>
    private const float EdgeOn = 0.08f;

    /// <summary>A mote's dark rim past its side, the side its lit top shows from and that top's height, in world px (the pick's <c>dustPass</c>).</summary>
    private const float MoteRim = 0.0875f, LitFrom = 0.275f, LitTop = 0.125f;

    /// <summary>The field's light on a mote, of its glow; an ember's glow, from how hot, its radius in world px and alpha.</summary>
    private const float MoteTint = 0.8f, EmberGlowFrom = 0.55f, EmberGlow = 0.8f, EmberGlowAlpha = 0.45f;

    private static readonly Color Ink = new("16171b"), Bezel = new("2a2d32");
    private static readonly Color RimDark = new("1a1b1f"), RimLight = new("26282d"), SootDark = new("4c4f55"), SootLight = new("606369");
    private static readonly Color SootTop = new(170 / 255f, 174 / 255f, 180 / 255f, 0.45f), Ember = new(1f, 138 / 255f, 58 / 255f);
    /// <summary>Its lens's colour shining and scanning, the mode wheel's too.</summary>
    internal static readonly Color Lamp = new(1f, 0.831f, 0.588f), Scan = new(0.463f, 0.925f, 0.808f);
    private static readonly Color Holo = new(DroneSign.Tint);

    private static Texture2D? _core, _plates;
    private readonly DroneFloat _float = new();
    private readonly DroneSwarm _swarm = new();
    private readonly Plate[] _placed = new Plate[DroneSwarm.Tune.Plates];
    private readonly Mote[] _motes = new Mote[DroneSwarm.Tune.Motes];
    private readonly Node2D _threads = new() { ShowBehindParent = true }, _glow = new();
    private readonly Vector2[] _thread = new Vector2[9];
    // A mote is up to three rects, an ember a rect and a fan of six: what one pass can hold.
    private readonly Triangles _soot = new(DroneSwarm.Tune.Motes * 3 * 4, DroneSwarm.Tune.Motes * 3 * 6);
    private readonly Triangles _sparks = new(DroneSwarm.Tune.Motes * 15, DroneSwarm.Tune.Motes * 30);
    private readonly DroneHolo _sign = new();
    /// <summary>How long it takes to turn most of the way to a new aim, in ms.</summary>
    private const double TurnMs = 90;

    private float _angle;
    private double _timeMs;

    public LightDrone()
    {
        TopLevel = true;
        // Over the hero and what its near hand holds, as the other drones fly.
        (ZAsRelative, ZIndex) = (false, Render.Layers.Drones);
        Visible = false;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        // The field's light adds, as the pick's did: its threads behind everything, the glow and the embers over them.
        foreach (var light in (Node2D[])[_threads, _glow])
        {
            light.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded, BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            AddChild(light);
        }
        _threads.Draw += DrawThreads;
        _glow.Draw += DrawGlow;
        AddChild(_sign);
    }

    /// <summary>
    /// Shows the gesture playing on its hero as a hologram over it (<see cref="DroneSign"/>), the plates fanned toward it, after
    /// <see cref="Follow"/> each frame; none hides it and sends the plates back to the mode's shape, unless it <see cref="Projects"/>.
    /// </summary>
    public void Sign((GestureMotion Motion, double Ms, Vector2 At)? gesture)
    {
        var toward = Vector2.FromAngle(_angle);
        _sign.Show(gesture, toward * LensAhead, toward, _timeMs);
        _swarm.Become(DroneSwarm.ShapeOf(_sign.Visible || Projects, Scanning), _angle, _timeMs);
        for (int i = 0; i < _placed.Length; i++) _placed[i] = _swarm.PlateAt(i, _angle, _timeMs);
        for (int i = 0; i < _motes.Length; i++) _motes[i] = _swarm.MoteAt(i, _angle, _timeMs);
        QueueRedraw();
        _threads.QueueRedraw();
        _glow.QueueRedraw();
    }

    /// <summary>Whether it scans rather than shines: the scanner worn, not the drone.</summary>
    public bool Scanning { get; set; }

    /// <summary>Whether it projects its hero's open bag (drone-companion): in the sign's shape, as for a gesture.</summary>
    public bool Projects { get; set; }

    /// <summary>The box round its lens as drawn, in pixels of the world: where the bag's hologram beams from.</summary>
    public Rect2 LensBox
    {
        get
        {
            var lens = GlobalPosition + Vector2.FromAngle(_angle) * LensAhead;
            const float Half = BezelY + BezelInk / 2;
            return new Rect2(lens.X - Half, lens.Y - Half, 2 * Half, 2 * Half);
        }
    }

    /// <summary>Floats it after the neck at <paramref name="neck"/>, its lens turned <paramref name="toward"/>, a unit vector.</summary>
    public void Follow(Vector2 neck, int facing, Vector2 toward, double elapsedMs, double timeMs)
    {
        var (x, y) = _float.Follow((neck.X, neck.Y), facing, elapsedMs, timeMs);
        GlobalPosition = new Vector2((float)x, (float)y);
        // It turns after the pointer, eased, never snapping across.
        float off = Mathf.Wrap(Mathf.Atan2(toward.Y, toward.X) - _angle, -Mathf.Pi, Mathf.Pi);
        _angle = Mathf.Wrap(_angle + off * (float)(1 - Math.Exp(-elapsedMs / TurnMs)), -Mathf.Pi, Mathf.Pi);
        _timeMs = timeMs;
    }

    /// <summary>The field's colour: the hologram's while the plates fan to a sign, else the lens's.</summary>
    private Color Field => _swarm.Shape == SwarmShape.Sign ? Holo : Scanning ? Scan : Lamp;

    /// <summary>In the pick's order: soot behind, plates behind, the core and its lens, soot before, plates before.</summary>
    public override void _Draw()
    {
        _core ??= HiRes.Texture(HiRes.Load("drone_core") ?? throw new InvalidOperationException("No picture drone_core"));
        _plates ??= HiRes.Texture(HiRes.Load("drone_plates") ?? throw new InvalidOperationException("No picture drone_plates"));
        float angle = _angle;
        var toward = Vector2.FromAngle(angle);
        var colour = Scanning ? Scan : Lamp;
        DrawSoot(front: false);
        for (int i = 0; i < _placed.Length; i++)
            if (!_placed[i].Front) DrawPlate(i);
        DrawTextureRect(_core, new Rect2(-CoreBox / 2f, -CoreBox / 2f, CoreBox, CoreBox), false);
        var lens = toward * LensAhead;
        DrawColoredPolygon(Ellipse(lens, BezelX + BezelInk / 2, BezelY + BezelInk / 2, angle), Ink);
        DrawColoredPolygon(Ellipse(lens, BezelX - BezelInk / 2, BezelY - BezelInk / 2, angle), Bezel);
        DrawColoredPolygon(Ellipse(lens + toward * 0.06f, GlassX, GlassY, angle), colour.Lerp(Colors.White, 0.3f));
        DrawColoredPolygon(Ellipse(lens + toward * 0.16f, 0.185f, 0.35f, angle), colour.Lerp(Colors.White, 0.8f));
        DrawSoot(front: true);
        for (int i = 0; i < _placed.Length; i++)
            if (_placed[i].Front) DrawPlate(i);
    }

    /// <summary>The motes on one side of the core in one call: each a dark rim, its tone, a lit top on the larger ones.</summary>
    private void DrawSoot(bool front)
    {
        _soot.Clear();
        foreach (var mote in _motes)
        {
            if (mote.Front != front) continue;
            float s = mote.Side, x = mote.X - s / 2, y = mote.Y - s / 2;
            _soot.Rect(x - MoteRim, y - MoteRim, s + 2 * MoteRim, s + 2 * MoteRim, mote.Dark ? RimDark : RimLight);
            _soot.Rect(x, y, s, s, mote.Dark ? SootDark : SootLight);
            if (s > LitFrom) _soot.Rect(x, y, s, LitTop, SootTop);
        }
        _soot.Flush(GetCanvasItem());
    }

    /// <summary>
    /// Plate <paramref name="i"/> where it is: the nearest of its pictures, face or rusty back, turned the rest of the way and squashed
    /// across its long axis by how far it is turned over (the pick's <c>flipY</c>); its light from above is off by a few degrees at most.
    /// </summary>
    private void DrawPlate(int i)
    {
        var plate = _placed[i];
        float flip = MathF.Abs(plate.Flip) < EdgeOn ? (plate.Flip < 0 ? -EdgeOn : EdgeOn) : plate.Flip;
        int turn = ((int)Math.Round(plate.Turn / Mathf.Tau * Turns) % Turns + Turns) % Turns;
        var squash = new Transform2D(Vector2.Right, new Vector2(0, MathF.Abs(flip)), Vector2.Zero);
        DrawSetTransformMatrix(new Transform2D(plate.Turn, new Vector2(plate.X, plate.Y)) * squash * new Transform2D(-turn * Mathf.Tau / Turns, Vector2.Zero));
        int column = turn + (flip < 0 ? Turns : 0);
        DrawTextureRectRegion(_plates, new Rect2(-PlateBox / 2f, -PlateBox / 2f, PlateBox, PlateBox), new Rect2(column * PlateBox, i * PlateBox, PlateBox, PlateBox));
        DrawSetTransform(Vector2.Zero, 0);
    }

    /// <summary>A faint thread of the field from the core to each plate in flight, brightest halfway, bowed as the pick's.</summary>
    private void DrawThreads()
    {
        var colour = Field.Lerp(Colors.White, 0.3f);
        foreach (var plate in _placed)
        {
            if (plate.Flight is <= 0 or >= 1) continue;
            var end = new Vector2(plate.X, plate.Y);
            var bow = new Vector2(end.X * 0.5f + end.Y * 0.25f, end.Y * 0.5f - end.X * 0.25f);
            for (int k = 0; k < _thread.Length; k++)
            {
                float s = k / (_thread.Length - 1f);
                _thread[k] = 2 * (1 - s) * s * bow + s * s * end;
            }
            _threads.DrawPolyline(_thread, new Color(colour, ThreadAlpha * Mathf.Sin(plate.Flight * Mathf.Pi)), ThreadWidth, true);
        }
    }

    /// <summary>The lens's haze, a glowing plate's halo, the field's light on the soot and the embers' red heat, all added.</summary>
    private void DrawGlow()
    {
        var colour = Field;
        // The lens's haze, added as the pick's halo: ring over ring, the sum fading out from the glass.
        var lens = Vector2.FromAngle(_angle) * LensAhead;
        var lensColour = Scanning ? Scan : Lamp;
        for (int k = LensHazeRings; k >= 1; k--) _glow.DrawCircle(lens, LensHaze * k / LensHazeRings, new Color(lensColour, LensHazeAlpha / LensHazeRings));
        foreach (var plate in _placed)
        {
            if (plate.Glow <= 0.05f) continue;
            for (int k = 3; k >= 1; k--) _glow.DrawCircle(new Vector2(plate.X, plate.Y), HaloRadius * k / 3, new Color(colour, HaloAlpha * plate.Glow / 3));
        }
        var tint = colour.Lerp(Colors.White, 0.35f);
        _sparks.Clear();
        foreach (var mote in _motes)
        {
            float s = mote.Side, x = mote.X - s / 2, y = mote.Y - s / 2;
            if (mote.Glow > 0.04f) _sparks.Rect(x, y, s, s, new Color(tint, MoteTint * Math.Min(1, mote.Glow)));
            float heat = mote.Heat;
            if (heat <= 0.05f) continue;
            _sparks.Rect(x, y, s, s, new Color(Ember.Lerp(Colors.White, 0.25f * heat), 0.95f * heat));
            if (heat > EmberGlowFrom) _sparks.Fan(new Vector2(mote.X, mote.Y), EmberGlow, new Color(Ember, EmberGlowAlpha * heat), new Color(Ember, 0));
        }
        _sparks.Flush(_glow.GetCanvasItem());
    }

    private static Vector2[] Ellipse(Vector2 at, float rx, float ry, float angle)
    {
        var points = new Vector2[12];
        for (int i = 0; i < points.Length; i++)
        {
            float t = Mathf.Tau * i / points.Length;
            points[i] = at + new Vector2(Mathf.Cos(t) * rx, Mathf.Sin(t) * ry).Rotated(angle);
        }
        return points;
    }

    /// <summary>Coloured triangles gathered for one call: hundreds of motes cost one draw, not one each.</summary>
    private sealed class Triangles(int points, int indices)
    {
        private const int FanSides = 6;
        private readonly Vector2[] _points = new Vector2[points];
        private readonly Color[] _colours = new Color[points];
        private readonly int[] _indices = new int[indices];
        private int _pointCount, _indexCount;

        public void Clear() => (_pointCount, _indexCount) = (0, 0);

        public void Rect(float x, float y, float w, float h, Color colour)
        {
            int at = _pointCount;
            Point(new Vector2(x, y), colour);
            Point(new Vector2(x + w, y), colour);
            Point(new Vector2(x + w, y + h), colour);
            Point(new Vector2(x, y + h), colour);
            foreach (int k in (ReadOnlySpan<int>)[0, 1, 2, 0, 2, 3]) _indices[_indexCount++] = at + k;
        }

        /// <summary>A soft disc: <paramref name="middle"/>'s colour at its middle, fading to <paramref name="rim"/>'s at its edge.</summary>
        public void Fan(Vector2 at, float radius, Color middle, Color rim)
        {
            int centre = _pointCount;
            Point(at, middle);
            for (int k = 0; k < FanSides; k++) Point(at + Vector2.FromAngle(Mathf.Tau * k / FanSides) * radius, rim);
            for (int k = 0; k < FanSides; k++)
            {
                _indices[_indexCount++] = centre;
                _indices[_indexCount++] = centre + 1 + k;
                _indices[_indexCount++] = centre + 1 + (k + 1) % FanSides;
            }
        }

        public void Flush(Rid item)
        {
            if (_indexCount == 0) return;
            RenderingServer.CanvasItemAddTriangleArray(item, new ReadOnlySpan<int>(_indices, 0, _indexCount),
                new ReadOnlySpan<Vector2>(_points, 0, _pointCount), new ReadOnlySpan<Color>(_colours, 0, _pointCount),
                ReadOnlySpan<Vector2>.Empty, ReadOnlySpan<int>.Empty, ReadOnlySpan<float>.Empty, default, -1);
        }

        private void Point(Vector2 at, Color colour)
        {
            _points[_pointCount] = at;
            _colours[_pointCount] = colour;
            _pointCount++;
        }
    }
}
