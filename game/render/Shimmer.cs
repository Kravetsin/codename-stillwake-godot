using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Heat shimmer (0075): what is drawn under a box of the world, sampled from the screen again with each row
/// nudged sideways by a wobble rising through it, faded out toward its edges, so the air over a fire or a
/// hot weld seems to boil. Every shimmer is drawn at <see cref="Layers.Shimmer"/>, after the world and before
/// anything drawn over it, from one copy of the screen taken there (<see cref="ScreenCopy"/>): it bends the world,
/// never an outline, a plan or a mark, and shows only what was drawn (alpha), so it never darkens.
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
                // Only what was drawn there is shown again: a texel with nothing in it (alpha 0) adds nothing, so a
                // shimmer can bend the scene but never darken it.
                vec4 seen = texture(screen, SCREEN_UV + vec2(dx * scale * SCREEN_PIXEL_SIZE.x, 0.0));
                COLOR = vec4(seen.rgb, mask * seen.a);
            }
            """,
    };

    private static readonly ImageTexture White = ImageTexture.CreateFromImage(Image.CreateEmpty(4, 4, false, Image.Format.Rgba8));
    private readonly ShaderMaterial _material = new() { Shader = Shader };
    /// <summary>
    /// Whether heat shimmers at all, the player's setting (on by default, <c>Settings.Shimmer</c>): off, every
    /// shimmer is hidden and costs nothing.
    /// </summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// The one copy of the screen every shimmer reads, taken at <see cref="Layers.Shimmer"/> before any of them:
    /// the world drawn so far and nothing over it. <c>Main</c> adds it as its first child, so at its layer it
    /// comes before every shimmer; shown only while one is.
    /// </summary>
    public static BackBufferCopy ScreenCopy { get; private set; } = Copy();

    /// <summary>A new copy for a new scene, the last one's gone with it.</summary>
    public static BackBufferCopy NewScreenCopy()
    {
        _shown = 0;
        return ScreenCopy = Copy();
    }

    private static BackBufferCopy Copy() => new()
    {
        CopyMode = BackBufferCopy.CopyModeEnum.Viewport,
        ZIndex = Layers.Shimmer,
        ZAsRelative = false,
        Visible = false,
    };

    private static int _shown;
    private bool _counted;
    private Rect2 _box;

    public Shimmer()
    {
        Material = _material;
        // At the shimmer layer whatever its parent's: after the world, before the plans, the aim's outline and
        // every mark over it, which a shimmer drawn later would copy and wobble.
        ZIndex = Layers.Shimmer;
        ZAsRelative = false;
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

    /// <summary>How far its rows wobble at most, in pixels of the world, times <see cref="Fx.Shimmer"/>; hidden at nothing, off the screen or switched off.</summary>
    public float Amplitude
    {
        set
        {
            float a = value * (float)Fx.Shimmer;
            Show(Enabled && a > 0.02f && OnScreen());
            if (Visible) _material.SetShaderParameter("amplitude", a);
        }
    }

    /// <summary>Whether its box shows on the screen: off it, it is hidden, and a screen with none shown takes no copy.</summary>
    private bool OnScreen() =>
        !IsInsideTree() || GetViewportRect().Intersects(GetGlobalTransformWithCanvas() * _box);

    /// <summary>How fast the wobble climbs through it, pixels a second.</summary>
    public float Rise
    {
        set => _material.SetShaderParameter("rise", value);
    }

    private void Show(bool shown)
    {
        Visible = shown;
        if (shown == _counted) return;
        _counted = shown;
        _shown = System.Math.Max(0, _shown + (shown ? 1 : -1));
        if (IsInstanceValid(ScreenCopy)) ScreenCopy.Visible = _shown > 0;
    }

    public override void _ExitTree() => Show(false);

    public override void _Draw() => DrawTextureRect(White, _box, false);
}
