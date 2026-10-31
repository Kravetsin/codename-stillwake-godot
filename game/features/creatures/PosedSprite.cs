using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A creature drawn as one sprite posed in code: its picture lit as the world, its glow over it unlit, a blow's white over that;
/// turned about its middle, a height over its feet. What draws the picture is the species' own.
/// </summary>
public abstract partial class PosedSprite : Node2D
{
    private static readonly CanvasItemMaterial Unshaded = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly int _width, _height;
    private readonly float _middle;
    private readonly Sprite2D _body, _white, _glow;
    private readonly Image _albedo, _silhouette, _emission;
    private readonly ImageTexture _albedoTexture, _silhouetteTexture, _emissionTexture;

    protected PosedSprite(int width, int height, int originX, int originY, float middle)
    {
        (_width, _height, _middle) = (width, height, middle);
        var offset = new Vector2(-originX, -originY + middle);
        (_body, _white, _glow) = (Sprite(offset), Sprite(offset), Sprite(offset));
        (_albedo, _silhouette, _emission) = (Blank(), Blank(), Blank());
        _albedoTexture = ImageTexture.CreateFromImage(_albedo);
        _silhouetteTexture = ImageTexture.CreateFromImage(_silhouette);
        _emissionTexture = ImageTexture.CreateFromImage(_emission);
        (_body.Texture, _white.Texture, _glow.Texture) = (_albedoTexture, _silhouetteTexture, _emissionTexture);
        Lit.Set(_body);
        _white.Material = Unshaded;
        _glow.Material = Unshaded;
        AddChild(_body);
        AddChild(_glow);
        AddChild(_white);
    }

    private static Sprite2D Sprite(Vector2 offset) => new()
    {
        Centered = false,
        Offset = offset,
        TextureFilter = TextureFilterEnum.Nearest,
    };

    private Image Blank() => Image.CreateEmpty(_width, _height, false, Image.Format.Rgba8);

    /// <summary>Its layers, each RGBA of its size, up to the screen.</summary>
    protected void Paint(byte[] albedo, byte[] silhouette, byte[] emission)
    {
        _albedo.SetData(_width, _height, false, Image.Format.Rgba8, albedo);
        _silhouette.SetData(_width, _height, false, Image.Format.Rgba8, silhouette);
        _emission.SetData(_width, _height, false, Image.Format.Rgba8, emission);
        _albedoTexture.Update(_albedo);
        _silhouetteTexture.Update(_silhouette);
        _emissionTexture.Update(_emission);
    }

    /// <summary>At <paramref name="feet"/>, shaken across, turned about its middle, facing 1 right or −1 left, a blow's white over it.</summary>
    protected void Place(Vector2 feet, double shake, double angle, int face, double flash)
    {
        _white.Modulate = new Color(1, 1, 1, (float)flash);
        Position = feet + new Vector2((float)shake, -_middle);
        Rotation = (float)angle;
        Scale = new Vector2(face, 1);
    }

    /// <summary>The world's pixels of <paramref name="albedo"/> as it stands now, for a burst.</summary>
    protected void PaintInto(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into, byte[] albedo) =>
        PixelBursts.Paint(into, Transform * new Transform2D(0, _body.Offset), _width, _height, albedo);
}
