using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Heat shimmer (0075): what is drawn under a box of the world, sampled from the screen again with each row
/// nudged sideways by a wobble rising through it, faded out toward its edges, so the air over a fire or a
/// hot weld seems to boil. One screen copy a frame for all of them, Godot's own on the first that reads it.
/// Set <see cref="Amplitude"/> each frame; at 0 it is hidden and costs nothing.
/// </summary>
public partial class Shimmer : Node2D
{
    private static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform sampler2D screen : hint_screen_texture, filter_linear, repeat_disable;
            // How far a row wobbles at most, in pixels of the world, and how fast the wobble rises, pixels a second.
            uniform float amplitude = 0.5;
            uniform float rise = 10.0;
            varying vec2 world;
            varying float scale;

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
                // Screen pixels a pixel of the world.
                scale = length((CANVAS_MATRIX * MODEL_MATRIX * vec4(1.0, 0.0, 0.0, 0.0)).xy);
            }

            float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
            float noise(vec2 p) {
                vec2 i = floor(p), f = fract(p), u = f * f * (3.0 - 2.0 * f);
                return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
            }

            void fragment() {
                float t = TIME;
                float wy = world.y + t * rise;
                float across = sin(3.14159265 * UV.y);
                float dx = amplitude * across * (0.55 * sin(wy * 1.1 + t * 5.0) + 0.3 * sin(wy * 2.7 - t * 8.0) + 0.6 * (noise(vec2(wy * 0.5, t * 2.0)) - 0.5));
                float r = length(UV * 2.0 - 1.0);
                float mask = r < 0.55 ? mix(1.0, 0.85, r / 0.55) : 0.85 * (1.0 - smoothstep(0.55, 1.0, r));
                vec3 seen = texture(screen, SCREEN_UV + vec2(dx * scale * SCREEN_PIXEL_SIZE.x, 0.0)).rgb;
                COLOR = vec4(seen, mask);
            }
            """,
    };

    private static readonly ImageTexture White = ImageTexture.CreateFromImage(Image.CreateEmpty(4, 4, false, Image.Format.Rgba8));
    private readonly ShaderMaterial _material = new() { Shader = Shader };
    private Rect2 _box;

    public Shimmer()
    {
        Material = _material;
        Visible = false;
    }

    /// <summary>The box of the world that shimmers, in pixels.</summary>
    public Rect2 Box
    {
        get => _box;
        set
        {
            if (_box == value) return;
            _box = value;
            QueueRedraw();
        }
    }

    /// <summary>How far its rows wobble at most, in pixels of the world, times <see cref="Fx.Shimmer"/>; hidden at nothing.</summary>
    public float Amplitude
    {
        set
        {
            float a = value * (float)Fx.Shimmer;
            Visible = a > 0.02f;
            if (Visible) _material.SetShaderParameter("amplitude", a);
        }
    }

    /// <summary>How fast the wobble climbs through it, pixels a second.</summary>
    public float Rise
    {
        set => _material.SetShaderParameter("rise", value);
    }

    public override void _Draw() => DrawTextureRect(White, _box, false);
}
