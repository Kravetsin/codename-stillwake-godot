using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Backdrop;

/// <summary>
/// A planet's sky and the land behind its ground out of focus (<see cref="PlanetLooks.Look.Far"/>), the play plane sharp in front of
/// them: on the stars' layer after all they draw, so Main makes it right after the stars. Hidden while sharp, aboard and in space, so
/// no screen is copied for it.
/// </summary>
public partial class FocusView : CanvasLayer
{
    private static readonly Shader Shader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap, repeat_disable;
            uniform float far = 0.0;
            void fragment() {
                // far: mip levels at 1080 rows, alike at every zoom and screen size, as the storm's wash; nine taps round a mip
                // level hide its blocks.
                float lod = clamp(log2(1.0 / SCREEN_PIXEL_SIZE.y / 1080.0) + far, 0.0, 4.0);
                vec2 p = SCREEN_PIXEL_SIZE * exp2(lod);
                vec3 c = textureLod(screen, SCREEN_UV, lod).rgb * 0.36;
                c += (textureLod(screen, SCREEN_UV + vec2(p.x, 0.0), lod).rgb + textureLod(screen, SCREEN_UV - vec2(p.x, 0.0), lod).rgb
                    + textureLod(screen, SCREEN_UV + vec2(0.0, p.y), lod).rgb + textureLod(screen, SCREEN_UV - vec2(0.0, p.y), lod).rgb) * 0.12;
                c += (textureLod(screen, SCREEN_UV + p, lod).rgb + textureLod(screen, SCREEN_UV - p, lod).rgb
                    + textureLod(screen, SCREEN_UV + vec2(p.x, -p.y), lod).rgb + textureLod(screen, SCREEN_UV + vec2(-p.x, p.y), lod).rgb) * 0.04;
                COLOR = vec4(c, 1.0);
            }
            """,
    };

    private readonly ShaderMaterial _material = new() { Shader = Shader };
    private readonly Func<float> _far;
    private float _set = -1;

    /// <param name="far">How out of focus all behind is now, 0 sharp: read each frame.</param>
    public FocusView(Func<float> far)
    {
        _far = far;
        Layer = ScreenLayers.Stars;
        var rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = _material };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport });
        AddChild(rect);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        float far = _far();
        Visible = far >= 0.01f;
        if (Visible && far != _set) _material.SetShaderParameter("far", _set = far);
    }
}
