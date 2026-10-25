using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The faces a tool's hit would strike (<see cref="FanFaces"/>), a thin rim just inside each, breathing in step with the aim's
/// outline (<see cref="Breath"/>): light added over the world, unlit, so it reads in the dark; drawn lines, no light of its own.
/// </summary>
public partial class FaceRim : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>The rim's two pixels, the edge's and the one inside it, as shares of its brightness.</summary>
    private const float Edge = 1f, Inside = 0.35f;

    private List<Face> _faces = [];
    private Color _colour;

    public FaceRim()
    {
        ZIndex = Layers.Dug;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Visible = false;
    }

    /// <summary>Shows <paramref name="faces"/> in <paramref name="colour"/> this frame, or nothing with none.</summary>
    public void Show(List<Face> faces, Color colour)
    {
        (_faces, _colour) = (faces, colour);
        Visible = faces.Count > 0;
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        var colour = _colour with { A = (float)Breath.Alpha(Time.GetTicksMsec()) };
        foreach (var f in _faces)
            for (int i = 0; i < 2; i++)
            {
                var c = colour with { A = colour.A * (i == 0 ? Edge : Inside) };
                float x = f.X * Cell, y = f.Y * Cell;
                var line = f.Side switch
                {
                    Side.Left => new Rect2(x + i, y, 1, Cell),
                    Side.Right => new Rect2(x + Cell - 1 - i, y, 1, Cell),
                    Side.Top => new Rect2(x, y + i, Cell, 1),
                    _ => new Rect2(x, y + Cell - 1 - i, Cell, 1),
                };
                DrawRect(line, c);
            }
    }
}
