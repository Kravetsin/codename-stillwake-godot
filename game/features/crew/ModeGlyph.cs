using System;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The power driver's mode by the arrow (mode-glyph, the picked "Знак у курсора"): its square's dots on a dark plate above and
/// right of the arrow's tip, clear of the tips below it. A switch eases each dot that changes, the far corner first.
/// </summary>
public partial class ModeGlyph : CanvasLayer
{
    private static readonly Color Waits = new("b0574a");
    private static readonly StyleBoxFlat Plate = new()
    {
        BgColor = new Color(5 / 255f, 8 / 255f, 14 / 255f, 0.62f),
        CornerRadiusTopLeft = 2,
        CornerRadiusTopRight = 2,
        CornerRadiusBottomLeft = 2,
        CornerRadiusBottomRight = 2,
    };
    /// <summary>The plate's place from the arrow's tip, its side, a dot box's side and the gap, in pixels at the normal slot.</summary>
    private static readonly Vector2 FromArrow = new(11, -14);
    private const float Side = 11, Box = 8, Gap = 1, Pad = 1.5f;
    /// <summary>How long a dot takes to change, and how much later each next one starts, in milliseconds.</summary>
    private const double FadeMs = 160, StaggerMs = 45;

    private readonly Ui _ui;
    private readonly Control _plate = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private ModeDot[] _from = [], _to = [];
    private double _since = double.MaxValue;
    private Vector2 _arrow;

    public ModeGlyph(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.Tips;
        _plate.Draw += Paint;
        AddChild(_plate);
    }

    /// <summary>Shows <paramref name="dots"/> by the arrow's tip at <paramref name="arrow"/>, on the screen; none hides it. A change starts the dots easing to it.</summary>
    public void Show(ModeDot[]? dots, Vector2 arrow)
    {
        _arrow = arrow;
        _plate.Visible = dots != null;
        if (dots == null) return;
        if (dots.Length != _to.Length) (_from, _to, _since) = (dots, dots, double.MaxValue);
        else if (!dots.SequenceEqual(_to)) (_from, _to, _since) = (_to, dots, 0);
    }

    public override void _Process(double delta)
    {
        if (!_plate.Visible) return;
        if (_since < double.MaxValue) _since += delta * 1000;
        float scale = Slots.SizeOf(_ui.SlotScale.Get()) / 40f;
        _plate.Scale = Vector2.One * scale;
        _plate.Position = (_arrow + FromArrow * scale).Round();
        _plate.QueueRedraw();
    }

    private void Paint()
    {
        int side = (int)Math.Round(Math.Sqrt(_to.Length));
        if (side < 2) return;
        _plate.DrawStyleBox(Plate, new Rect2(0, 0, Side, Side));
        float d = (Box - Gap * (side - 1)) / side;
        for (int i = 0; i < _to.Length; i++)
        {
            // The bottom row first, left to right, as the square is worked; the far corner changes first.
            int col = i % side, row = side - 1 - i / side;
            var at = new Vector2(Pad + col * (d + Gap), Pad + row * (d + Gap));
            double k = Ease(Math.Clamp((_since - (_to.Length - 1 - i) * StaggerMs) / FadeMs, 0, 1));
            float lit = (float)Mix(_from[i] == ModeDot.Lit, _to[i] == ModeDot.Lit, k);
            float waits = (float)Mix(_from[i] == ModeDot.Waiting, _to[i] == ModeDot.Waiting, k);
            if (lit > 0.01f)
            {
                float s = d * (0.55f + 0.45f * lit), o = (d - s) / 2;
                _plate.DrawRect(new Rect2(at + Vector2.One * o, new Vector2(s, s)), Outline.SquareReady with { A = lit });
            }
            if (waits > 0.01f) _plate.DrawRect(new Rect2(at + Vector2.One * 0.5f, new Vector2(d - 1, d - 1)), Waits with { A = 0.9f * waits }, filled: false, width: 1);
        }
    }

    private static double Ease(double v) => v * v * (3 - 2 * v);
    private static double Mix(bool from, bool to, double k) => (from ? 1 : 0) + ((to ? 1 : 0) - (from ? 1 : 0)) * k;
}
