using Godot;
using Stillwake.Game.Render;
using L = Stillwake.Game.Features.Crew.DroneMotesLook;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's hologram of a drone window before the hero (<see cref="DroneMotesLook"/>): a small cloud of motes over the drone and a
/// faint cone up to it from the lens, through the edge keys' shader, unlit; a glow under it. Drawn in the world's pixels, on the
/// drone's layer.
/// </summary>
public partial class DroneMotes : Node2D
{
    private static readonly Color Tint = new(DroneSign.Tint);

    private readonly Glow _glow = new();
    private readonly Vector2[] _cone = new Vector2[3], _square = new Vector2[4];
    private readonly Color[] _coneColours = new Color[3];
    private Vector2 _middle, _lens;
    private double _shown, _timeMs, _seed;
    private float _alpha;

    public DroneMotes()
    {
        TopLevel = true;
        (ZAsRelative, ZIndex) = (false, Layers.Drones);
        Visible = false;
        Material = DroneHolo.Holo(grey: false);
        AddChild(_glow);
    }

    /// <summary>Shows it <paramref name="shown"/> in (0 hides it), its middle at <paramref name="middle"/>, its cone from <paramref name="lens"/>.</summary>
    public void Show(double shown, bool coming, Vector2 middle, Vector2 lens, double timeMs, double seed)
    {
        Visible = shown > 0;
        if (!Visible) return;
        double bright = SheetHoloLook.Brightness(shown, coming);
        _alpha = (float)(DroneSign.Alpha * bright * (1 - DroneSign.FlickerDepth * SheetHoloLook.Flicker(timeMs, seed)));
        (_shown, _middle, _lens, _timeMs, _seed) = (shown, middle, lens, timeMs, seed);
        _glow.Position = middle;
        float glow = (float)(SheetHoloLook.Light * bright * 0.25);
        _glow.Colour = new Color(Tint.R * glow, Tint.G * glow, Tint.B * glow);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var lens = _lens - _middle;
        float half = (float)(L.HalfWide * L.ConeWide);
        if (lens.Length() > 2)
        {
            (_cone[0], _cone[1], _cone[2]) = (_lens, _middle + new Vector2(-half, 0), _middle + new Vector2(half, 0));
            (_coneColours[0], _coneColours[1], _coneColours[2]) =
                (Tint with { A = _alpha * (float)L.ConeAtLens }, Tint with { A = _alpha * (float)L.ConeAtCloud }, Tint with { A = _alpha * (float)L.ConeAtCloud });
            DrawPolygon(_cone, _coneColours);
        }
        float side = (float)L.Side / 2;
        for (int i = 0; i < L.Count; i++)
        {
            var (x, y) = L.At(i, _shown, _timeMs, _seed, (lens.X, lens.Y));
            var at = _middle + new Vector2((float)x, (float)y);
            (_square[0], _square[1], _square[2], _square[3]) =
                (at + new Vector2(-side, -side), at + new Vector2(side, -side), at + new Vector2(side, side), at + new Vector2(-side, side));
            DrawColoredPolygon(_square, Tint with { A = _alpha * (float)L.Twinkle(i, _timeMs, _seed) });
        }
    }
}
