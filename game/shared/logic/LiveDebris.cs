using System;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>
/// The host player's own physics as drawn (debris-physics): a piece's body where the physics has it this frame, and a line's rope as
/// long as the drum has it now. The debris feature sets them while it runs the bodies; elsewhere, and on a guest's client, they give
/// none and the sim's poses and lengths are drawn. Drawing from the bodies, not the sim the feed corrects twenty times a second, keeps
/// a piece's motion as smooth as the physics.
/// </summary>
public static class LiveDebris
{
    /// <summary>Where piece <c>id</c>'s body is drawn now, or none.</summary>
    public static Func<int, PiecePose?> Pose { get; set; } = _ => null;

    /// <summary>How long the line of peer <c>by</c> on piece <c>piece</c> is now, in thousandths of a cell, or none.</summary>
    public static Func<int, int, double?> LineLength { get; set; } = (_, _) => null;

    /// <summary>
    /// Where a piece with no body here is drawn, given where the sim has it carried on: on a guest's client its corrections eased away
    /// (<see cref="PieceEase"/>), else just there.
    /// </summary>
    public static Func<Piece, PiecePose, PiecePose> Eased { get; set; } = (_, sim) => sim;
}
