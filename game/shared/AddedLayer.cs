using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>A layer drawn added over its parent, for light: what <paramref name="draw"/> draws on it. Takes no pointer.</summary>
public sealed partial class AddedLayer(Action<CanvasItem> draw) : Control
{
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    }

    public override void _Draw() => draw(this);
}
