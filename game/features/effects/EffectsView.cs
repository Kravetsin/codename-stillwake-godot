using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Effects;

/// <summary>
/// Every particle the features emit into <see cref="Particles.Shared"/> (features/effects): dust, sparks,
/// embers, air. Each is a square of its colour, unlit, over everything in the world, plans included;
/// those that glow have a soft halo added under them, and light what is around them
/// (<c>LightView</c>, 0019). Moved on every frame.
/// </summary>
public partial class EffectsView : Node2D
{
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly Node2D _halos = new()
    {
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        },
    };
    private readonly Node2D _specks = new() { Material = Unlit };

    public EffectsView()
    {
        ZIndex = 21;
        AddChild(_halos);
        AddChild(_specks);
        _halos.Draw += DrawHalos;
        _specks.Draw += DrawSpecks;
    }

    /// <summary>The particles flying: what a recording checks.</summary>
    public static int Count => Particles.Shared.Live.Count;

    public override void _Process(double delta)
    {
        Particles.Shared.Step(delta * 1000);
        _halos.QueueRedraw();
        _specks.QueueRedraw();
    }

    private static Color ColourOf(int rgb, double alpha) =>
        new(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, (float)alpha);

    private void DrawHalos()
    {
        foreach (var p in Particles.Shared.Live)
        {
            if (p.Effect.Glow is not var (colour, size)) continue;
            float s = (float)size;
            _halos.DrawTextureRect(PixelArt.Glow, new Rect2((float)p.X - s / 2, (float)p.Y - s / 2, s, s), false, ColourOf(colour, p.Alpha * 0.8));
        }
    }

    private void DrawSpecks()
    {
        foreach (var p in Particles.Shared.Live)
        {
            float s = (float)p.Size;
            _specks.DrawRect(new Rect2((float)p.X - s / 2, (float)p.Y - s / 2, s, s), ColourOf(p.Effect.Colour, p.Alpha));
        }
    }
}
