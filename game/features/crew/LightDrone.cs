using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone (light-drone, drone-swarm), the picked «Рой»: an iron core floating by the hero's head, its lens turned where it
/// shines, holding ten scrap shards by its field in a shape per mode (<see cref="DroneSwarm"/>). The core and the shards are pictures
/// (<c>drone_core</c>, <c>drone_shards</c>, tools/anime-art drone.js); the lens, its haze, the field's threads and the shards' glow are
/// drawn here, as the pick drew them. Drawn unlit, in the world's frame; its light is the head lamp's.
/// </summary>
public partial class LightDrone : Node2D
{
    /// <summary>The core's picture's side; how many angles each shard is drawn at, the side of each, in world px (drone.js's <c>CoreBox</c>, <c>Turns</c>, <c>ShardBox</c>).</summary>
    private const int CoreBox = 4, Turns = 8, ShardBox = 4;

    /// <summary>How far the lens sits ahead of the core's middle, its bezel's and glass's half-axes and its ink, in world px (the pick's, scaled 0.82, a quarter of its pixels).</summary>
    private const float LensAhead = 0.55f, BezelX = 0.636f, BezelY = 0.902f, BezelInk = 0.267f, GlassX = 0.43f, GlassY = 0.677f;

    /// <summary>A glowing shard's halo, its radius and its alpha at the middle; a thread's width and its alpha mid-flight.</summary>
    private const float HaloRadius = 1.375f, HaloAlpha = 0.45f, ThreadWidth = 0.175f, ThreadAlpha = 0.6f;

    private static readonly Color Ink = new("16171b"), Bezel = new("2a2d32");
    /// <summary>Its lens's colour shining and scanning, the mode wheel's too.</summary>
    internal static readonly Color Lamp = new(1f, 0.831f, 0.588f), Scan = new(0.463f, 0.925f, 0.808f);
    private static readonly Color Holo = new(DroneSign.Tint);

    private static Texture2D? _core, _shards;
    private readonly DroneFloat _float = new();
    private readonly DroneSwarm _swarm = new();
    private readonly Shard[] _placed = new Shard[DroneSwarm.Tune.Shards];
    private readonly Node2D _threads = new() { ShowBehindParent = true }, _glow = new();
    private readonly Vector2[] _thread = new Vector2[9];
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
        // The field's light adds, as the pick's did: its threads behind everything, the shards' glow over them.
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
    /// Shows the gesture playing on its hero as a hologram over it (<see cref="DroneSign"/>), the shards fanned toward it, after
    /// <see cref="Follow"/> each frame; none hides it and sends the shards back to the mode's shape.
    /// </summary>
    public void Sign((GestureMotion Motion, double Ms, Vector2 At)? gesture)
    {
        var toward = Vector2.FromAngle(_angle);
        _sign.Show(gesture, toward * LensAhead, toward, _timeMs);
        _swarm.Become(_sign.Visible ? SwarmShape.Sign : Scanning ? SwarmShape.Scan : SwarmShape.Lamp, _angle, _timeMs);
        for (int i = 0; i < _placed.Length; i++) _placed[i] = _swarm.At(i, _angle, _timeMs);
        QueueRedraw();
        _threads.QueueRedraw();
        _glow.QueueRedraw();
    }

    /// <summary>Whether it scans rather than shines: the scanner worn, not the drone.</summary>
    public bool Scanning { get; set; }

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

    /// <summary>The field's colour: the hologram's while the shards fan to a sign, else the lens's.</summary>
    private Color Field => _swarm.Shape == SwarmShape.Sign ? Holo : Scanning ? Scan : Lamp;

    public override void _Draw()
    {
        _core ??= HiRes.Texture(HiRes.Load("drone_core") ?? throw new InvalidOperationException("No picture drone_core"));
        _shards ??= HiRes.Texture(HiRes.Load("drone_shards") ?? throw new InvalidOperationException("No picture drone_shards"));
        float angle = _angle;
        var toward = Vector2.FromAngle(angle);
        var colour = Scanning ? Scan : Lamp;
        for (int i = 0; i < _placed.Length; i++)
            if (!_placed[i].Front) DrawShard(i);
        DrawTextureRect(_core, new Rect2(-CoreBox / 2f, -CoreBox / 2f, CoreBox, CoreBox), false);
        var lens = toward * LensAhead;
        for (int i = 4; i >= 1; i--) DrawCircle(lens, 0.9f * i, new Color(colour, 0.09f));
        DrawColoredPolygon(Ellipse(lens, BezelX + BezelInk / 2, BezelY + BezelInk / 2, angle), Ink);
        DrawColoredPolygon(Ellipse(lens, BezelX - BezelInk / 2, BezelY - BezelInk / 2, angle), Bezel);
        DrawColoredPolygon(Ellipse(lens + toward * 0.06f, GlassX, GlassY, angle), colour.Lerp(Colors.White, 0.3f));
        DrawColoredPolygon(Ellipse(lens + toward * 0.16f, 0.185f, 0.35f, angle), colour.Lerp(Colors.White, 0.8f));
        for (int i = 0; i < _placed.Length; i++)
            if (_placed[i].Front) DrawShard(i);
    }

    /// <summary>Shard <paramref name="i"/> where it is: the nearest of its pictures, turned the rest of the way; its light from above is off by a few degrees at most.</summary>
    private void DrawShard(int i)
    {
        var shard = _placed[i];
        int turn = ((int)Math.Round(shard.Turn / Mathf.Tau * Turns) % Turns + Turns) % Turns;
        DrawSetTransform(new Vector2(shard.X, shard.Y), Mathf.Wrap(shard.Turn - turn * Mathf.Tau / Turns, -Mathf.Pi, Mathf.Pi));
        DrawTextureRectRegion(_shards, new Rect2(-ShardBox / 2f, -ShardBox / 2f, ShardBox, ShardBox), new Rect2(turn * ShardBox, i * ShardBox, ShardBox, ShardBox));
        DrawSetTransform(Vector2.Zero, 0);
    }

    /// <summary>A faint thread of the field from the core to each shard in flight, brightest halfway, bowed as the pick's.</summary>
    private void DrawThreads()
    {
        var colour = Field.Lerp(Colors.White, 0.3f);
        foreach (var shard in _placed)
        {
            if (shard.Flight is <= 0 or >= 1) continue;
            var end = new Vector2(shard.X, shard.Y);
            var bow = new Vector2(end.X * 0.5f + end.Y * 0.25f, end.Y * 0.5f - end.X * 0.25f);
            for (int k = 0; k < _thread.Length; k++)
            {
                float s = k / (_thread.Length - 1f);
                _thread[k] = 2 * (1 - s) * s * bow + s * s * end;
            }
            _threads.DrawPolyline(_thread, new Color(colour, ThreadAlpha * Mathf.Sin(shard.Flight * Mathf.Pi)), ThreadWidth, true);
        }
    }

    /// <summary>A glowing shard's halo, fading out from its middle.</summary>
    private void DrawGlow()
    {
        var colour = Field;
        foreach (var shard in _placed)
        {
            if (shard.Glow <= 0.05f) continue;
            for (int k = 3; k >= 1; k--) _glow.DrawCircle(new Vector2(shard.X, shard.Y), HaloRadius * k / 3, new Color(colour, HaloAlpha * shard.Glow / 3));
        }
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
}
