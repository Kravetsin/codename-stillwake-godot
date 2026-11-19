using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// How the drone's net is drawn (the pick «Сеть по краю»), the window's frame and the drone's own lines alike: a line glows red
/// to white by its heat in a soft halo round a thin pale core, a node is a hot point in a halo. Added light.
/// </summary>
public static class NetLook
{
    private static readonly Color Cold = new(SootRun.Tune.Cold), Warm = new(SootRun.Tune.Warm), Hot = new(SootRun.Tune.Hot);

    /// <summary>The colour of <paramref name="heat"/>, 0..1: cold red, ember orange, white-hot.</summary>
    public static Color Heat(float heat)
    {
        heat = Math.Clamp(heat, 0, 1);
        return heat < 0.5f ? Cold.Lerp(Warm, heat * 2) : Warm.Lerp(Hot, heat * 2 - 1);
    }

    /// <summary>A glowing line from <paramref name="a"/> to <paramref name="b"/>, <paramref name="alpha"/> strong, its widths times <paramref name="scale"/>.</summary>
    public static void Line(Triangles to, Vector2 a, Vector2 b, float alpha, float heat, float scale = 1)
    {
        if (alpha <= 0.012f) return;
        alpha = Math.Min(1, alpha);
        var colour = Heat(heat);
        to.Quad(a, b, (SootRun.Tune.HaloWidth + SootRun.Tune.HaloWider * heat) * scale, new Color(colour, SootRun.Tune.HaloAlpha * alpha));
        to.Quad(a, b, (SootRun.Tune.MidWidth + SootRun.Tune.MidWider * heat) * scale, new Color(colour, SootRun.Tune.MidAlpha * alpha));
        var core = colour.Lerp(Colors.White, SootRun.Tune.CorePale + SootRun.Tune.CorePaler * heat);
        to.Quad(a, b, (SootRun.Tune.CoreWidth + SootRun.Tune.CoreWider * heat) * scale, new Color(core, SootRun.Tune.CoreAlpha * alpha));
    }

    /// <summary>A node at <paramref name="at"/>: a pale hot point fading through its heat's colour to nothing at <paramref name="radius"/>.</summary>
    public static void Node(Triangles to, Vector2 at, float radius, float alpha, float heat)
    {
        if (alpha <= 0.01f) return;
        var colour = Heat(heat);
        // The pick's sprite as stacked fans: a wide faint halo, a closer one, and a pale point.
        to.Fan(at, radius, new Color(colour, 0.12f * alpha), new Color(colour, 0), Sides);
        to.Fan(at, radius * 0.45f, new Color(colour, 0.3f * alpha), new Color(colour, 0), Sides);
        to.Fan(at, radius * 0.2f, new Color(colour.Lerp(Colors.White, 0.4f), 0.75f * alpha), new Color(colour, 0), Sides);
    }

    private const int Sides = 12;
}
