using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A guard's body in ink (2026-11-01-guards-in-ink): a sprite a picture, placed each frame as its rig says and turned by the GPU; its plates lit as the world, what
/// shines over them unlit, a blow's white over all. Its origin the middle of its feet. A picture shared by every guard of its kind.
/// </summary>
public partial class InkRig : Node2D
{
    // Unshaded, so the world's darkness does not grey it: a lamp and a flash are light.
    private static readonly CanvasItemMaterial Unshaded = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly string _id;
    private readonly IReadOnlyDictionary<string, CreatureArt.Part> _parts;
    private readonly List<(Sprite2D Sprite, Sprite2D? White)> _sprites = [];
    private readonly Node2D _flash = new();

    public InkRig(string id, IReadOnlyDictionary<string, CreatureArt.Part> parts) => (_id, _parts) = (id, parts);

    /// <summary>Its pictures where <paramref name="slots"/> put them, <paramref name="flash"/> of a blow's white over it, its plates darkened by <paramref name="dim"/>.</summary>
    public void Show(InkSlot[] slots, double flash, double dim)
    {
        if (_sprites.Count == 0) Make(slots);
        float shade = 1 - (float)dim;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            var (sprite, white) = _sprites[i];
            sprite.Position = new Vector2((float)slot.X, (float)slot.Y);
            sprite.Rotation = (float)slot.A;
            if (slot.Shines)
            {
                sprite.Visible = slot.Alpha > 0.01;
                sprite.Modulate = new Color((float)slot.Tint.R / 255, (float)slot.Tint.G / 255, (float)slot.Tint.B / 255, (float)slot.Alpha);
            }
            else sprite.Modulate = new Color(shade, shade, shade);
            if (white != null) (white.Position, white.Rotation) = (sprite.Position, sprite.Rotation);
        }
        _flash.Visible = flash > 0.01;
        _flash.Modulate = new Color(1, 1, 1, (float)flash);
    }

    private void Make(InkSlot[] slots)
    {
        foreach (var slot in slots)
        {
            var sprite = Sprite(slot.Part, CreatureParts.Kind.Colour);
            AddChild(sprite);
            if (slot.Shines)
            {
                sprite.Material = Unshaded;
                _sprites.Add((sprite, null));
                continue;
            }
            Lit.Set(sprite);
            var white = Sprite(slot.Part, CreatureParts.Kind.White);
            white.Material = Unshaded;
            _flash.AddChild(white);
            _sprites.Add((sprite, white));
        }
        AddChild(_flash);
    }

    private Sprite2D Sprite(string part, CreatureParts.Kind kind)
    {
        var p = _parts[part];
        return new Sprite2D { Centered = false, Offset = new Vector2(-p.PivotX, -p.PivotY), Texture = CreatureParts.Ink(_id, part, kind) };
    }

    /// <summary>Every pixel of its plates as drawn now through <paramref name="toWorld"/>, the last drawn over the rest: what it crumbles into.</summary>
    public void Pixels(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into, Transform2D toWorld)
    {
        foreach (var (sprite, white) in _sprites)
        {
            if (white == null || sprite.Texture is not { } texture || CreatureParts.PixelsOf(texture) is not var (w, h, data)) continue;
            PixelBursts.Paint(into, toWorld * Transform * sprite.Transform * new Transform2D(0, sprite.Offset), w, h, data);
        }
    }
}
