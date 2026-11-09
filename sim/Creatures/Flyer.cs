using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The flyer (the catcher's jetpack, tracker-catchers): no gravity; rising it moves its speed up first, else across first, then down,
/// each axis stopped where its body would meet something solid. Leaving (<see cref="Through"/>), nothing stops it. What it keeps, in its
/// <c>Mem</c>: whether it passes through.
/// </summary>
internal sealed class FlyerBody : BodyRule
{
    internal const string Through = "through";

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
        // Rising it goes up first, else across first: a run up a step, then on; across an edge, then down (catcher-paths).
        if (c.VY < 0 && !Moves(m, c, s, c.X, c.Y + c.VY)) c.VY = 0;
        if (c.VX != 0 && !Moves(m, c, s, c.X + c.VX, c.Y)) c.VX = 0;
        if (c.VY > 0 && !Moves(m, c, s, c.X, c.Y + c.VY)) c.VY = 0;
    }

    /// <summary>Moves <paramref name="c"/> to (<paramref name="x"/>, <paramref name="y"/>) if nothing solid is there; else false.</summary>
    private static bool Moves(CreaturesModule m, Creature c, Spec.Shape s, int x, int y)
    {
        if (m.Overlaps(s, x, y)) return false;
        (c.X, c.Y) = (x, y);
        return true;
    }
}
