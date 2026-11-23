using System;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Spec = Stillwake.Content.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// A piece's body on the host player's client (debris-physics): a box <c>Debris.ShapeShare</c> of its picture either way, half of it
/// <see cref="HalfW"/> by <see cref="HalfH"/> in thousandths of a cell; its <see cref="Mass"/>, its box's area in cells² at
/// <c>Debris.Density</c>; its <see cref="Inertia"/> about its middle, a solid box's, in mass times thousandths of a cell².
/// </summary>
public readonly record struct PieceShape(double HalfW, double HalfH, double Mass, double Inertia)
{
    /// <summary>Half a small piece's picture each way, in thousandths of a cell, as the hands aim at it: a cell or a little more across.</summary>
    public const int SmallHalf = 1000;

    /// <summary>The body of a piece of <paramref name="item"/>.</summary>
    public static PieceShape Of(string item)
    {
        var (w, h) = WreckAim.HalfOf(item) is var (ww, wh) ? (ww - WreckAim.Margin, wh - WreckAim.Margin) : (SmallHalf, SmallHalf);
        double halfW = w * Spec.ShapeShare / 1000.0, halfH = h * Spec.ShapeShare / 1000.0;
        return Sized(halfW, halfH);
    }

    /// <summary>A body <paramref name="halfW"/> by <paramref name="halfH"/> half its box each way, in thousandths of a cell.</summary>
    public static PieceShape Sized(double halfW, double halfH)
    {
        double cells = 2 * halfW / 1000 * (2 * halfH / 1000);
        double mass = Math.Max(cells * Spec.Density / 1000, 0.01);
        return new PieceShape(halfW, halfH, mass, mass * (4 * halfW * halfW + 4 * halfH * halfH) / 12);
    }

    /// <summary>
    /// How fast a body eases back to its own drift and tumble, a second: <c>Debris.Drag</c> a tick as a rate (0.506), the physics' damping.
    /// Damping toward still and a steady push of <see cref="Push"/> settle it at its drift, as the prototype's piece.gd.
    /// </summary>
    public static readonly double DragPerSecond = -Math.Log(1 - Spec.Drag / 1000.0) * World.TicksPerSecond;

    /// <summary>The steady push, mass times a second's motion a second, that holds a body of <paramref name="mass"/> at <paramref name="motion"/> against <see cref="DragPerSecond"/>.</summary>
    public static double Push(double mass, double motion) => mass * motion * DragPerSecond;
}
