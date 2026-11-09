using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The flyer (the catcher's jetpack, tracker-catchers): no gravity; it moves its speed across, then down, each axis stopped where its body
/// would meet something solid. Stopped, one that wears strikes every solid cell its body would take there, ascending, every
/// <c>CutEvery</c> ticks, when all of them are grown walls (<c>Building.Wears</c>); one that is not stays stopped, so it slides along
/// the other axis. Leaving (<see cref="Through"/>), nothing stops it. What it keeps, in its <c>Mem</c>: whether it passes through,
/// the clock of its next strike, and of its last.
/// </summary>
internal sealed class FlyerBody : BodyRule
{
    internal const string Through = "through", NextCut = "nextCut", LastCut = "lastCut";

    internal override Type Serves => typeof(Spec.Flyer);

    /// <summary>Something built into it since: it rises a cell a tick, through anything, till it is free, spending the tick.</summary>
    internal override bool Unstick(CreaturesModule m, Creature c, Spec.Shape s)
    {
        if (c.Recall(Through) == 1 || !m.Overlaps(s, c.X, c.Y)) return false;
        c.Y = Math.Max(s.Height * Unit, c.Y - Unit);
        return true;
    }

    internal override bool Grounded(CreaturesModule m, Creature c, Spec.Shape s) => false;

    /// <summary>Only where something has gravity (slice 1, the planet); never in anything solid.</summary>
    internal override string? ReleaseProblem(CreaturesModule m, Spec.Species spec, int cell)
    {
        if (m.Rules.Floats) return "creatures.notHere";
        var (x, y) = Standing(spec, cell % m.GridWidth, cell / m.GridWidth);
        return m.Overlaps(spec.Body, x, y) ? "creatures.blocked" : null;
    }

    internal override void Place(CreaturesModule m, Creature c, Spec.Species spec, int cell) =>
        (c.X, c.Y) = Standing(spec, cell % m.GridWidth, cell / m.GridWidth);

    internal override void Step(CreaturesModule m, Creature c, Spec.Species spec, bool grounded)
    {
        var s = spec.Body;
        if (c.Recall(Through) == 1)
        {
            c.X = Math.Clamp(c.X + c.VX, s.Width * Unit / 2, m.GridWidth * Unit - s.Width * Unit / 2);
            c.Y = Math.Max(s.Height * Unit, c.Y + c.VY);
            return;
        }
        if (c.VX != 0 && !Moves(m, c, spec, c.X + c.VX, c.Y)) c.VX = 0;
        if (c.VY != 0 && !Moves(m, c, spec, c.X, c.Y + c.VY)) c.VY = 0;
    }

    /// <summary>Moves <paramref name="c"/> to (<paramref name="x"/>, <paramref name="y"/>) if nothing solid is there: else it may strike its way, and false.</summary>
    private static bool Moves(CreaturesModule m, Creature c, Spec.Species spec, int x, int y)
    {
        if (!m.Overlaps(spec.Body, x, y))
        {
            (c.X, c.Y) = (x, y);
            return true;
        }
        if (c.Act == null) Cut(m, c, (Spec.Flyer)spec.Body, x, y);
        return false;
    }

    /// <summary>
    /// <paramref name="c"/>'s strike on what stops a body of <paramref name="f"/> at (<paramref name="x"/>, <paramref name="y"/>):
    /// every solid cell there, ascending, when its time has come and all of them wear; none past the grid or in the fog.
    /// </summary>
    private static void Cut(CreaturesModule m, Creature c, Spec.Flyer f, int x, int y)
    {
        if (f.Wear == 0 || m.Clock < c.Recall(NextCut)) return;
        int half = f.Width * Unit / 2;
        int left = Down(x - half, Unit), right = Down(x + half - 1, Unit), top = Down(y - f.Height * Unit, Unit), bottom = Down(y - 1, Unit);
        for (int row = top; row <= bottom; row++)
            for (int column = left; column <= right; column++)
                if (m.Solid(column, row) && (column < 0 || row < 0 || column >= m.GridWidth || row >= m.GridHeight || !m.Wears(row * m.GridWidth + column)))
                    return;
        for (int row = top; row <= bottom; row++)
            for (int column = left; column <= right; column++)
                if (m.Solid(column, row)) m.Wear(row * m.GridWidth + column, f.Wear);
        c.Keep(NextCut, m.Clock + f.CutEvery);
        c.Keep(LastCut, m.Clock);
    }
}
