using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A key on the tablet's edge (<see cref="TabletEdge"/>) as a small green phosphor screen floating over it (hologram-keys): its motif glowing on
/// dark glass, grain and a rolling line through it, dim unless the pointer nears the screen's foot. Brighter under the pointer, sunk a pixel and
/// dimmer while held; while what it switches is on the glass lights and the motif shows dark on it; amber while it tells of something waiting.
/// A number it carries stands on a plate at its foot, counted to and flared (<see cref="CountFlare"/>).
/// </summary>
public partial class EdgeKey : Control
{
    /// <summary>The screen's side, and how far below its foot the number's baseline lies.</summary>
    public const int Side = 28, NumberDown = 5;
    private const int Inset = 5, Icon = Side - 2 * Inset, GlowPad = 6;
    private static readonly Color Green = Rgb(124, 240, 150), Amber = Rgb(255, 168, 60), Flared = Rgb(255, 250, 235);
    private static readonly Font NumberFont = Style.Mono;
    private const int NumberSize = 15;
    private static Material? _light;

    private readonly string _name;
    private readonly Action _pressed;
    private readonly Func<bool> _on, _amber;
    private readonly Func<int?>? _number;
    private readonly KeyGlow _amberGlow = new();
    private readonly CountFlare _count = new();
    private readonly Light _lit;
    private readonly Over _over;
    private readonly float _seed;
    private bool _down, _hover, _counted;
    private double _t;

    /// <param name="name">Its motif's pictures: <c>holo_&lt;name&gt;</c> and its glow, drawn by <c>tools/anime-art/subjects/edgekeys.js</c>.</param>
    /// <param name="pressed">What a press does.</param>
    /// <param name="on">Whether what it switches is on.</param>
    /// <param name="amber">Whether it tells of something waiting.</param>
    /// <param name="number">The number it carries, null for none.</param>
    public EdgeKey(string name, Action pressed, Func<bool>? on = null, Func<bool>? amber = null, Func<int?>? number = null)
    {
        (_name, _pressed, _on, _amber, _number) = (name, pressed, on ?? (() => false), amber ?? (() => false), number);
        _seed = (float)(name.Length * 0.73 + name[0] * 0.011);
        Size = CustomMinimumSize = new Vector2(Side, Side);
        MouseFilter = MouseFilterEnum.Stop;
        _lit = new Light(this) { Material = _light ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://shared/holo.gdshader") } };
        _over = new Over(this);
        AddChild(_lit);
        AddChild(_over);
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

    /// <summary>How near the pointer is to the screen's foot, 0 to 1, from the edge: it brightens the key.</summary>
    public float Near { private get; set; }

    /// <summary>How much of it shows as it flickers in or out, 0 to 1, from the edge.</summary>
    public float Shows { private get; set; } = 1;

    public override void _Process(double delta)
    {
        _t += delta;
        _amberGlow.Step(delta, _amber());
        _count.Step(delta);
        // A number not shown reads as 0, so it counts up from there when it comes back.
        int? n = _number?.Invoke();
        _count.Read(n ?? 0);
        _counted = n != null;
        QueueRedraw();
        _lit.QueueRedraw();
        _over.QueueRedraw();
    }

    /// <summary>How bright it is: dim at rest, bright near the foot, brighter under the pointer, dimmer held, up with a count's flare; and its flicker.</summary>
    private float Level
    {
        get
        {
            float glow = 0.4f + 0.6f * Near, flick = 0.9f + 0.1f * Noise(Mathf.Floor((float)_t * 30) * 1.3f + _seed * 7);
            float b = (_hover ? Mathf.Min(1.1f, glow + 0.25f) : glow * 0.85f) * (_down ? 0.7f : 1) + (float)(_counted ? _count.Heat : 0) * 0.6f;
            return b * Shows * flick;
        }
    }

    private Color Colour => Green.Lerp(Amber, (float)_amberGlow.Lit);

    /// <summary>The screen's square, sunk a pixel while held.</summary>
    private Rect2 Screen => new(0, _down ? 1 : 0, Side, Side);

    public override void _Draw()
    {
        float b = Level;
        if (b <= 0.01f) return;
        var col = Colour;
        bool on = _on();
        var box = new StyleBoxFlat
        {
            BgColor = on ? col with { A = 0.62f * b } : (col * 0.1f) with { A = Mathf.Min(0.75f, 0.25f + 0.5f * b) },
            BorderColor = col with { A = 0.5f * b },
            AntiAliasing = true,
        };
        box.SetCornerRadiusAll(3);
        box.SetBorderWidthAll(1);
        DrawStyleBox(box, Screen.Grow(-0.5f));
    }

    /// <summary>What it gives off: the motif and its glow, the rolling line and the grain's bright grains, added over the glass.</summary>
    private sealed partial class Light(EdgeKey key) : Node2D
    {
        public override void _Draw()
        {
            float b = key.Level;
            if (b <= 0.01f) return;
            var col = key.Colour;
            var s = key.Screen;
            if (!key._on())
            {
                var at = s.Position + new Vector2(Inset, Inset);
                DrawTextureRect(PixelArt.Picture($"holo_{key._name}_glow"), new Rect2(at - new Vector2(GlowPad, GlowPad), new Vector2(Icon + 2 * GlowPad, Icon + 2 * GlowPad)), false, col with { A = 0.9f * b });
                DrawTextureRect(PixelArt.Picture($"holo_{key._name}"), new Rect2(at, new Vector2(Icon, Icon)), false, col with { A = b });
            }
            float roll = (((float)key._t + key._seed) / 1.7f) % 1;
            DrawRect(new Rect2(s.Position.X + 2, s.Position.Y + 2 + roll * (Side - 5), Side - 4, 1.5f), col with { A = 0.25f * b });
            int frame = Mathf.FloorToInt((float)key._t * 24);
            for (int i = 1; i < 30; i += 2) DrawRect(new Rect2(Grain(s, frame, i), Vector2.One), col with { A = 0.35f * b });
        }
    }

    /// <summary>What lies over the light: the grain's dark grains, the motif dark on lit glass, and the number on its plate.</summary>
    private sealed partial class Over(EdgeKey key) : Node2D
    {
        public override void _Draw()
        {
            float b = key.Level;
            if (b <= 0.01f) return;
            var col = key.Colour;
            var s = key.Screen;
            var dark = col * 0.1f;
            int frame = Mathf.FloorToInt((float)key._t * 24);
            for (int i = 0; i < 30; i += 2) DrawRect(new Rect2(Grain(s, frame, i), Vector2.One), dark with { A = 0.6f * Mathf.Min(1, b * 1.5f) });
            if (key._on()) DrawTextureRect(PixelArt.Picture($"holo_{key._name}"), new Rect2(s.Position + new Vector2(Inset, Inset), new Vector2(Icon, Icon)), false, dark with { A = 0.95f * key.Shows });
            if (!key._counted) return;
            string text = key._count.Shown.ToString();
            var face = col;
            float wide = NumberFont.GetStringSize(text, HorizontalAlignment.Left, -1, NumberSize).X + 6, mid = Side / 2f, baseline = s.End.Y + NumberDown;
            var plate = new Rect2(mid - wide / 2, baseline - 12, wide, 14);
            DrawRect(plate, new Color(3 / 255f, 10 / 255f, 5 / 255f, 0.85f * Mathf.Clamp(b * 1.5f, 0, 1)));
            DrawRect(plate.Grow(-0.5f), face with { A = 0.55f * b }, false, 1);
            var ink = face.Lerp(Flared, (float)key._count.Heat * 0.6f) with { A = Mathf.Min(1, b * 1.3f) };
            DrawString(NumberFont, new Vector2(plate.Position.X + 3, baseline), text, HorizontalAlignment.Left, -1, NumberSize, ink);
        }
    }

    /// <summary>Where grain <paramref name="i"/> lies this frame: anywhere inside the glass but its rim.</summary>
    private static Vector2 Grain(Rect2 s, int frame, int i) => new(
        Mathf.Floor(s.Position.X + 2 + Noise(frame * 17.3f + i * 3.1f) * (Side - 4)),
        Mathf.Floor(s.Position.Y + 2 + Noise(frame * 11.9f + i * 5.7f) * (Side - 4)));

    private static float Noise(float i)
    {
        float s = Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    private static Color Rgb(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);
}
