using Godot;
using Stillwake.Game.Render;
using L = Stillwake.Game.Features.Crew.DroneMotesLook;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's hologram before the hero (<see cref="DroneMotesLook"/>): a small cloud of motes over the drone for a drone window, a
/// blueprint's grid of them with the plan out, eased from one to the other, and a faint cone up to it from the lens, through the edge
/// keys' shader, unlit; a glow under it. Drawn in the world's pixels, on the drone's layer.
/// </summary>
public partial class DroneMotes : Node2D
{
    private static readonly Color Tint = new(DroneSign.Tint);

    private readonly Glow _glow = new();
    private readonly Vector2[] _cone = new Vector2[3];
    private readonly Color[] _coneColours = new Color[3];
    private Vector2 _middle, _lens;
    private double _shown, _grid, _timeMs, _seed;
    private float _alpha, _bright;

    public DroneMotes()
    {
        TopLevel = true;
        (ZAsRelative, ZIndex) = (false, Layers.Drones);
        Visible = false;
        Material = Holo();
        AddChild(_glow);
    }

    /// <summary>
    /// Shows it <paramref name="shown"/> in (0 hides it), <paramref name="grid"/> of the way from the cloud to the plans' grid, its
    /// middle at <paramref name="middle"/>, its cone from <paramref name="lens"/>.
    /// </summary>
    public void Show(double shown, bool coming, double grid, Vector2 middle, Vector2 lens, double timeMs, double seed)
    {
        Visible = shown > 0;
        if (!Visible) return;
        double bright = SheetHoloLook.Brightness(shown, coming);
        _alpha = (float)(DroneSign.Alpha * (1 - DroneSign.FlickerDepth * SheetHoloLook.Flicker(timeMs, seed)));
        (_shown, _grid, _middle, _lens, _timeMs, _seed, _bright) = (shown, grid, middle, lens, timeMs, seed, (float)bright);
        _glow.Position = middle;
        float glow = (float)(SheetHoloLook.Light * bright * 0.25);
        _glow.Colour = new Color(Tint.R * glow, Tint.G * glow, Tint.B * glow);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = _alpha * _bright;
        float half = (float)(L.HalfWide * L.ConeWide);
        if ((_lens - _middle).Length() > 2)
        {
            (_cone[0], _cone[1], _cone[2]) = (_lens, _middle + new Vector2(-half, 0), _middle + new Vector2(half, 0));
            (_coneColours[0], _coneColours[1], _coneColours[2]) =
                (Tint with { A = alpha * (float)L.ConeAtLens }, Tint with { A = alpha * (float)L.ConeAtCloud }, Tint with { A = alpha * (float)L.ConeAtCloud });
            DrawPolygon(_cone, _coneColours);
        }
        var lens = _lens - _middle;
        for (int i = 0; i < L.Count; i++)
        {
            var (x, y) = L.At(i, _shown, _timeMs, _seed, (lens.X, lens.Y), _grid);
            Square(_middle + new Vector2((float)x, (float)y), (float)L.Side, alpha * (float)L.Twinkle(i, _timeMs, _seed));
        }
    }

    /// <summary>A rect, not a polygon: Godot fails to triangulate one this small far out on a planet and logs it every frame, which cost a millisecond.</summary>
    private void Square(Vector2 at, float side, float alpha)
    {
        if (side <= 0 || alpha <= 0.004f) return;
        DrawRect(new Rect2(at.X - side / 2, at.Y - side / 2, side, side), Tint with { A = alpha });
    }

    /// <summary>The edge keys' hologram shader, unlit here in the world as the drone is: the same code with <c>unshaded</c> added.</summary>
    private static ShaderMaterial Holo()
    {
        const string Mode = "render_mode blend_add;";
        var code = GD.Load<Shader>("res://shared/holo.gdshader").Code;
        if (!code.Contains(Mode, System.StringComparison.Ordinal)) throw new System.InvalidOperationException("holo.gdshader's render_mode changed");
        var material = new ShaderMaterial { Shader = new Shader { Code = code.Replace(Mode, "render_mode blend_add, unshaded;", System.StringComparison.Ordinal) } };
        material.SetShaderParameter("grey", false);
        return material;
    }
}
