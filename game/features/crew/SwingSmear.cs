using System;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The swipe of light an axe's blade leaves on its downstroke (0075): a band round the shoulder from where the
/// stroke began to the blade, brightest at the edge and fading into nothing behind it, with a thin bright rim;
/// after the strike it shrinks toward the blade and dies away. Drawn in the arms' frame, the shoulder at its
/// origin, so a figure facing left mirrors it; added over the figure.
/// </summary>
public partial class SwingSmear : Node2D
{
    private const int Segments = 12;
    /// <summary>The longest the band trails the blade, radians, times <see cref="Fx.Length"/>.</summary>
    private const double Longest = 1.6;
    /// <summary>Each 60 Hz frame after the stroke the band keeps this much of its light and its tail closes this share on the blade.</summary>
    private const double Keep = 0.68, Close = 0.3;
    private static readonly Color Tail = new(190 / 255f, 215 / 255f, 1), Head = new(230 / 255f, 240 / 255f, 1);

    private double _tail, _head, _wind, _light;
    private float _inner, _outer;
    private bool _shown, _striking;
    private readonly Vector2[] _band = new Vector2[2 * (Segments + 1)], _rim = new Vector2[Segments + 1];
    private readonly Color[] _bandColours = new Color[2 * (Segments + 1)], _rimColours = new Color[Segments + 1];

    public SwingSmear()
    {
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
        Visible = false;
    }

    /// <summary>
    /// The band <paramref name="elapsedMs"/> on: following a blade whose edge runs from <paramref name="from"/> to
    /// <paramref name="to"/>, pixels from the shoulder, while <paramref name="striking"/>; dying away after.
    /// </summary>
    public void Follow(Vector2? from, Vector2? to, bool striking, double elapsedMs)
    {
        double frames = elapsedMs * 60 / 1000;
        if (striking && from is { } f && to is { } t)
        {
            double head = Math.Atan2(t.Y, t.X);
            // A new stroke starts its band afresh, whatever is left of the last.
            if (!_striking) (_shown, _wind, _head) = (true, head, head);
            _head = Math.Max(_head, head);
            _tail = Math.Max(_wind, _head - Longest * Fx.Length);
            (_inner, _outer, _light) = (f.Length(), t.Length() + 0.6f, 1);
        }
        else if (_shown)
        {
            _light *= Math.Pow(Keep, frames);
            _tail += (_head - _tail) * (1 - Math.Pow(1 - Close, frames));
            if (_light < 0.03) _shown = false;
        }
        _striking = striking && from != null;
        Visible = _shown && _head - _tail > 0.02;
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        double span = _head - _tail;
        float k = (float)(_light * Fx.Glow), width = Math.Max(0.35f, 1.1f / Math.Max(0.01f, GetViewport().GetCanvasTransform().X.Length()));
        for (int i = 0; i <= Segments; i++)
        {
            double q = (double)i / Segments, a = _tail + span * q;
            var way = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
            // Clear at the tail, faint most of the way, brightest at the blade.
            var colour = q < 0.7 ? Tail with { A = (float)(0.16 * q / 0.7) * k } : Tail.Lerp(Head, (float)((q - 0.7) / 0.3)) with { A = (float)(0.16 + 0.26 * (q - 0.7) / 0.3) * k };
            _band[i] = way * _outer;
            _band[2 * Segments + 1 - i] = way * _inner;
            _bandColours[i] = _bandColours[2 * Segments + 1 - i] = colour;
            _rim[i] = way * _outer;
            _rimColours[i] = Colors.White with { A = (float)(0.95 * q * q) * (float)_light };
        }
        DrawPolygon(_band, _bandColours);
        DrawPolylineColors(_rim, _rimColours, width, true);
    }
}
