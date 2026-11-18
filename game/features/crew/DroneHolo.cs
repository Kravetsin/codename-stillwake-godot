using System;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone's hologram of a gesture (<see cref="DroneSign"/>): the wheel's hand (<see cref="WheelView.Hand"/>) in pale cyan, added
/// through the edge keys' shader (holo.gdshader) with its scanlines, and a faint cone up from the lens. A child of the drone; unlit.
/// </summary>
public partial class DroneHolo : Node2D
{
    /// <summary>The cone's width under the sign at full size, and its alpha at the lens and under the sign, of the sign's alpha.</summary>
    private const float ConeWide = 5, ConeAtLens = 0.45f, ConeAtSign = 0.12f;

    private static Shader? _shader;
    private static readonly Color Tint = new(DroneSign.Tint);
    private readonly Node2D _hand = new() { TextureFilter = TextureFilterEnum.Linear };
    private readonly Vector2[] _cone = new Vector2[3];
    private readonly Color[] _coneColours = new Color[3];
    private Gesture? _kind;
    private float _alpha, _scale;
    private Vector2 _lens;

    public DroneHolo()
    {
        Visible = false;
        Material = Holo(grey: false);
        _hand.Material = Holo(grey: true);
        _hand.Draw += () =>
        {
            if (_kind is { } kind) WheelView.Hand(_hand, Vector2.Zero, (float)DroneSign.Height, kind);
        };
        AddChild(_hand);
    }

    /// <summary>
    /// Shows <paramref name="gesture"/> (its motion, how far in, where a point points in world px) over the drone, from its lens at
    /// <paramref name="lens"/> from its middle, a point's fallback turn <paramref name="toward"/>; none hides it.
    /// </summary>
    public void Show((GestureMotion Motion, double Ms, Vector2 At)? gesture, Vector2 lens, Vector2 toward, double timeMs)
    {
        if (gesture is not { } sign || DroneSign.KindOf(sign.Motion) is not { } kind)
        {
            Visible = false;
            return;
        }
        var (risen, scale, alpha) = DroneSign.At(sign.Motion, sign.Ms, timeMs);
        Visible = alpha > 0;
        if (!Visible) return;
        if (_kind != kind) _hand.QueueRedraw();
        (_kind, _alpha, _scale, _lens) = (kind, (float)alpha, (float)scale, lens);
        var (x, y) = DroneSign.Place(risen, (lens.X, lens.Y));
        var at = new Vector2((float)x, (float)y);
        var pointed = sign.At - GlobalPosition - at;
        var line = pointed.Length() >= 1 ? pointed : toward;
        var (turn, mirrored) = DroneSign.Turned(kind, (line.X, line.Y));
        _hand.Position = at;
        _hand.Rotation = (float)turn;
        _hand.Scale = new Vector2(mirrored ? -_scale : _scale, _scale);
        _hand.Modulate = Tint with { A = _alpha };
        QueueRedraw();
    }

    public override void _Draw()
    {
        // The cone from the lens to under the hand, fading up: the light it is made of.
        var under = _hand.Position + new Vector2(0, (float)DroneSign.Height * 0.4f * _scale);
        var half = new Vector2(ConeWide / 2 * _scale, 0);
        (_cone[0], _cone[1], _cone[2]) = (_lens, under - half, under + half);
        (_coneColours[0], _coneColours[1], _coneColours[2]) =
            (Tint with { A = _alpha * ConeAtLens }, Tint with { A = _alpha * ConeAtSign }, Tint with { A = _alpha * ConeAtSign });
        if (under.DistanceTo(_lens) >= 0.5f) DrawPolygon(_cone, _coneColours);
    }

    /// <summary>The edge keys' hologram shader, unlit here in the world as the drone is: the same code with <c>unshaded</c> added.</summary>
    private static ShaderMaterial Holo(bool grey)
    {
        if (_shader == null)
        {
            const string Mode = "render_mode blend_add;";
            var code = GD.Load<Shader>("res://shared/holo.gdshader").Code;
            if (!code.Contains(Mode, StringComparison.Ordinal)) throw new InvalidOperationException("holo.gdshader's render_mode changed");
            _shader = new Shader { Code = code.Replace(Mode, "render_mode blend_add, unshaded;", StringComparison.Ordinal) };
        }
        var material = new ShaderMaterial { Shader = _shader };
        material.SetShaderParameter("grey", grey);
        return material;
    }
}
