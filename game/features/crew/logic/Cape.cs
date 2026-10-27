using System;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>Where a piece of the cape is drawn, in the figure's frame (+x the way it faces, +y down): its top and its turn, 0 hanging straight down.</summary>
public readonly record struct CapePiece(double X, double Y, double Turn);

/// <summary>
/// A heavy cape (a prototype for capes and tails of hair): a <see cref="Rope"/> hanging from the shoulders down to the
/// floor, kept behind the back, and the <see cref="Pieces"/> of its picture laid along it, each turned from one of
/// its points toward the next but one. Heavy: strong gravity and little air drag, so it hangs and only trails a
/// little as the hero runs. Drawn only, never simulated: every peer moves its own.
/// </summary>
public sealed class Cape
{
    /// <summary>The pictures it is drawn in (<c>art/hero_cape_n.png</c>), top to hem, each over two of the rope's links.</summary>
    public const int Count = 4;
    /// <summary>
    /// Where it hangs from, from the feet in pixels: the back of the neck. How long it is, to the floor, and how it
    /// moves a step: what it keeps of its speed, gravity, and a faint drift so it is never quite still.
    /// </summary>
    public static readonly (double X, double Y) From = (-6, -(Rig.Neck - 1));
    public const double Length = Rig.Neck - 1.5;
    private static readonly RopeStyle Style = new(Links: Count * 2, Keep: 0.965, Gravity: 0.65, Drift: 0.012, DriftSpeed: 0.03, Wave: 0.7);
    /// <summary>How hard, a pass, the cloth is drawn back against the body where it hangs off it: it clings rather than tents.</summary>
    private const double Cling = 0.005;

    private Rope? _rope;
    private readonly CapePiece[] _pieces = new CapePiece[Count];

    /// <summary>The pieces as last laid, top to hem.</summary>
    public ReadOnlySpan<CapePiece> Pieces => _pieces;

    /// <summary>
    /// Moves it on by <paramref name="elapsedMs"/>. <paramref name="toWorld"/> and <paramref name="toFrame"/> turn a point
    /// of the figure's frame into the world's pixels and back; <paramref name="backAt"/> is how far behind the middle the
    /// back lies at a height of the frame: the air pack's back where it is worn, the legs' below.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), (double X, double Y)> toWorld, Func<(double X, double Y), (double X, double Y)> toFrame, Func<double, double> backAt)
    {
        var top = toWorld(From);
        _rope ??= new Rope(Style, top, toWorld((From.X, From.Y + Length)));
        _rope.Hang(top, elapsedMs, Length, knot =>
        {
            var (x, y) = toFrame((knot.X, knot.Y));
            // Against the back, never in it nor under the feet; drawn a little toward it where it hangs off.
            double most = -backAt(y), lies = x > most ? most : x + (most - x) * Cling;
            var (wx, wy) = toWorld((lies, Math.Min(y, -0.5)));
            // Moved with its speed kept, so it slides along the back rather than bouncing off.
            knot.Px += wx - knot.X;
            knot.Py += wy - knot.Y;
            (knot.X, knot.Y) = (wx, wy);
        });
        for (int i = 0; i < Count; i++)
        {
            var (ax, ay) = toFrame(_rope.DrawnAt(i * 2));
            var (bx, by) = toFrame(_rope.DrawnAt(i * 2 + 2));
            _pieces[i] = new CapePiece(ax, ay, Math.Atan2(-(bx - ax), by - ay));
        }
    }

    /// <summary>Forgets where it hung, so it falls straight from the shoulders next step: after a jump in place, a respawn.</summary>
    public void Reset() => _rope = null;
}
