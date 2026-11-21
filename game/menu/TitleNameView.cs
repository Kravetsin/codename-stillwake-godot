using Godot;
using Ink = Stillwake.Game.Menu.TitleNameInk;

namespace Stillwake.Game.Menu;

/// <summary>
/// The game's name on the title (<see cref="TitleName"/>): the exported word, its pixels leaving by its map in a shader, and
/// the dust drawn over it while the word is apart. Its box is the letters' cap height from their left ink edge, so it sits in the
/// menu's column like the label it replaced; the pictures' margin spills outside. The loop starts when it is made.
/// </summary>
public partial class TitleNameView : Control
{
    private const float Margin = 40;
    private const string Leave = """
        shader_type canvas_item;
        uniform sampler2D leaving : filter_linear;
        uniform float clock;
        uniform float te_lo;
        uniform float te_hi;
        uniform float soft;
        void fragment() {
            float te = mix(te_lo, te_hi, texture(leaving, UV).r);
            COLOR.a *= clamp((te - clock) / soft, 0.0, 1.0);
        }
        """;

    private readonly TextureRect _word;
    private readonly ShaderMaterial _leave;
    private readonly Control _dust;
    private readonly Color _cold = Rgb(Ink.Dust), _warm = Rgb(Ink.Warm);
    private double _t;

    public TitleNameView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2((float)Ink.BoxW - 2 * Margin, (float)Ink.BoxH - 2 * Margin);
        _leave = new ShaderMaterial { Shader = new Shader { Code = Leave } };
        _leave.SetShaderParameter("leaving", GD.Load<Texture2D>("res://art/hd/menu/title_name_leaving.png"));
        _leave.SetShaderParameter("te_lo", Ink.TeLo);
        _leave.SetShaderParameter("te_hi", Ink.TeHi);
        _leave.SetShaderParameter("soft", Ink.Soft);
        AddChild(_word = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://art/hd/menu/title_name.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Position = new Vector2(-Margin, -Margin),
            Size = new Vector2((float)Ink.BoxW, (float)Ink.BoxH),
            MouseFilter = MouseFilterEnum.Ignore,
            Material = _leave,
        });
        AddChild(_dust = new Control { MouseFilter = MouseFilterEnum.Ignore });
        _dust.Draw += DrawDust;
    }

    public override void _Process(double delta)
    {
        bool wasWhole = TitleName.Whole(_t);
        _t = (_t + delta) % Ink.Cycle;
        _leave.SetShaderParameter("clock", TitleName.Clock(_t));
        if (!wasWhole || !TitleName.Whole(_t)) _dust.QueueRedraw();
    }

    private void DrawDust()
    {
        if (TitleName.Whole(_t)) return;
        var origin = new Vector2((float)Ink.BoxX + Margin, (float)Ink.BoxY + Margin);
        foreach (var g in TitleName.Grains)
        {
            if (TitleName.At(g, _t) is not { } p || p.Alpha < 0.004) continue;
            var (x, y) = ((float)p.X, (float)p.Y);
            var c = _cold.Lerp(_warm, (float)TitleName.Warmth(p.X, p.Y, g.Glint)) with { A = (float)p.Alpha };
            float h = (float)g.Half;
            _dust.DrawRect(new Rect2(new Vector2(x - h, y - h) - origin, new Vector2(2 * h, 2 * h)), c);
        }
    }

    private static Color Rgb(int[] c) => new(c[0] / 255f, c[1] / 255f, c[2] / 255f);
}
