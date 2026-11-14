using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The vitals' thermometer, right of the meals (the-thermometer, the user's pick B): the frost star and the degrees where the
/// hero stands. Calm and pale inside its limit; past it the number turns frost-blue, deeper the further, a cold light gathers
/// behind and rime creeps over the star from its edge, laid on top, the sign's own paint untouched. Hover: the reading and the
/// limit. Hidden with no hero or passed out. Reads the sim four times a second.
/// </summary>
public partial class Thermometer : CanvasLayer
{
    private const double RefreshS = 0.25;
    private const float Sign = 11 * Vitals.SignScale, TextSize = 10;
    private static readonly Color Calm = new("d3dee9"), FrostLight = new("9ed8ff"), FrostDeep = new("3a86e0"), Rime = new("e6f7ff");

    /// <summary>The rime's strokes: an angle round the star, a length, when in the frost it starts, whether it forks.</summary>
    private static readonly (float Angle, float Length, float At, bool Fork)[] Strokes = MakeStrokes();

    private readonly Func<(int Degrees, int Low, int Felt)?> _read;
    private readonly Control _face;
    private readonly TipSpots _tips;
    private readonly Ui _ui;
    private double _sinceRead = RefreshS;
    private (int Degrees, int Low, int Felt) _shown;

    /// <param name="read">The degrees at this player's hero's body, the coldest it withstands and the degrees it feels past that; null without a hero or passed out, which hides it.</param>
    /// <param name="ui">The slots' size the block follows, and the one tooltip.</param>
    public Thermometer(Func<(int Degrees, int Low, int Felt)?> read, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
        _face = new Face(this)
        {
            Position = new Vector2(Vitals.ColdX - Sign / 2, Vitals.ColdY - Sign / 2),
            Size = new Vector2(Vitals.ColdText - Vitals.ColdX + Sign / 2 + 26, Sign),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_face);
        AddChild(root);
        _tips.Add(_face, () => Thermometers.Tip(_shown.Degrees, _shown.Low, _shown.Felt));
    }

    public override void _Process(double delta)
    {
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        float scale = Vitals.ScaleOf(_ui.Slot);
        var root = (Control)_face.GetParent();
        (root.Position, root.Scale) = (new Vector2(x, y), new Vector2(scale, scale));
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            var read = _read();
            Visible = read != null;
            if (read is { } now && now != _shown)
            {
                _shown = now;
                _face.QueueRedraw();
            }
        }
        _tips.Frame(Visible && !_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
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
        public override void _Draw()
        {
            var (degrees, _, felt) = owner._shown;
            float frost = Thermometers.Frost(felt);
            var centre = new Vector2(Sign / 2, Sign / 2);
            var colour = felt > 0 ? FrostLight.Lerp(FrostDeep, frost) : Calm;
            if (felt > 0) Glow(centre, 13, FrostLight with { A = 0.3f * (0.4f + 0.6f * frost) });
            DrawTextureRect(Signs.Cold(), new Rect2(Vector2.Zero, new Vector2(Sign, Sign)), false);
            if (felt > 0) DrawRime(centre, frost);
            string text = Thermometers.Reading(degrees);
            var at = new Vector2(Vitals.ColdText - (Vitals.ColdX - Sign / 2), Sign / 2 + TextSize * Style.MonoMiddle + TextSize * 0.25f);
            if (felt > 0)
            {
                var size = Style.Mono.GetStringSize(text, HorizontalAlignment.Left, -1, (int)TextSize);
                Glow(at + new Vector2(size.X / 2, -TextSize * 0.3f), size.X * 0.7f, colour with { A = 0.25f + 0.25f * frost });
            }
            DrawString(Style.Mono, at, text, HorizontalAlignment.Left, -1, (int)TextSize, colour);
        }

        /// <summary>Rime from the star's edge inward: more strokes, longer, the deeper the frost.</summary>
        private void DrawRime(Vector2 centre, float frost)
        {
            foreach (var (angle, length, from, fork) in Strokes)
            {
                float k = Math.Clamp((frost * 1.15f - from) / 0.25f, 0, 1);
                if (k <= 0) continue;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var start = centre + outward * 9.5f;
                var tip = start - outward * length * k;
                var stroke = Rime with { A = 0.75f * k };
                DrawLine(start, tip, stroke, 0.6f, true);
                if (!fork || length * k <= 2) continue;
                var mid = start - outward * length * k * 0.55f;
                var side = new Vector2(-outward.X * 0.6f + outward.Y * 0.8f, -outward.Y * 0.6f - outward.X * 0.8f);
                DrawLine(mid, mid + side * length * k * 0.4f, stroke, 0.6f, true);
            }
        }

        private void Glow(Vector2 at, float radius, Color colour) =>
            DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);
    }
}
