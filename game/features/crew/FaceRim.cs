using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The surface a tool's hit would strike (<see cref="FanFaces"/>), shown as one glow along it rather than its blocks: light from
/// each face running a few pixels into the rock, the rock of each inner corner lit too, the glow fading out at its open ends
/// (<see cref="FaceGlow"/>) and breathing with the aim's outline (<see cref="Breath"/>). Light added over the world, unlit, so
/// it reads in the dark; drawn pixels, no light of its own.
/// </summary>
public partial class FaceRim : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>The glow's brightness a pixel deeper into the rock each, from the face in.</summary>
    private static readonly float[] Depth = [0.55f, 0.25f, 0.14f, 0.08f, 0.05f, 0.03f];

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
        float breath = (float)Breath.Alpha(Time.GetTicksMsec());
        var joined = FaceGlow.Joined(_faces);
        for (int f = 0; f < _faces.Count; f++)
        {
            var face = _faces[f];
            var (ix, iy) = FaceGlow.Inward(face.Side);
            bool across = face.Side is Side.Top or Side.Bottom;
            // The face's line of pixels, and the first pixel of the rock behind it, in pixels of the world.
            int line = face.Side switch
            {
                Side.Left => face.X * Cell,
                Side.Right => (face.X + 1) * Cell - 1,
                Side.Top => face.Y * Cell,
                _ => (face.Y + 1) * Cell - 1,
            };
            int start = across ? face.X * Cell : face.Y * Cell;
            for (int i = 0; i < Cell; i++)
            {
                // An end that meets no other face fades out over the cell.
                float fade = 1f;
                if (!joined[f].From) fade = Math.Min(fade, (i + 0.5f) / Cell);
                if (!joined[f].To) fade = Math.Min(fade, (Cell - i - 0.5f) / Cell);
                for (int k = 0; k < Depth.Length; k++)
                {
                    int depth = line + k * (across ? iy : ix);
                    var at = across ? new Vector2(start + i, depth) : new Vector2(depth, start + i);
                    DrawRect(new Rect2(at, Vector2.One), _colour with { A = Depth[k] * fade * breath });
                }
            }
        }
        // The rock in an inner corner, lit by both faces, so the glow turns it with no hole.
        foreach (var ((px, py), (dx, dy)) in FaceGlow.Corners(_faces))
            for (int kx = 0; kx < Depth.Length; kx++)
                for (int ky = 0; ky < Depth.Length; ky++)
                {
                    float x = dx > 0 ? px * Cell + kx : px * Cell - 1 - kx, y = dy > 0 ? py * Cell + ky : py * Cell - 1 - ky;
                    DrawRect(new Rect2(x, y, 1, 1), _colour with { A = Depth[Math.Max(kx, ky)] * breath });
                }
    }
}
