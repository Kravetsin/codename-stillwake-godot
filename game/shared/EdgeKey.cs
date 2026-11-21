using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A key on the tablet's edge (<see cref="TabletEdge"/>) in the thread icons' look, the pick «Одна нить»: a dark ember glass, its rim and
/// its motif each one ember thread (<see cref="ThreadIcon"/>), dim unless the pointer nears the screen's foot. Under the pointer a spark
/// runs the motif; held it stands and the key sinks a pixel; while what it switches is on a spark walks it slowly on a warmer glass;
/// while it tells of something waiting it pulses. A number it carries stands on a plate at its foot, counted to and flared
/// (<see cref="CountFlare"/>).
/// </summary>
public partial class EdgeKey : Control
{
    /// <summary>The screen's side, and how far below its foot the number's baseline lies.</summary>
    public const int Side = TabletPeek.KeySide, NumberDown = 5;
    /// <summary>The motif's half side, px; the rim's and the motif's threads' widths.</summary>
    private const float MotifHalf = 9, RimThread = 0.7f, MotifThread = 1;
    private static readonly Color Flared = new(1, 250 / 255f, 235 / 255f);
    private static readonly Font NumberFont = Style.Mono;
    private const int NumberSize = 13;

    private readonly string _name;
    private readonly IconLines _icon;
    private readonly Action _pressed;
    private readonly Func<bool> _on, _amber;
    private readonly Func<int?>? _number;
    private readonly CountFlare _count = new();
    private readonly Node2D _light = new() { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add } };
    private readonly Over _over;
    private readonly EmberMarks _marks = new();
    private readonly EmberArt _art = new();
    private readonly float _seed;
    private bool _down, _hover, _counted;
    private double _t;
    private (float, bool, bool, bool, int, double, bool) _drawn;
    private readonly StyleBoxFlat _box = new() { AntiAliasing = true };

    /// <param name="name">Its motif, one of <see cref="EdgeRow.Keys"/>: its thread icon (<see cref="ThreadIcon.Named"/>).</param>
    /// <param name="pressed">What a press does.</param>
    /// <param name="on">Whether what it switches is on.</param>
    /// <param name="amber">Whether it tells of something waiting.</param>
    /// <param name="number">The number it carries, null for none.</param>
    public EdgeKey(string name, Action pressed, Func<bool>? on = null, Func<bool>? amber = null, Func<int?>? number = null)
    {
        (_name, _pressed, _on, _amber, _number) = (name, pressed, on ?? (() => false), amber ?? (() => false), number);
        _icon = ThreadIcon.Named(name);
        _seed = Array.IndexOf(EdgeRow.Keys, name) * 2.3f + 1;
        Size = CustomMinimumSize = new Vector2(Side, Side);
        MouseFilter = MouseFilterEnum.Stop;
        _over = new Over(this);
        _box.SetCornerRadiusAll(3);
        AddChild(_light);
        AddChild(_over);
        _light.Draw += () => _art.Light(_marks, _light);
        MouseEntered += () => _hover = true;
        MouseExited += () => (_hover, _down) = (false, false);
        GuiInput += e =>
        {
            if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button) return;
            AcceptEvent();
            _down = button.Pressed;
            if (button.Pressed) _pressed();
        };
    }

    /// <summary>Its motif, one of <see cref="EdgeRow.Keys"/>.</summary>
    public string Motif => _name;

    /// <summary>How near the pointer is to the screen's foot, 0 to 1, from the edge: it brightens the key.</summary>
    public float Near { private get; set; }

    /// <summary>How much of it shows as it flickers in or out, 0 to 1, from the edge.</summary>
    public float Shows { private get; set; } = 1;

    public override void _Process(double delta)
    {
        _count.Step(delta);
        // A number not shown reads as 0, so it counts up from there when it comes back.
        int? n = _number?.Invoke();
        _count.Read(n ?? 0);
        _counted = n != null;
        _t += delta;
        var look = Look;
        // Drawn each frame only while a spark runs or it pulses: still, only when what it shows changes.
        bool moving = look.Waiting || !look.Held && (look.Hover > 0.01f || look.On);
        var drawn = (look.Level, look.On, look.Held, look.Waiting, _count.Shown, moving ? _t : 0, _counted);
        if (drawn == _drawn) return;
        _drawn = drawn;
        _marks.Clear();
        float cx = Side / 2f, cy = Side / 2f + (look.Held ? 1 : 0), b = look.Level;
        if (b > 0.01f)
        {
            ThreadIcon.Draw(_marks, IconInk.Square, look with { Level = b * 0.5f, Rim = true, Waiting = false }, cx, cy, Side / 2f - 0.5f, RimThread);
            ThreadIcon.Draw(_marks, _icon, look, cx, cy, MotifHalf, MotifThread);
        }
        QueueRedraw();
        _light.QueueRedraw();
        _over.QueueRedraw();
    }

    /// <summary>How it stands now: dim at rest, bright near the foot, brighter under the pointer, dimmer held, up with a count's flare.</summary>
    private IconLook Look
    {
        get
        {
            float glow = 0.4f + 0.6f * Near;
            float b = (_hover ? Mathf.Min(1.1f, glow + 0.25f) : glow * 0.85f) * (_down ? 0.7f : 1) + (float)(_counted ? _count.Heat : 0) * 0.6f;
            return new IconLook(b * Shows, _hover ? 1 : 0, _on(), _down, _amber(), Time: (float)_t, Seed: _seed);
        }
    }

    /// <summary>The screen's square, sunk a pixel while held.</summary>
    private Rect2 Screen => new(0, _down ? 1 : 0, Side, Side);

    public override void _Draw()
    {
        float b = _drawn.Item1;
        if (b <= 0.01f) return;
        var deep = EmberArt.Ember;
        _box.BgColor = _on() ? new Color(deep.R * 0.3f, deep.G * 0.3f, deep.B * 0.3f, 0.7f) : new Color(deep.R * 0.1f, deep.G * 0.1f, deep.B * 0.1f, Mathf.Min(0.75f, 0.25f + 0.5f * b));
        DrawStyleBox(_box, Screen.Grow(-0.5f));
    }

    /// <summary>What lies over the light: the number on its plate.</summary>
    private sealed partial class Over(EdgeKey key) : Node2D
    {
        public override void _Draw()
        {
            float b = key._drawn.Item1;
            if (b <= 0.01f || !key._counted) return;
            var s = key.Screen;
            string text = key._count.Shown.ToString();
            float wide = NumberFont.GetStringSize(text, HorizontalAlignment.Left, -1, NumberSize).X + 6, mid = Side / 2f, baseline = s.End.Y + NumberDown;
            var plate = new Rect2(mid - wide / 2, baseline - 12, wide, 14);
            DrawRect(plate, new Color(4 / 255f, 6 / 255f, 9 / 255f, 0.85f));
            DrawRect(plate.Grow(-0.5f), EmberArt.Ember with { A = 0.55f * b }, false, 1);
            var ink = EmberArt.Of(0.45f, Mathf.Min(1, b)).Lerp(Flared, (float)key._count.Heat * 0.6f);
            DrawString(NumberFont, new Vector2(plate.Position.X + 3, baseline), text, HorizontalAlignment.Left, -1, NumberSize, ink);
        }
    }
}
