using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A hologram window's frame once open: a thin line with a soft glow, each edge buzzing on its own (<see cref="HoloLook"/>), drawn on
/// a canvas item that adds its light. A machine's window (<see cref="MachineHolo"/>) and the drone's bag (<see cref="HoloWindow"/>).
/// </summary>
public static class HoloFrame
{
    /// <summary>The whole of <paramref name="frame"/> on <paramref name="on"/> in <paramref name="light"/>, <paramref name="seconds"/> into its buzz, at <paramref name="alpha"/>.</summary>
    public static void Buzz(CanvasItem on, HoloRect frame, Color light, double seconds, float alpha)
    {
        var r = frame;
        (float X, float Y) tl = (r.X, r.Y), tr = (r.Right, r.Y), br = (r.Right, r.Bottom), bl = (r.X, r.Bottom);
        Edge(tr, tl, 0);
        Edge(tl, bl, 1);
        Edge(bl, br, 2);
        Edge(br, tr, 3);

        void Edge((float X, float Y) a, (float X, float Y) b, int i)
        {
            float buzz = HoloLook.BuzzLow + HoloLook.BuzzDepth * (float)(0.5 + 0.5 * Math.Sin(seconds * 31 + i * 2.1) * Math.Sin(seconds * 13.7 + i));
            Line(on, new Vector2(a.X, a.Y), new Vector2(b.X, b.Y), light, alpha * buzz);
        }
    }

    /// <summary>One edge of a frame: its soft glow and its pale core.</summary>
    public static void Line(CanvasItem on, Vector2 a, Vector2 b, Color light, float alpha)
    {
        on.DrawLine(a, b, light with { A = HoloLook.FrameGlowAlpha * alpha }, HoloLook.FrameGlow, true);
        on.DrawLine(a, b, light.Lerp(Colors.White, HoloLook.FramePale) with { A = HoloLook.FrameCoreAlpha * alpha }, HoloLook.FrameCore, true);
    }
}
