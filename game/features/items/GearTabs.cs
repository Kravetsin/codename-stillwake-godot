using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The equipment's two folder tabs on top of its metal (gear-tabs, pick B): Gear, a helmet, and Appearance, a coat hanger. The one
/// open is the metal's own, merged into the frame, its name on a strip of screen in the screen's colour, cyan on the gear's, amber
/// on the Appearance's; the other sits back, darker. A press on the closed one opens it. Laid out at the slot size.
/// </summary>
public partial class GearTabs : HBoxContainer
{
    private readonly Tab _gear, _looks;

    /// <param name="slot">The slot size it is laid out for.</param>
    /// <param name="chose">The tab pressed: true for Appearance.</param>
    public GearTabs(int slot, Action<bool> chose)
    {
        float k = slot / (float)DollLayout.BaseSlot;
        Alignment = AlignmentMode.Begin;
        AddThemeConstantOverride("separation", Mathf.RoundToInt(3 * k));
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_gear = new Tab(T("items.gear"), looks: false, k, () => chose(false)));
        AddChild(_looks = new Tab(T("items.looks"), looks: true, k, () => chose(true)));
        CustomMinimumSize = GetCombinedMinimumSize();
        Show(false);
    }

    /// <summary>Which tab is open: Appearance or the gear.</summary>
    public void Show(bool looks)
    {
        _gear.Open = !looks;
        _looks.Open = looks;
    }

    /// <summary>One folder tab: its glyph and name, drawn in code.</summary>
    private sealed partial class Tab : Control
    {
        private static readonly Color BackTop = new("3e434b"), BackFoot = new("2b2f36"), OpenTop = new("8a9099"), OpenFoot = new("6c7179");
        private static readonly Color Ink = new("77818f"), InkHover = new("a8b2c0"), Cyan = new("62d6ea"), Amber = new("e9a84c");
        private static readonly Color GearGlass = new("0e1526"), LooksGlass = new("1b120a");

        private readonly string _name;
        private readonly bool _looksTab;
        private readonly float _k;
        private readonly Action _pressed;
        private readonly int _fontSize;
        private bool _open, _hovered;

        public Tab(string name, bool looks, float k, Action pressed)
        {
            (_name, _looksTab, _k, _pressed) = (name.ToUpperInvariant(), looks, k, pressed);
            _fontSize = Mathf.RoundToInt(9 * k);
            float text = Style.Mono.GetStringSize(_name, HorizontalAlignment.Left, -1, _fontSize).X;
            CustomMinimumSize = new Vector2(Mathf.Ceil(text + (6 + 6 + 11 + 5 + 6 + 6) * k), Mathf.Ceil(26 * k));
            SizeFlagsVertical = SizeFlags.ShrinkEnd;
            MouseFilter = MouseFilterEnum.Stop;
            MouseDefaultCursorShape = CursorShape.PointingHand;
            MouseEntered += () => Hover(true);
            MouseExited += () => Hover(false);
            GuiInput += e =>
            {
                if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
                AcceptEvent();
                if (!_open) _pressed();
            };
        }

        public bool Open
        {
            set
            {
                _open = value;
                QueueRedraw();
            }
        }

        private void Hover(bool on)
        {
            _hovered = on;
            QueueRedraw();
        }

        public override void _Draw()
        {
            // The open one stands taller and reaches a pixel into the metal under it; the closed one sits back.
            float top = _open ? 0 : 6 * _k, w = Size.X, h = Size.Y + (_open ? 2 : 0), r = 5 * _k;
            Shape(top, w, h, r, _open ? OpenTop : BackTop, _open ? OpenFoot : BackFoot);
            DrawLine(new Vector2(r, top + 0.5f), new Vector2(w - r, top + 0.5f), Colors.White with { A = _open ? 0.5f : 0.12f });
            var (ink, glass) = _open ? (_looksTab ? Amber : Cyan, _looksTab ? LooksGlass : GearGlass) : (_hovered ? InkHover : Ink, default);
            float pad = 6 * _k;
            var label = new Rect2(pad, top + 5 * _k, w - 2 * pad, Size.Y - top - 11 * _k);
            if (_open)
            {
                DrawRect(label, glass);
                // The glass's recess, as the screen's own.
                DrawRect(new Rect2(label.Position, new Vector2(label.Size.X, 1)), Colors.Black with { A = 0.6f });
                DrawRect(new Rect2(label.Position, new Vector2(1, label.Size.Y)), Colors.Black with { A = 0.4f });
            }
            float icon = 11 * _k, x = label.Position.X + 6 * _k, middle = label.Position.Y + label.Size.Y / 2;
            if (_looksTab) Hanger(new Vector2(x, middle - icon / 2), icon, ink);
            else Helmet(new Vector2(x, middle - icon / 2), icon, ink);
            var font = Style.Mono;
            float baseline = middle + (font.GetAscent(_fontSize) - font.GetDescent(_fontSize)) / 2;
            DrawString(font, new Vector2(x + icon + 5 * _k, Mathf.Round(baseline)), _name, HorizontalAlignment.Left, -1, _fontSize, ink);
        }

        // A tab's outline, its top corners rounded, shaded from top to foot.
        private void Shape(float top, float w, float h, float r, Color upper, Color lower)
        {
            var points = new System.Collections.Generic.List<Vector2> { new(0, h) };
            for (int i = 0; i <= 6; i++)
            {
                float a = Mathf.Pi + Mathf.Pi / 2 * i / 6;
                points.Add(new Vector2(r + r * Mathf.Cos(a), top + r + r * Mathf.Sin(a)));
            }
            for (int i = 0; i <= 6; i++)
            {
                float a = Mathf.Pi * 1.5f + Mathf.Pi / 2 * i / 6;
                points.Add(new Vector2(w - r + r * Mathf.Cos(a), top + r + r * Mathf.Sin(a)));
            }
            points.Add(new Vector2(w, h));
            var colours = new Color[points.Count];
            for (int i = 0; i < points.Count; i++) colours[i] = upper.Lerp(lower, (points[i].Y - top) / (h - top));
            DrawPolygon([.. points], colours);
        }

        // A helmet's dome over its rim, its visor's arc inside: the gear.
        private void Helmet(Vector2 at, float side, Color ink)
        {
            float u = side / 24, w = 2 * u;
            var c = at + new Vector2(12, 13) * u;
            DrawArc(c, 7 * u, Mathf.Pi, Mathf.Tau, 16, ink, w, true);
            DrawPolyline([at + new Vector2(5, 13) * u, at + new Vector2(5, 16) * u, at + new Vector2(19, 16) * u, at + new Vector2(19, 13) * u], ink, w, true);
            DrawArc(c, 3 * u, Mathf.Pi, Mathf.Tau, 10, ink, w, true);
        }

        // A coat hanger, the old appearance key's glyph: its hook, neck and shoulders down to its bar.
        private void Hanger(Vector2 at, float side, Color ink)
        {
            float u = side / 14, w = 1.4f * u;
            at += new Vector2(0, 2 * u);
            DrawArc(at + new Vector2(7, 1.6f) * u, 1.5f * u, Mathf.Pi, Mathf.Tau * 1.15f, 10, ink, w, true);
            DrawLine(at + new Vector2(8.4f, 2.2f) * u, at + new Vector2(7, 3.6f) * u, ink, w, true);
            DrawPolyline([at + new Vector2(0.8f, 9) * u, at + new Vector2(7, 3.6f) * u, at + new Vector2(13.2f, 9) * u, at + new Vector2(0.8f, 9) * u], ink, w, true);
        }
    }
}
