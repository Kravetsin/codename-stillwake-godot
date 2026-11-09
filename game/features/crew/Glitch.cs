using System;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A figure drawn whole into its own buffer, then cut into bands across (catcher-combat, «Красная помеха»): each band moved sideways, a
/// red copy of its shape added over it by its own bands; or only that red copy, a ghost. Calm, it draws its figure as it is.
/// </summary>
public partial class Glitch : CanvasGroup
{
    /// <summary>The most bands it is cut into: a ghost's (<see cref="CatcherFx.GhostBands"/>).</summary>
    private const int Most = CatcherFx.GhostBands;

    // Bands counted from Top over the feet; a band's shift, in pixels of the world, made the screen's by how far a pixel of the
    // screen goes in the world. The buffer holds colour times alpha: the figure is laid as it is, the red added where its shape is.
    private static readonly Shader Banded = new()
    {
        Code = $$"""
            shader_type canvas_item;
            render_mode unshaded, blend_premul_alpha;
            uniform sampler2D drawn : hint_screen_texture, repeat_disable, filter_linear;
            uniform vec2 feet;
            uniform int bands = {{CatcherFx.Bands}};
            uniform float shift[{{Most}}];
            uniform float red_shift[{{Most}}];
            uniform float red_alpha[{{Most}}];
            uniform float top;
            uniform float tall;
            uniform float body = 1.0;
            uniform vec3 red = vec3(1.0, 0.204, 0.157);
            varying vec2 at;

            void vertex() {
                at = VERTEX;
            }

            void fragment() {
                float b = floor((at.y - feet.y - top) * float(bands) / tall);
                bool in_band = b >= 0.0 && b < float(bands);
                int i = clamp(int(b), 0, {{Most - 1}});
                float per = SCREEN_PIXEL_SIZE.x / max(abs(dFdx(at.x)), 1e-5);
                float s = in_band ? shift[i] : 0.0;
                vec4 c = textureLod(drawn, SCREEN_UV - vec2(s * per, 0.0), 0.0) * body;
                if (in_band && red_alpha[i] > 0.0) {
                    float a = textureLod(drawn, SCREEN_UV - vec2(red_shift[i] * per, 0.0), 0.0).a;
                    c.rgb += red * a * red_alpha[i];
                }
                COLOR = c;
            }
            """,
    };

    private readonly ShaderMaterial _banded = new() { Shader = Banded };
    private readonly float[] _shift = new float[Most], _redShift = new float[Most], _redAlpha = new float[Most];
    private int _frame = int.MinValue;

    public Glitch()
    {
        // Room for a band moved sideways, the red copy past it.
        FitMargin = 8;
        _banded.SetShaderParameter("top", CatcherFx.Top);
        _banded.SetShaderParameter("tall", CatcherFx.Tall);
    }

    /// <summary>The bands' shifts and its red copy's, to fill for <see cref="Cut"/>.</summary>
    public Span<float> Shift => _shift;

    public Span<float> RedShift => _redShift;
    public Span<float> RedAlpha => _redAlpha;

    /// <summary>
    /// Whether the bands for jitter frame <paramref name="frame"/> are to be filled: once a frame, as the page held each 45 ms.
    /// </summary>
    public bool Needs(int frame) => frame != _frame;

    /// <summary>
    /// Cut it into <paramref name="bands"/> bands over <paramref name="feet"/> (in its parent's pixels) as <see cref="Shift"/>,
    /// <see cref="RedShift"/> and <see cref="RedAlpha"/> hold for <paramref name="frame"/>, its figure drawn <paramref name="body"/> of itself.
    /// </summary>
    public void Cut(Vector2 feet, int bands, int frame, float body)
    {
        Material = _banded;
        _banded.SetShaderParameter("feet", feet - Position);
        _banded.SetShaderParameter("body", body);
        if (frame == _frame) return;
        _frame = frame;
        _banded.SetShaderParameter("bands", bands);
        _banded.SetShaderParameter("shift", _shift);
        _banded.SetShaderParameter("red_shift", _redShift);
        _banded.SetShaderParameter("red_alpha", _redAlpha);
    }

    /// <summary>Drawn whole again, as its figure is.</summary>
    public void Calm()
    {
        Material = null;
        _frame = int.MinValue;
    }
}
