using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// An <see cref="Ink"/> its owner fills and redraws: a retch's bile (food-tuning). Made bare, it draws as its parent is lit;
/// made at a layer, it stands free of its parent in the world, lit, at that layer.
/// </summary>
public partial class InkLayer : Node2D
{
    private readonly InkCanvas _canvas = new();

    /// <summary>What it draws, cleared and filled by its owner before a redraw.</summary>
    public Ink Ink { get; } = new();

    public InkLayer() => UseParentMaterial = true;

    public InkLayer(int layer)
    {
        (TopLevel, ZAsRelative, ZIndex) = (true, false, layer);
        Lit.Set(this);
    }

    public override void _Draw() => _canvas.Draw(this, Ink);
}
