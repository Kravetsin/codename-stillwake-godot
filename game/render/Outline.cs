using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A thin line round the silhouette of what is aimed at: the pictures added are drawn together as
/// masks, each in its colour, and only the ring of screen pixels just outside them shows, a pixel wide
/// at 1080 lines whatever the zoom, so a machine, a door or a piece of debris is outlined along its own
/// shape, not boxed. Unlit, so it reads in the dark. Hidden with nothing added, when it costs nothing.
/// </summary>
public partial class Outline : CanvasGroup
{
    /// <summary>The lines of the screen a pixel of the line's width stands for: 1 pixel at 1080 lines, 2 at 2160.</summary>
    public const int LinesPerPixel = 1080;

    /// <summary>How wide the line is, in screen pixels, on a screen <paramref name="lines"/> high: never under one.</summary>
    public static int Width(float lines) => Mathf.Max(1, Mathf.RoundToInt(lines / LinesPerPixel));

    // The masks are drawn into the group's own buffer; a pixel clear there with one drawn within the
    // line's width round it takes that one's colour.
    private static readonly ShaderMaterial Ring = new()
    {
        Shader = new Shader
        {
            Code = $$"""
                shader_type canvas_item;
                render_mode unshaded;
                uniform sampler2D masks : hint_screen_texture, repeat_disable, filter_nearest;

                void fragment() {
                    vec4 here = textureLod(masks, SCREEN_UV, 0.0);
                    int width = int(max(1.0, round(1.0 / (SCREEN_PIXEL_SIZE.y * {{LinesPerPixel}}.0))));
                    vec4 near = vec4(0.0);
                    for (int i = 1; i <= width; i++)
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dy = -1; dy <= 1; dy++) {
                                vec4 s = textureLod(masks, SCREEN_UV + vec2(float(dx), float(dy)) * float(i) * SCREEN_PIXEL_SIZE, 0.0);
                                if (s.a > near.a) near = s;
                            }
                    // Drawn on a clear buffer, a mask's colour is there times its alpha.
                    COLOR = here.a > 0.001 || near.a < 0.001 ? vec4(0.0) : vec4(near.rgb / near.a, near.a);
                }
                """,
        },
    };

    // A mask: the picture's shape, solid in the colour it is tinted.
    private static readonly ShaderMaterial Flat = new()
    {
        Shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                render_mode unshaded;
                varying vec4 tint;

                void vertex() {
                    tint = COLOR;
                }

                void fragment() {
                    COLOR = vec4(tint.rgb, tint.a * step(0.5, texture(TEXTURE, UV).a));
                }
                """,
        },
    };

    private static ImageTexture? _white;

    /// <summary>A white pixel: a box's mask, stretched.</summary>
    private static ImageTexture White
    {
        get
        {
            if (_white != null) return _white;
            var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            image.Fill(Colors.White);
            return _white = ImageTexture.CreateFromImage(image);
        }
    }

    private readonly List<Sprite2D> _masks = [];
    private int _used;

    public Outline(int z)
    {
        ZIndex = z;
        Material = Ring;
        Visible = false;
    }

    /// <summary>Starts an outline: what was added before is let go.</summary>
    public void Begin() => _used = 0;

    /// <summary>
    /// <paramref name="picture"/>, or the piece <paramref name="cut"/> of it, laid from its top-left at
    /// <paramref name="corner"/>, in pixels of the world, outlined in <paramref name="colour"/>.
    /// </summary>
    public void Add(Texture2D picture, Vector2 corner, Color colour, Rect2? cut = null)
    {
        var mask = Take(picture, colour);
        mask.Centered = false;
        mask.Position = corner;
        mask.RegionEnabled = cut != null;
        if (cut is { } c) mask.RegionRect = c;
    }

    /// <summary><paramref name="picture"/> centred on <paramref name="centre"/>, turned and scaled as it is drawn there.</summary>
    public void Add(Texture2D picture, Vector2 centre, float rotation, float scale, Color colour)
    {
        var mask = Take(picture, colour);
        mask.Centered = true;
        mask.Position = centre;
        mask.Rotation = rotation;
        mask.Scale = new Vector2(scale, scale);
        mask.RegionEnabled = false;
    }

    /// <summary>The box <paramref name="box"/>, in pixels of the world: for what has no picture of its own to outline.</summary>
    public void AddBox(Rect2 box, Color colour)
    {
        var mask = Take(White, colour);
        mask.Centered = false;
        mask.Position = box.Position;
        mask.Scale = box.Size;
        mask.RegionEnabled = false;
    }

    /// <summary>Ends the outline: the masks left over are hidden, and the whole with nothing added.</summary>
    public void End()
    {
        for (int i = _used; i < _masks.Count; i++) _masks[i].Visible = false;
        Visible = _used > 0;
    }

    /// <summary>The next mask, kept between outlines: an outline redrawn every frame makes no nodes.</summary>
    private Sprite2D Take(Texture2D picture, Color colour)
    {
        if (_used == _masks.Count)
        {
            var made = new Sprite2D { Material = Flat };
            _masks.Add(made);
            AddChild(made);
        }
        var mask = _masks[_used++];
        mask.Texture = picture;
        mask.Modulate = colour;
        mask.Rotation = 0;
        mask.Scale = Vector2.One;
        mask.Visible = true;
        return mask;
    }
}
