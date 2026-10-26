using Stillwake.Sim.Debris;
using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The clinger (the sentry, 2026-10-26-creatures-are-composed): clamped to the top of a wreck, <c>along</c> from its middle,
/// riding its drift. Off it, it flies straight by its speed, nothing stopping it (0006), and clamps on again when its drive
/// brings it home. With its wreck gone it holds the last point it clamped to. What it keeps, in its <c>Mem</c>: its piece, the
/// point along it, whether it is clamped on, and that point last seen.
/// </summary>
internal sealed class ClingerBody : BodyRule
{
    internal const string Piece = "piece", Along = "along", Mounted = "mounted", HomeX = "homeX", HomeY = "homeY";

    internal override Type Serves => typeof(Spec.Clinger);

    internal override bool Unstick(CreaturesModule m, Creature c, Spec.Shape s) => false;

    internal override bool Grounded(CreaturesModule m, Creature c, Spec.Shape s) => c.Recall(Mounted) == 1;

    internal override string? ReleaseProblem(CreaturesModule m, Spec.Species spec, int cell) => WreckAt(m, cell) == null ? "creatures.noPiece" : null;

    /// <summary>Clamped to the wreck covering <paramref name="cell"/>, at the point of its top over the cell, its body on it.</summary>
    internal override void Place(CreaturesModule m, Creature c, Spec.Species spec, int cell)
    {
        var piece = WreckAt(m, cell)!;
        var (w, _) = Content.Wreckage.SizeOf(piece.Item)!.Value;
        int room = Math.Max(0, (w - spec.Body.Width) * Unit / 2);
        c.Keep(Piece, piece.Id);
        c.Keep(Along, Math.Clamp(m.Middle(cell % m.GridWidth) - piece.X, -room, room));
        c.Keep(Mounted, 1);
        (c.X, c.Y) = Hold(m, c);
    }

    /// <summary>
    /// Where <paramref name="c"/> clamps on: its wreck's top at its point along it, kept as its home; with the wreck gone, the
    /// home last kept.
    /// </summary>
    internal static (int X, int Y) Hold(CreaturesModule m, Creature c)
    {
        if (m.Debris.PieceById(c.Recall(Piece)) is { } piece && Content.Wreckage.SizeOf(piece.Item) is var (_, h))
        {
            c.Keep(HomeX, piece.X + c.Recall(Along));
            c.Keep(HomeY, piece.Y - h * Unit / 2);
        }
        return (c.Recall(HomeX), c.Recall(HomeY));
    }

    /// <summary>Clamped on, it rides its wreck; else it flies its speed, kept on the grid.</summary>
    internal override void Step(CreaturesModule m, Creature c, Spec.Species spec, bool grounded)
    {
        // Read now, not as the tick began: a pounce launched this tick has let go.
        if (c.Recall(Mounted) == 1)
        {
            (c.X, c.Y) = Hold(m, c);
            (c.VX, c.VY) = (0, 0);
            return;
        }
        c.X = Math.Clamp(c.X + c.VX, 0, m.GridWidth * Unit - 1);
        c.Y = Math.Clamp(c.Y + c.VY, spec.Body.Height * Unit, m.GridHeight * Unit);
    }

    /// <summary>The first wreck in spawn order whose box (<c>Wreckage.SizeOf</c>) covers part of <paramref name="cell"/>; null for none.</summary>
    private static Piece? WreckAt(CreaturesModule m, int cell)
    {
        int left = cell % m.GridWidth * Unit, top = cell / m.GridWidth * Unit;
        foreach (var p in m.Debris.Pieces)
            if (Content.Wreckage.SizeOf(p.Item) is var (w, h)
                && p.X - w * Unit / 2 < left + Unit && p.X + w * Unit / 2 > left && p.Y - h * Unit / 2 < top + Unit && p.Y + h * Unit / 2 > top)
                return p;
        return null;
    }
}
