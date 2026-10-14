using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Makes a picture give off light in the cells mode (0026): added as a child of a <see cref="Sprite2D"/>,
/// its glowing pixels — those at least <see cref="Emission.Glowing"/> bright — light the cells they are
/// drawn in, as the sprite is drawn now (moved, turned, flipped, its picture changed), in its kind's
/// colour and strength (<see cref="Light.Emits"/>) times <see cref="Strength"/>. A picture drawn
/// otherwise — a tile — is given as <see cref="Picture"/>, laid from this node's own place. While it or
/// a parent is hidden it gives none.
/// </summary>
public partial class Emitter : Node2D
{
    private static readonly List<Emitter> Shining = [];
    private static readonly Dictionary<(Texture2D, float), EmitterMask> Masks = new();

    /// <summary>Every emitter in the tree, for the light to read each frame.</summary>
    public static IReadOnlyList<Emitter> All => Shining;

    /// <summary>What kind of light it gives: one of <see cref="Light.Emits"/>.</summary>
    public Emission Emits { get; }

    /// <summary>How brightly it shines now, a share of its kind's strength: a lamp's power, a fire's blaze.</summary>
    public float Strength { get; set; } = 1;

    /// <summary>The picture whose pixels glow, laid from this node's place; none for its parent sprite's, as that is drawn.</summary>
    public Texture2D? Picture { get; set; }

    public Emitter(Emission emits) => Emits = emits;

    public override void _EnterTree() => Shining.Add(this);

    public override void _ExitTree() => Shining.Remove(this);

    /// <summary>
    /// The glowing pixels it shows now and where they lie in the world (a picture's pixels to the world's);
    /// false while it shows none: hidden, dark, or with no picture.
    /// </summary>
    public bool Shows(out EmitterMask mask, out System.Numerics.Matrix3x2 toWorld)
    {
        mask = null!;
        toWorld = default;
        if (Strength <= 0 || !IsVisibleInTree()) return false;
        var transform = GlobalTransform;
        var picture = Picture;
        if (picture == null)
        {
            if (GetParent() is not Sprite2D { Texture: { } texture } sprite) return false;
            picture = texture;
            // Laid as the sprite lays it: centred or from its corner, moved by its offset, flipped.
            var size = texture.GetSize();
            var corner = (sprite.Centered ? -size / 2 : Vector2.Zero) + sprite.Offset;
            transform = sprite.GlobalTransform * Laid(corner, size, sprite.FlipH, sprite.FlipV);
        }
        mask = MaskOf(picture, Emits.Glowing);
        toWorld = new System.Numerics.Matrix3x2(transform.X.X, transform.X.Y, transform.Y.X, transform.Y.Y, transform.Origin.X, transform.Origin.Y);
        return mask.Count > 0;
    }

    /// <summary>
    /// A picture's pixels to its sprite's space: from <paramref name="corner"/>, a flipped picture mirrored
    /// within its own rectangle, as Godot draws it.
    /// </summary>
    private static Transform2D Laid(Vector2 corner, Vector2 size, bool flipH, bool flipV) => new(
        new Vector2(flipH ? -1 : 1, 0),
        new Vector2(0, flipV ? -1 : 1),
        corner + new Vector2(flipH ? size.X : 0, flipV ? size.Y : 0));

    private static EmitterMask MaskOf(Texture2D picture, float glowing)
    {
        if (Masks.TryGetValue((picture, glowing), out var mask)) return mask;
        var image = picture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return Masks[(picture, glowing)] = EmitterMask.Of(image.GetData(), image.GetWidth(), image.GetHeight(), glowing);
    }
}
