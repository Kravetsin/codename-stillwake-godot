using System;
using Godot;

namespace Stillwake.Game.Features.Research;

/// <summary>Draws <see cref="BoardInk"/>'s strokes: thin at their ends, thicker where the stylus pressed, gone where it lifted.</summary>
public static class InkDraw
{
    /// <summary>The thinnest a stroke is drawn on the screen: under this it would fall apart into dashes, and on a scaled canvas it is scaled too.</summary>
    public const float Least = 0.9f;

    /// <summary>How far a stroke's edge fades out, on the screen: what an antialiased line's does.</summary>
    private const float Feather = 1;

    /// <summary>A stroke, <paramref name="drawn"/> of the way along, as one call (<see cref="BoardInk.Mesh"/>); returns where its tip is.</summary>
    public static Vector2 Stroke(CanvasItem on, InkPoint[] stroke, Color colour, float width, float least = Least, float drawn = 1)
    {
        var mesh = MeshOf(stroke, width, least, drawn);
        Add(on, [mesh], colour);
        return new Vector2(mesh.Tip.X, mesh.Tip.Y);
    }

    /// <summary>Several strokes in one colour, as one call.</summary>
    public static void Strokes(CanvasItem on, InkPoint[][] strokes, Color colour, float width, float least = Least) =>
        Add(on, Array.ConvertAll(strokes, stroke => MeshOf(stroke, width, least, 1)), colour);

    // A pixel of the screen on this canvas is what least is scaled by.
    private static InkMesh MeshOf(InkPoint[] stroke, float width, float least, float drawn) => BoardInk.Mesh(stroke, width, least, least / Least * Feather, drawn);

    private static void Add(CanvasItem on, InkMesh[] meshes, Color colour)
    {
        int count = 0, corners = 0;
        foreach (var mesh in meshes) (count, corners) = (count + mesh.Indices.Length, corners + mesh.Points.Length);
        if (count == 0) return;
        var indices = new int[count];
        var points = new Vector2[corners];
        var colours = new Color[corners];
        int i = 0, p = 0;
        foreach (var mesh in meshes)
        {
            foreach (int index in mesh.Indices) indices[i++] = p + index;
            for (int k = 0; k < mesh.Points.Length; k++, p++)
            {
                points[p] = new Vector2(mesh.Points[k].X, mesh.Points[k].Y);
                colours[p] = colour with { A = colour.A * mesh.Alpha[k] };
            }
        }
        RenderingServer.CanvasItemAddTriangleArray(on.GetCanvasItem(), indices, points, colours);
    }

    /// <summary>The stylus at a stroke's tip, and the two specks it flicks up; <paramref name="scale"/> undoes what the canvas is scaled by.</summary>
    public static void Nib(CanvasItem on, Vector2 at, Color colour, float scale)
    {
        on.DrawRect(new Rect2(at + new Vector2(-1, -1) * scale, new Vector2(2.4f, 2.4f) * scale), colour);
        on.DrawRect(new Rect2(at + new Vector2(2, -4) * scale, new Vector2(1.4f, 1.4f) * scale), colour);
        on.DrawRect(new Rect2(at + new Vector2(4, -7) * scale, new Vector2(1.2f, 1.2f) * scale), colour);
    }
}
