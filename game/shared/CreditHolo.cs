using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The crew's credits as a gold hologram on the right of the edge, clear of its keys (hologram-keys): the coin and the number large on the same phosphor glass, dim with
/// the row unless the pointer nears the screen's foot. Every change counts to the new number and swells the readout and lights it full, so the eye
/// goes to it (<see cref="CountFlare.Pop"/>).
/// </summary>
public partial class CreditHolo : Control
{
    /// <summary>Its height, as a key's; how much larger it swells on a change.</summary>
    public const int Tall = EdgeKey.Side;
    private const float Swell = 0.35f;
    private const int NumberSize = 20, CoinSide = 14, Pad = 6, Gap = 4;
    private static readonly Color Gold = new(240 / 255f, 208 / 255f, 96 / 255f), Flared = new(1, 250 / 255f, 235 / 255f);
    private static readonly Font NumberFont = Style.Mono;

    private readonly Func<int> _credits;
    private readonly CountFlare _count = new();
    private readonly Part _light, _coin, _over;
    private double _t;

    /// <param name="credits">The crew's credits as last read.</param>
    public CreditHolo(Func<int> credits)
    {
        _credits = credits;
        MouseFilter = MouseFilterEnum.Stop;
        var shader = GD.Load<Shader>("res://shared/holo.gdshader");
        var grey = new ShaderMaterial { Shader = shader };
        grey.SetShaderParameter("grey", true);
        AddChild(_light = new Part(DrawLight) { Material = new ShaderMaterial { Shader = shader } });
        AddChild(_coin = new Part(DrawCoin) { Material = grey });
        AddChild(_over = new Part(DrawOver));
        Size = new Vector2(WideFor(0), Tall);
    }

    /// <summary>How near the pointer is to the screen's foot, 0 to 1, from the edge.</summary>
    public float Near { private get; set; }

    /// <summary>How much of it shows as it flickers in or out, 0 to 1, from the edge.</summary>
    public float Shows { private get; set; } = 1;

    private float _grown = 1;

    /// <summary>Lays it ending at <paramref name="right"/>, its top at <paramref name="top"/>, <paramref name="grown"/> times its own size before it swells.</summary>
    public void Lay(float right, float top, float grown)
    {
        _grown = grown;
        // It swells about its middle: placed by where its middle stands at the size laid.
        Position = new Vector2(Mathf.Round(right - Size.X * grown / 2), Mathf.Round(top + Tall * grown / 2)) - Size / 2;
    }

    public override void _Process(double delta)
    {
        _t += delta;
        _count.Step(delta);
        _count.Read(_credits());
        Size = new Vector2(WideFor(_count.Shown), Tall);
        PivotOffset = Size / 2;
        Scale = Vector2.One * _grown * (1 + Swell * (float)Pop);
        QueueRedraw();
        foreach (var part in (Part[])[_light, _coin, _over]) part.QueueRedraw();
    }

    private double Pop => _count.Pop;

    private static float WideFor(int shown) => Pad + CoinSide + Gap + NumberFont.GetStringSize(shown.ToString(), HorizontalAlignment.Left, -1, NumberSize).X + Pad;

    /// <summary>How bright it is: as the keys at rest and near the foot, full while a change swells it; and its flicker.</summary>
    private float Level
    {
        get
        {
            float flick = 0.9f + 0.1f * Noise(Mathf.Floor((float)_t * 30) * 1.3f + 5.1f);
            return Mathf.Max(0.4f + 0.6f * Near, (float)Pop * 1.1f) * Shows * flick;
        }
    }

    private Color Ink => Gold.Lerp(Flared, (float)_count.Heat * 0.6f);

    private float Baseline => Tall / 2f + NumberSize * Style.MonoMiddle;

    public override void _Draw()
    {
        float b = Level;
        if (b <= 0.01f) return;
        var box = new StyleBoxFlat { BgColor = (Gold * 0.1f) with { A = Mathf.Min(0.8f, 0.3f + 0.5f * b) }, BorderColor = Gold with { A = 0.5f * b }, AntiAliasing = true };
        box.SetCornerRadiusAll(3);
        box.SetBorderWidthAll(1);
        DrawStyleBox(box, new Rect2(Vector2.Zero, Size).Grow(-0.5f));
    }

    /// <summary>The number's glow, the rolling line and the bright grains, added over the glass.</summary>
    private void DrawLight(Part c)
    {
        float b = Level;
        if (b <= 0.01f) return;
        string text = _count.Shown.ToString();
        var at = new Vector2(Pad + CoinSide + Gap, Baseline);
        c.DrawStringOutline(NumberFont, at, text, HorizontalAlignment.Left, -1, NumberSize, 5, Ink with { A = 0.22f * b });
        float roll = ((float)_t / 1.9f) % 1;
        c.DrawRect(new Rect2(2, 2 + roll * (Tall - 5), Size.X - 4, 1.5f), Gold with { A = 0.25f * b });
        int frame = Mathf.FloorToInt((float)_t * 24);
        for (int i = 1; i < 40; i += 2) c.DrawRect(new Rect2(Grain(frame, i), Vector2.One), Gold with { A = 0.35f * b });
    }

    /// <summary>The credit's chip, in the hologram's gold.</summary>
    private void DrawCoin(Part c)
    {
        float b = Level;
        if (b > 0.01f) c.DrawTextureRect(Signs.Credit(), new Rect2(Pad, (Tall - CoinSide) / 2f, CoinSide, CoinSide), false, Ink with { A = b });
    }

    /// <summary>The dark grains, and the number itself.</summary>
    private void DrawOver(Part c)
    {
        float b = Level;
        if (b <= 0.01f) return;
        int frame = Mathf.FloorToInt((float)_t * 24);
        for (int i = 0; i < 40; i += 2) c.DrawRect(new Rect2(Grain(frame, i), Vector2.One), (Gold * 0.1f) with { A = 0.6f * Mathf.Min(1, b * 1.5f) });
        c.DrawString(NumberFont, new Vector2(Pad + CoinSide + Gap, Baseline), _count.Shown.ToString(), HorizontalAlignment.Left, -1, NumberSize, Ink with { A = Mathf.Min(1, b * 1.3f) });
    }

    /// <summary>Where grain <paramref name="i"/> lies this frame: anywhere inside the glass but its rim.</summary>
    private Vector2 Grain(int frame, int i) => new(
        Mathf.Floor(2 + Noise(frame * 17.3f + i * 3.1f) * (Size.X - 4)),
        Mathf.Floor(2 + Noise(frame * 11.9f + i * 5.7f) * (Tall - 4)));

    private static float Noise(float i)
    {
        float s = Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    /// <summary>A layer of it, drawn through its own material.</summary>
    private sealed partial class Part(Action<Part> paint) : Node2D
    {
        public override void _Draw() => paint(this);
    }
}
