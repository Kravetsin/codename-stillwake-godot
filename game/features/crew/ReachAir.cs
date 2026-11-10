using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The air a tool bends while it is used (blade-trail): the haze a held blade leaves where it has been (<see cref="BladeTrail"/>),
/// and round a running grinder's wheel, over its zone (<see cref="AirCuts"/>). Drawn in the arms' frame, the shoulder at its
/// origin, so a figure facing left mirrors it; a breath paler, unshaded, and the world behind it bent on a node of its own.
/// </summary>
public partial class ReachAir : Node2D
{
    // The world seen through the trail, bent as through hot air, by the trail's own opacity, and a breath paler.
    private static readonly Shader HazeShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;
            uniform float strength = 7.0;
            void fragment() {
                float m = COLOR.a;
                float wave = sin(FRAGCOORD.y * 0.55 + TIME * 22.0) * 0.6 + sin(FRAGCOORD.x * 0.4 - TIME * 15.0) * 0.4;
                float lift = cos(FRAGCOORD.x * 0.5 + TIME * 18.0);
                vec4 seen = texture(screen, SCREEN_UV + vec2(wave, 0.6 * lift) * m * strength * SCREEN_PIXEL_SIZE);
                COLOR = vec4(seen.rgb + vec3(0.05) * m, min(1.0, m * 5.0) * seen.a);
            }
            """,
    };

    /// <summary>The haze's ink is only how much it bends; over it, a breath of pale air.</summary>
    private static readonly Ink.Tone Bend = Ink.Tone.Of(255, 255, 255, 1), Breath = WorkLooks.AirInk.Faded(0.1f);

    private readonly AirCuts _cuts = new();
    private readonly BladeTrail _trail;
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();
    private readonly Func<(double X, double Y), (double X, double Y)> _local;
    /// <summary>The node the trail's haze is drawn on, at the shimmers' layer: over the world, under what is drawn over it. The haze itself has none.</summary>
    private readonly ReachAir? _haze;
    private Transform2D _here;

    public ReachAir() : this(null)
    {
    }

    private ReachAir(BladeTrail? hazeOf)
    {
        _trail = hazeOf ?? new BladeTrail();
        _local = Local;
        Visible = false;
        if (hazeOf != null)
        {
            Material = new ShaderMaterial { Shader = HazeShader };
            (ZIndex, ZAsRelative) = (Layers.Shimmer, false);
            return;
        }
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        AddChild(_haze = new ReachAir(_trail));
    }

    /// <summary>The haze round a running grinder's wheel, this far either side of its hub, in pixels.</summary>
    private const double WheelRound = 11;

    private readonly System.Collections.Generic.List<(double X, double Y)> _shape = [];

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the running tool's <paramref name="zone"/>, none while none runs, and the held blade's
    /// <paramref name="edge"/> where it is in the world, none while none is held.
    /// </summary>
    public void Follow(AirCuts.Zone? zone, double elapsedMs, (Vector2 From, Vector2 To)? edge = null)
    {
        _cuts.Step(elapsedMs, zone);
        _trail.Step(elapsedMs, edge is { } e ? ((e.From.X, e.From.Y), (e.To.X, e.To.Y)) : null);
        Visible = _haze!.Visible = _trail.Live || _cuts.Working != null;
        if (!Visible) return;
        QueueRedraw();
        _haze._working = _cuts.Working;
        _haze.QueueRedraw();
    }

    private (AirCuts.Zone Zone, double On)? _working;

    // A running wheel bends the air round it; a bit and a thrust show nothing of their own (model-reach).
    private void Wheel()
    {
        if (_working is not ({ Disc: true } zone, var on)) return;
        var (dx, dy) = zone.Toward;
        _shape.Clear();
        for (int i = 0; i < 16; i++)
        {
            double a = i * System.Math.PI / 8, along = System.Math.Cos(a) * WheelRound, across = System.Math.Sin(a) * WheelRound;
            _shape.Add((zone.At.X + dx * along - dy * across, zone.At.Y + dy * along + dx * across));
        }
        // A haze has no edge: it fades over half its radius.
        float was = _ink.Feather;
        _ink.Feather = (float)(WheelRound * 0.5);
        _ink.Fill(_shape, zone.At, Bend.Faded((float)on));
        _ink.Feather = was;
    }

    public override void _Draw()
    {
        _ink.Clear();
        _ink.Feather = InkCanvas.PixelOf(this);
        Wheel();
        // The trail stays where it was swept in the world, whatever the figure does since.
        _here = GetGlobalTransform().AffineInverse();
        _trail.Draw(_ink, _local, _haze != null ? Breath : Bend);
        _canvas.Draw(this, _ink);
    }

    private (double X, double Y) Local((double X, double Y) p)
    {
        var l = _here * new Vector2((float)p.X, (float)p.Y);
        return (l.X, l.Y);
    }
}
