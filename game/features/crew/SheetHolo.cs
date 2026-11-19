using System;
using Godot;
using Stillwake.Game.Render;
using L = Stillwake.Game.Features.Crew.SheetHoloLook;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's hologram in the planner sheet's place (<see cref="SheetHoloLook"/>): the band round the eyes, its squares and
/// marker, and a faint cone up from the drone's lens, through the edge keys' shader with its scanlines, unlit; a glow under it
/// lights the helmet. Drawn in the world's pixels, apart from its figure's turns.
/// </summary>
public partial class SheetHolo : Node2D
{
    private const int Steps = 14;
    private static readonly Color Tint = new(DroneSign.Tint), Pale = Tint.Lerp(Colors.White, (float)L.DotPale);

    private readonly Glow _glow = new();
    private readonly Vector2[] _band = new Vector2[2 * (Steps + 1)], _rim = new Vector2[2 * (Steps + 1) + 1], _cone = new Vector2[3];
    private readonly Color[] _coneColours = new Color[3];
    private readonly Vector2[] _square = new Vector2[4];
    private Vector2 _eye, _lens;
    private double _radius, _toward, _half, _thick;
    private float _alpha;

    public SheetHolo()
    {
        // Apart from the figure it is apart from its layer too: on the drone's, over the machines, as the drone is.
        TopLevel = true;
        (ZAsRelative, ZIndex) = (false, Layers.Drones);
        Visible = false;
        Material = DroneHolo.Holo(grey: false);
        AddChild(_glow);
    }

    /// <summary>Where its middle is, in the world's pixels, as last shown.</summary>
    public Vector2 Middle { get; private set; }

    /// <summary>
    /// Shows it <paramref name="shown"/> in (0 hides it), <paramref name="coming"/> on or going out, round <paramref name="eye"/>
    /// through the sheet's middle <paramref name="middle"/>, its cone from <paramref name="lens"/>, brighter by <paramref name="thrust"/>.
    /// </summary>
    public void Show(double shown, bool coming, Vector2 eye, Vector2 middle, Vector2 lens, double thrust, double timeMs, double seed)
    {
        Visible = shown > 0;
        if (!Visible) return;
        (_radius, _toward, _half) = L.Arc((eye.X, eye.Y), (middle.X, middle.Y));
        _thick = L.HalfThick * L.Thickness(shown, coming);
        _alpha = (float)(DroneSign.Alpha * L.Brightness(shown, coming) * (1 - DroneSign.FlickerDepth * L.Flicker(timeMs, seed)) * (1 + 0.8 * thrust));
        (_eye, _lens, Middle) = (eye, lens, middle);
        _glow.Position = middle;
        float glow = (float)(L.Light * L.Brightness(shown, coming) * (1 + 1.5 * thrust));
        _glow.Colour = new Color(Tint.R * glow * 0.35f, Tint.G * glow * 0.35f, Tint.B * glow * 0.35f);
        QueueRedraw();
    }

    /// <summary>The spot (<paramref name="along"/>, <paramref name="across"/>) on it as last shown (<see cref="SheetPoke"/>), in the world's pixels; none while it is out.</summary>
    public Vector2? Spot(double along, double across, int facing) =>
        Visible ? On(L.AngleOf(_toward, _half, along, facing), _radius + across * _thick) : null;

    /// <summary>How far along it, -1..1, the line from the eyes to <paramref name="at"/> crosses it as last shown.</summary>
    public double AlongTo(Vector2 at, int facing) => L.AlongTo((_eye.X, _eye.Y), _toward, _half, (at.X, at.Y), facing);

    private Vector2 On(double angle, double radius) => _eye + new Vector2((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius));

    public override void _Draw()
    {
        double from = _toward - _half, step = 2 * _half / Steps;
        for (int i = 0; i <= Steps; i++)
        {
            _band[i] = On(from + i * step, _radius + _thick);
            _band[2 * Steps + 1 - i] = On(from + i * step, _radius - _thick);
        }
        // The cone first, under the band: from the lens to the band's two ends seen from it.
        int left = 0, right = 0;
        float leftAt = float.MaxValue, rightAt = float.MinValue, toBand = (Middle - _lens).Angle();
        for (int i = 0; i < _band.Length; i++)
        {
            float a = Mathf.Wrap((_band[i] - _lens).Angle() - toBand, -Mathf.Pi, Mathf.Pi);
            if (a < leftAt) (leftAt, left) = (a, i);
            if (a > rightAt) (rightAt, right) = (a, i);
        }
        if (_lens.DistanceTo(Middle) > 2)
        {
            (_cone[0], _cone[1], _cone[2]) = (_lens, _band[left], _band[right]);
            (_coneColours[0], _coneColours[1], _coneColours[2]) =
                (Tint with { A = _alpha * (float)L.ConeAtLens }, Tint with { A = _alpha * (float)L.ConeAtBand }, Tint with { A = _alpha * (float)L.ConeAtBand });
            DrawPolygon(_cone, _coneColours);
        }
        DrawColoredPolygon(_band, Tint with { A = _alpha * (float)L.Fill });
        Array.Copy(_band, _rim, _band.Length);
        _rim[^1] = _band[0];
        DrawPolyline(_rim, Tint with { A = _alpha * (float)L.Rim }, (float)L.RimWidth, antialiased: true);
        float squash = (float)(_thick / L.HalfThick), half = (float)L.Square / 2;
        var squares = Tint with { A = _alpha * (float)L.SquareAlpha };
        for (int row = -1; row <= 1; row += 2)
            for (int i = 0; i < L.Squares; i++)
            {
                double angle = from + (i + 0.5) * (2 * _half / L.Squares);
                var at = On(angle, _radius + row * L.RowOff * squash);
                var along = new Vector2((float)-Math.Sin(angle), (float)Math.Cos(angle)) * half;
                var across = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * half * squash;
                (_square[0], _square[1], _square[2], _square[3]) = (at - along - across, at + along - across, at + along + across, at - along + across);
                DrawColoredPolygon(_square, squares);
            }
        DrawCircle(On(_toward + _half * L.DotAt, _radius), (float)L.DotRadius, Pale with { A = _alpha * (float)L.DotAlpha });
    }
}
