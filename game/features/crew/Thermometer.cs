using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The vitals' thermometer, a row under the charge (the-thermometer, the user's pick B): the frost star in the signs' column and
/// the hero's body to a tenth (body-temperature), gliding between breaths. Calm and pale at its normal; colder the number turns
/// frost-blue, deeper toward the cold's death, a cold light gathers behind and rime creeps over the star from its edge, laid on top,
/// the sign's own paint untouched. Hotter (2026-11-14-heat-limits, the user's pick C) the star gives way to the heat's flame, the
/// number turns ember and a shimmer rises off it. Hover: the body, the degrees where it stands and both limits. Hidden with no hero
/// or passed out. Reads the sim four times a second.
/// </summary>
public partial class Thermometer : CanvasLayer
{
    private const double RefreshS = 0.25;
    // The star drawn as tall as the oxygen sign and the bolt show (some 17 units: their pictures have more margin), its number
    // larger than the pieces are tall.
    private const float Sign = Vitals.Icon, TextSize = 14, Grow = Sign / 16.5f;
    private static readonly Color Calm = new("d3dee9"), FrostLight = new("9ed8ff"), FrostDeep = new("3a86e0"), Rime = new("e6f7ff");
    private static readonly Color HeatLight = new("ffc46e"), HeatDeep = new("ff5a26");

    /// <summary>The rime's strokes: an angle round the star, a length, when in the frost it starts, whether it forks.</summary>
    private static readonly (float Angle, float Length, float At, bool Fork)[] Strokes = MakeStrokes();

    private readonly Func<ThermometerReading?> _read;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Face _face;
    private readonly TipSpots _tips;
    private readonly Ui _ui;
    private double _sinceRead = RefreshS;
    private ThermometerReading _sim;
    private float _glided;
    private bool _shown;
    private double _clock;

    /// <param name="read">What this player's hero's body reads (<see cref="Thermometers.Read"/>); null without a hero or passed out, which hides it.</param>
    /// <param name="ui">The slots' size the block follows, and the one tooltip.</param>
    public Thermometer(Func<ThermometerReading?> read, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
        _face = new Face(this)
        {
            Position = new Vector2(Vitals.SignX - Sign / 2, Vitals.ColdY - Sign / 2),
            Size = new Vector2(Vitals.RowLeft - Vitals.SignX + Sign / 2 + 40, Sign),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.AddChild(_face);
        AddChild(_root);
        _tips.Add(_face, () => Thermometers.Tip(_sim));
    }

    public override void _Process(double delta)
    {
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        float scale = Vitals.ScaleOf(_ui.Slot);
        (_root.Position, _root.Scale) = (new Vector2(x, y), new Vector2(scale, scale));
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            var read = _read();
            Visible = read != null;
            if (read is { } now)
            {
                // Shown again (a first read, waking up): the reading as it is, nothing to glide from.
                if (!_shown) _glided = now.Body / 100f;
                _sim = now;
            }
            _shown = read != null;
        }
        if (!Visible)
        {
            _tips.Frame(null);
            return;
        }
        _clock += delta;
        float glided = Thermometers.Glide(_glided, _sim.Body / 100f, delta);
        // The shimmer moves while the body is hot: drawn each frame then.
        if (glided != _glided || _glided > Thermometers.Normal)
        {
            _glided = glided;
            _face.QueueRedraw();
        }
        else if (_face.Drawn != Thermometers.Shown(_glided)) _face.QueueRedraw();
        _tips.Frame(!_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
    }

    private static (float, float, float, bool)[] MakeStrokes()
    {
        static float Hash(float n)
        {
            double s = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
            return (float)(s - Math.Floor(s));
        }
        var strokes = new (float, float, float, bool)[12];
        for (int i = 0; i < strokes.Length; i++)
            strokes[i] = (Hash(i * 5.3f) * Mathf.Tau, 2.5f + Hash(i * 9.1f) * 3.5f, i / 12f, Hash(i * 2.7f) > 0.4f);
        return strokes;
    }

    private sealed partial class Face(Thermometer owner) : Control
    {
        /// <summary>The tenths of a degree last drawn.</summary>
        public int Drawn { get; private set; }

        public override void _Draw()
        {
            int tenths = Thermometers.Shown(owner._glided), normal = Thermometers.Shown(Thermometers.Normal);
            Drawn = tenths;
            // Cold or hot as the number reads: a body gliding the last hundredths home shows calm.
            bool cold = tenths < normal, warm = tenths > normal;
            float frost = Thermometers.Frost(owner._glided), hot = Thermometers.Hot(owner._glided), swap = Thermometers.Swap(owner._glided);
            var centre = new Vector2(Sign / 2, Sign / 2);
            var colour = cold ? FrostLight.Lerp(FrostDeep, frost) : warm ? HeatLight.Lerp(HeatDeep, hot) : Calm;
            if (cold) Glow(centre, 13 * Grow, FrostLight with { A = 0.3f * (0.4f + 0.6f * frost) });
            var box = new Rect2(Vector2.Zero, new Vector2(Sign, Sign));
            if (swap < 1) DrawTextureRect(Signs.Cold(), box, false, Colors.White with { A = 1 - swap });
            if (swap > 0) DrawTextureRect(Signs.Heat(), box, false, Colors.White with { A = swap });
            if (cold) DrawRime(centre, frost);
            string text = Thermometers.Body(tenths);
            var at = new Vector2(Vitals.RowLeft - (Vitals.SignX - Sign / 2), Sign / 2 + TextSize * Style.MonoMiddle);
            var size = Style.Mono.GetStringSize(text, HorizontalAlignment.Left, -1, (int)TextSize);
            if (cold) Glow(at + new Vector2(size.X / 2, -TextSize * 0.3f), size.X * 0.7f, colour with { A = 0.25f + 0.25f * frost });
            if (warm) Glow(at + new Vector2(size.X / 2, -TextSize * 0.3f), size.X * 0.7f, colour with { A = 0.18f + 0.2f * hot });
            DrawString(Style.Mono, at, text, HorizontalAlignment.Left, -1, (int)TextSize, colour);
            if (warm) DrawShimmer(at with { Y = Sign / 2 }, size.X, hot, swap);
        }

        /// <summary>Thin wavering strokes rising off the number: more and brighter as the heat deepens.</summary>
        private void DrawShimmer(Vector2 at, float width, float hot, float swap)
        {
            int n = 3 + (int)MathF.Round(3 * hot);
            var points = new Vector2[7];
            float clock = (float)owner._clock;
            for (int i = 0; i < n; i++)
            {
                float phase = (clock * 0.55f + Hash(i * 3.1f)) % 1, x = at.X + width * (i + 0.5f) / n, y = at.Y - 6 - phase * 13;
                float alpha = (1 - phase) * Math.Min(1, phase * 5) * (0.35f + 0.5f * hot) * swap;
                if (alpha <= 0.01f) continue;
                for (int k = 0; k < points.Length; k++) points[k] = new Vector2(x + MathF.Sin(k * 1.2f + clock * 6 + i * 2) * 1.1f, y - k * 1.1f);
                DrawPolyline(points, HeatLight with { A = alpha }, 0.8f, true);
            }
        }

        private static float Hash(float n)
        {
            double h = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
            return (float)(h - Math.Floor(h));
        }

        /// <summary>Rime from the star's edge inward: more strokes, longer, the deeper the frost.</summary>
        private void DrawRime(Vector2 centre, float frost)
        {
            foreach (var (angle, length, from, fork) in Strokes)
            {
                float k = Math.Clamp((frost * 1.15f - from) / 0.25f, 0, 1);
                if (k <= 0) continue;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var start = centre + outward * 9.5f * Grow;
                var tip = start - outward * length * k * Grow;
                var stroke = Rime with { A = 0.75f * k };
                DrawLine(start, tip, stroke, 0.6f * Grow, true);
                if (!fork || length * k <= 2) continue;
                var mid = start - outward * length * k * 0.55f * Grow;
                var side = new Vector2(-outward.X * 0.6f + outward.Y * 0.8f, -outward.Y * 0.6f - outward.X * 0.8f);
                DrawLine(mid, mid + side * length * k * 0.4f * Grow, stroke, 0.6f * Grow, true);
            }
        }

        private void Glow(Vector2 at, float radius, Color colour) =>
            DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);
    }
}
