using Godot;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The hack's marks over the tablet's screen, placed by hand as picked ([market-decrypt](../../../docs/decisions/2026-11-02-market-decrypt.md)): a dim column the poison bled into, slices of
/// the picture slipped, pixels dead and stuck. At another window's size their places scale, their sizes do not.
/// </summary>
public partial class MarketFaults : Control
{
    /// <summary>The screen they were placed on.</summary>
    private static readonly Vector2 Placed = new(1204, 644);

    /// <summary>A slice of what is under it, drawn <c>By</c> pixels along.</summary>
    private static readonly (float X, float Wide, float Y, float Tall, float By)[] Slips = [(270, 926, 189, 1, 3), (8, 1188, 365, 1, -2), (570, 400, 596, 2, 4)];
    private static readonly Vector2[] Dead = [new(582, 97), new(1160, 259), new(258, 476), new(910, 423)];
    private static readonly Vector2[] Stuck = [new(442, 234), new(1074, 52), new(930, 628)];
    private const float ColumnX = 957, ColumnFrom = 40, ColumnTo = 636;

    private static readonly Shader Slipped = new()
    {
        Code = """
            shader_type canvas_item;
            uniform sampler2D screen : hint_screen_texture, filter_nearest;
            uniform float by = 0.0;
            void fragment() {
                COLOR = vec4(texture(screen, SCREEN_UV - vec2(by * SCREEN_PIXEL_SIZE.x, 0.0)).rgb, COLOR.a);
            }
            """,
    };

    private readonly ColorRect[] _slips = new ColorRect[Slips.Length];

    public MarketFaults()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        for (int i = 0; i < Slips.Length; i++)
        {
            var material = new ShaderMaterial { Shader = Slipped };
            material.SetShaderParameter("by", Slips[i].By);
            AddChild(_slips[i] = new ColorRect { MouseFilter = MouseFilterEnum.Ignore, Material = material });
        }
        Resized += Place;
    }

    private void Place()
    {
        var scale = Size / Placed;
        for (int i = 0; i < Slips.Length; i++)
        {
            var (x, wide, y, tall, _) = Slips[i];
            _slips[i].Position = new Vector2(x, y) * scale;
            _slips[i].Size = new Vector2(wide * scale.X, tall);
        }
    }

    public override void _Draw()
    {
        var scale = Size / Placed;
        DrawRect(new Rect2(ColumnX * scale.X, ColumnFrom * scale.Y, 1, (ColumnTo - ColumnFrom) * scale.Y), MarketPage.Poison with { A = 0.16f });
        foreach (var at in Dead) DrawRect(new Rect2((at * scale).Round(), new Vector2(3, 2)), Colors.Black);
        foreach (var at in Stuck) DrawRect(new Rect2((at * scale).Round(), new Vector2(2, 2)), MarketPage.Poison);
    }
}
