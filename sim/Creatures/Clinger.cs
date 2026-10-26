using Stillwake.Sim.Debris;
using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The clinger (the sentry, 2026-10-26-creatures-are-composed): clamped to the top of a wreck, <c>along</c> from its middle, on
/// one of its picture's perches (<c>Wreckage.Perches</c>), or on its box's top when let out on one with none; riding its drift.
/// Watching, it crawls along its perch. Off it, it flies straight by its speed, nothing stopping it (0006), and clamps on again
/// when its drive brings it home. With its wreck gone it dies. What it keeps, in its <c>Mem</c>:
/// its piece, its perch (one past its number; 0 for the box's top), the point along it, whether it crawls back, whether it is
/// clamped on, and that point last seen.
/// </summary>
internal sealed class ClingerBody : BodyRule
{
    internal const string Piece = "piece", Perch = "perch", Along = "along", Back = "back", Mounted = "mounted", HomeX = "homeX", HomeY = "homeY";

    /// <summary>Thousandths of a cell in a pixel of a wreck's picture.</summary>
    private const int Pixel = Unit / Content.Wreckage.PixelsPerCell;

    internal override Type Serves => typeof(Spec.Clinger);

    internal override bool Unstick(CreaturesModule m, Creature c, Spec.Shape s) => false;

    internal override bool Grounded(CreaturesModule m, Creature c, Spec.Shape s) => c.Recall(Mounted) == 1;

    internal override string? ReleaseProblem(CreaturesModule m, Spec.Species spec, int cell) => WreckAt(m, cell) == null ? "creatures.noPiece" : null;

    /// <summary>
    /// Clamped to the wreck covering <paramref name="cell"/>, at the point of its top over the cell: on the perch under it, its body
    /// kept on the perch, else on the box's top.
    /// </summary>
    internal override void Place(CreaturesModule m, Creature c, Spec.Species spec, int cell)
    {
        var piece = WreckAt(m, cell)!;
        var (w, _) = Content.Wreckage.SizeOf(piece.Item)!.Value;
        int x = m.Middle(cell % m.GridWidth) - piece.X;
        var perches = Content.Wreckage.PerchesOf(piece.Item, piece.Id);
        for (int i = 0; i < perches.Count; i++)
            if (Fits(perches[i], spec.Body) && x >= perches[i].Left * Pixel - w * Unit / 2 && x < perches[i].Right * Pixel - w * Unit / 2)
            {
                var (lo, hi) = Span(piece, perches[i], spec.Body);
                Mount(m, c, piece, i + 1, Math.Clamp(x, lo, hi));
                return;
            }
        int room = Math.Max(0, (w - spec.Body.Width) * Unit / 2);
        Mount(m, c, piece, 0, Math.Clamp(x, -room, room));
    }

    /// <summary>Whether a body of <paramref name="s"/> stands on <paramref name="perch"/> whole.</summary>
    internal static bool Fits(Content.Wreckage.Perch perch, Spec.Shape s) => (perch.Right - perch.Left) * Pixel >= s.Width * Unit;

    /// <summary>The points along <paramref name="piece"/> from its middle a body of <paramref name="s"/> on <paramref name="perch"/> stands between.</summary>
    internal static (int Lo, int Hi) Span(Piece piece, Content.Wreckage.Perch perch, Spec.Shape s)
    {
        int half = Content.Wreckage.SizeOf(piece.Item)!.Value.W * Unit / 2, body = s.Width * Unit / 2;
        return (perch.Left * Pixel - half + body, perch.Right * Pixel - half - body);
    }

    /// <summary>
    /// <paramref name="c"/> clamped to <paramref name="piece"/>'s top, on its <paramref name="perch"/> (one past its number; 0 for the
    /// box's top), <paramref name="along"/> from its middle: where it was let out, or the wreck it came with.
    /// </summary>
    internal static void Mount(CreaturesModule m, Creature c, Piece piece, int perch, int along)
    {
        c.Keep(Piece, piece.Id);
        c.Keep(Perch, perch);
        c.Keep(Along, along);
        c.Keep(Mounted, 1);
        // Blows on the wreck before it was there are none of its business.
        c.Keep(GuardDrive.Answered, m.Debris.Clock + 1);
        (c.X, c.Y) = Hold(m, c);
    }

    /// <summary>
    /// Where <paramref name="c"/> clamps on: its wreck's perch, or box top, at its point along it, kept as its home; with the wreck
    /// gone, the home last kept.
    /// </summary>
    internal static (int X, int Y) Hold(CreaturesModule m, Creature c)
    {
        if (m.Debris.PieceById(c.Recall(Piece)) is { } piece && Content.Wreckage.SizeOf(piece.Item) is var (_, h))
        {
            var s = Spec.Get(c.Species).Body;
            int perch = c.Recall(Perch), top = perch == 0 ? 0 : Content.Wreckage.PerchesOf(piece.Item, piece.Id)[perch - 1].Top * Pixel;
            c.Keep(HomeX, ClampX(m, s, piece.X + c.Recall(Along)));
            c.Keep(HomeY, ClampY(m, s, piece.Y - h * Unit / 2 + top));
        }
        return (c.Recall(HomeX), c.Recall(HomeY));
    }

    /// <summary>Clamped on a perch, <paramref name="c"/> crawls its species' <c>Crawl</c> along it, turning at its ends.</summary>
    internal static void Crawl(CreaturesModule m, Creature c, Spec.Species spec)
    {
        var body = (Spec.Clinger)spec.Body;
        int perch = c.Recall(Perch);
        if (body.Crawl == 0 || perch == 0 || c.Recall(Mounted) != 1 || m.Debris.PieceById(c.Recall(Piece)) is not { } piece) return;
        var (lo, hi) = Span(piece, Content.Wreckage.PerchesOf(piece.Item, piece.Id)[perch - 1], body);
        bool back = c.Recall(Back) == 1;
        int along = c.Recall(Along) + (back ? -body.Crawl : body.Crawl);
        if (along >= hi) (along, back) = (hi, true);
        else if (along <= lo) (along, back) = (lo, false);
        c.Keep(Along, along);
        c.Keep(Back, back ? 1 : 0);
    }

    /// <summary>Clamped on, it rides its wreck; else it flies its speed, kept on the grid. Its wreck gone, it dies.</summary>
    internal override void Step(CreaturesModule m, Creature c, Spec.Species spec, bool grounded)
    {
        // Its wreck broken or gone, it goes with it (the user's word: never a sentry left adrift).
        if (m.Debris.PieceById(c.Recall(Piece)) == null)
        {
            m.Kill(c, spec);
            return;
        }
        // Read now, not as the tick began: a pounce launched this tick has let go.
        if (c.Recall(Mounted) == 1)
        {
            (c.X, c.Y) = Hold(m, c);
            (c.VX, c.VY) = (0, 0);
            return;
        }
        c.X = ClampX(m, spec.Body, c.X + c.VX);
        c.Y = ClampY(m, spec.Body, c.Y + c.VY);
    }

    /// <summary>A body's middle kept so the whole of it is on the grid across.</summary>
    private static int ClampX(CreaturesModule m, Spec.Shape s, int x) => Math.Clamp(x, s.Width * Unit / 2, m.GridWidth * Unit - s.Width * Unit / 2);

    /// <summary>A body's feet kept so the whole of it is on the grid down.</summary>
    private static int ClampY(CreaturesModule m, Spec.Shape s, int y) => Math.Clamp(y, s.Height * Unit, m.GridHeight * Unit);

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
