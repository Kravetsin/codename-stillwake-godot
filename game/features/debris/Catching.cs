using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The piece of debris E would catch: its cell, whether the hero reaches it, and why the sim would
/// refuse the grab, if it would.
/// </summary>
public sealed record Catch(int Piece, int Cell, bool InReach, string? Problem);

/// <summary>
/// What the hands do with debris (the debris half of apps/game/src/features/crew/aim.ts), how a piece
/// fades (features/debris), and the pieces this peer is grabbing (session/predicted.ts).
/// </summary>
public static class Catching
{
    private const int Unit = DebrisModule.Unit;
    /// <summary>How far a piece fades in after it spawns and out before it is gone, in thousandths of a cell.</summary>
    private const double Fade = 8000;

    /// <summary>How visible a piece at <paramref name="x"/> is: out of the dark over the start of its path, back into it at the end.</summary>
    public static double FadeOf(double x, int start, int end)
    {
        double into = Math.Min(1, (start - x) / Fade);
        double away = Math.Min(1, (x - end) / Fade);
        return Math.Max(0, Math.Min(into, away));
    }

    /// <summary>The pieces of debris this peer has a grab in flight for: gone from its view at once (0014).</summary>
    public static IReadOnlySet<int> Grabbing(IReadOnlyList<Command> inFlight) =>
        inFlight.OfType<Grab>().Select(g => g.Piece).ToHashSet();

    /// <summary>
    /// The piece of debris peer <paramref name="peer"/>'s hero, its body in <paramref name="bodyCell"/>,
    /// aims at with the pointer at <paramref name="point"/>, in cells: the one nearest the pointer within
    /// <c>Debris.AimRadius</c>, only while the hero is out in space, where it can catch one, and none in
    /// <paramref name="passOver"/>, grabbed already, its grab still in flight. Its cell is where the
    /// piece is, which may be beside the pointer's.
    /// </summary>
    public static Catch? AimAt(
        DebrisModule debris, IRoomsQueries rooms, int width, int peer, int bodyCell, Func<int, bool> reaches,
        (double X, double Y) point, IReadOnlySet<int>? passOver = null)
    {
        if (rooms.RoomAt(bodyCell) != RoomsModule.Space) return null;
        Piece? nearest = null;
        double best = (double)Spec.AimRadius * Spec.AimRadius;
        foreach (var p in debris.Pieces)
        {
            if (passOver?.Contains(p.Id) == true) continue;
            double dx = p.X - point.X * Unit, dy = p.Y - point.Y * Unit;
            if (dx * dx + dy * dy > best) continue;
            best = dx * dx + dy * dy;
            nearest = p;
        }
        if (nearest == null) return null;
        int cell = DebrisModule.CellOf(nearest.X, nearest.Y, width);
        return new Catch(nearest.Id, cell, reaches(cell), debris.GrabProblem(peer, nearest.Id));
    }

    /// <summary>What a press sends: the grab of the piece aimed at within reach, unless the sim would refuse it.</summary>
    public static Grab? GrabOf(Catch? aim, bool pressed) =>
        pressed && aim is { InReach: true, Problem: null } ? new Grab(aim.Piece) : null;
}
