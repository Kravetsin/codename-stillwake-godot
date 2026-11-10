using Stillwake.Sim.Crew;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The sender's hero was crossed by creature arc <c>Arc</c> (the sentry's), as its client saw it the tick it struck (the ADR: hits on
/// a hero are that hero's): its suit pays the projectile's damage, once an arc a hero. Refused for one no longer lingering, never
/// struck, or that hurt this hero already (<c>creatures.gone</c>). An arc whose sentry has died hurts on.
/// </summary>
public sealed record Bolted(int Arc) : Command;

/// <summary>Arcs (2026-10-27-sentry-arc): struck by creatures at once, from their middle to a point, lingering for heroes' word.</summary>
public sealed partial class CreaturesModule
{
    /// <summary>The arcs still lingering, in id order; live and read-only. One first seen is one struck that tick.</summary>
    public IReadOnlyList<Arc> Arcs => State.Arcs;

    /// <summary>The index of cell <paramref name="at"/> on the grid.</summary>
    internal int CellOf((int X, int Y) at) => at.Y * _grid.Width + at.X;

    /// <summary>
    /// <paramref name="c"/>'s <paramref name="shoot"/> struck from its middle at the middle of <paramref name="cell"/>: cut to its
    /// reach on the longer axis, toward none, and at the first solid cell between, where it enters it. False, and nothing struck,
    /// with its target on its own middle.
    /// </summary>
    internal bool StrikeArc(Creature c, Spec.Species spec, Spec.Shoot shoot, int cell)
    {
        int x = c.X, y = c.Y - spec.Body.Height * Unit / 2;
        return Strike(c, shoot, x, y, Middle(cell % _grid.Width) - x, Middle(cell / _grid.Width) - y);
    }

    /// <summary>
    /// <paramref name="c"/>'s <paramref name="shoot"/> struck from its middle along fine heading <paramref name="heading"/>, where its
    /// gun points (catcher-motion): as <see cref="StrikeArc"/> struck at a cell that far along it.
    /// </summary>
    internal void StrikeAlong(Creature c, Spec.Species spec, Spec.Shoot shoot, int heading)
    {
        var (dx, dy) = Rays.FineDirection(heading);
        Strike(c, shoot, c.X, c.Y - spec.Body.Height * Unit / 2, dx, dy);
    }

    private bool Strike(Creature c, Spec.Shoot shoot, int x, int y, long run, long rise)
    {
        long far = Math.Max(Math.Abs(run), Math.Abs(rise));
        if (far == 0) return false;
        // A beam runs on past the cell aimed at, Through cells off in all; an arc stops there, or at its Reach.
        long reach = (long)(shoot.Through > 0 ? shoot.Through : shoot.Reach) * Unit;
        if (far > reach || shoot.Through > 0) (run, rise) = (run * reach / far, rise * reach / far);
        var (x1, y1) = BeamStop(x, y, run, rise);
        if (shoot.Leaves != null) (x1, y1) = Landing(x, y, x1, y1);
        State.Fired += 1;
        State.Arcs.Add(new Arc
        {
            Id = State.Fired,
            By = c.Id,
            Projectile = shoot.Projectile,
            X0 = x,
            Y0 = y,
            X1 = x1,
            Y1 = y1,
            Left = shoot.Lingers,
            Lingers = shoot.Lingers,
            Leaves = shoot.Leaves,
            Burns = shoot.Through > 0,
        });
        return true;
    }

    /// <summary>
    /// Where a line from (<paramref name="x"/>, <paramref name="y"/>) running (<paramref name="run"/>, <paramref name="rise"/>) on stops:
    /// where it enters the first solid cell, else its end. Where an arc ends, and where a catcher's aim line is drawn to (catcher-motion).
    /// </summary>
    public (int X, int Y) BeamStop(int x, int y, long run, long rise)
    {
        // As a share num / den of the way; the whole way with none.
        long num = 1, den = 1;
        Rays.Walk(_grid.Width, _grid.Height, x, y, run, rise, at =>
        {
            if (!Solid(at)) return false;
            (num, den) = (0, 1);
            int left = at % _grid.Width * Unit, top = at / _grid.Width * Unit;
            if (run != 0) Later(run > 0 ? left - x : x - (left + Unit), Math.Abs(run));
            if (rise != 0) Later(rise > 0 ? top - y : y - (top + Unit), Math.Abs(rise));
            return true;
        });
        void Later(long n, long d)
        {
            if (n * den > num * d) (num, den) = (n, d);
        }
        return ((int)(x + run * num / den), (int)(y + rise * num / den));
    }

    /// <summary>
    /// Where a lob from (<paramref name="x0"/>, <paramref name="y0"/>) aimed at (<paramref name="x1"/>, <paramref name="y1"/>) lands: on
    /// the floor under its end, a hair back from what stopped it, at the bottom of that open cell.
    /// </summary>
    private (int X, int Y) Landing(int x0, int y0, int x1, int y1)
    {
        int x = Down(x1 - Math.Sign(x1 - x0), Unit), y = Down(y1 - Math.Sign(y1 - y0), Unit);
        return (x1 - Math.Sign(x1 - x0), (FloorUnder(x, y) + 1) * Unit - 1);
    }

    /// <summary>
    /// Each arc a tick older, those whose lingering is over gone, a lob laying its patch where it ends as it lands; before the creatures
    /// strike this tick's.
    /// </summary>
    private void Linger()
    {
        var arcs = State.Arcs;
        foreach (var arc in arcs)
        {
            arc.Left -= 1;
            if (arc.Left <= 0 && arc.Leaves is { } leaves) LayPatch(leaves, Down(arc.X1, Unit), Down(arc.Y1, Unit), floor: true);
        }
        arcs.RemoveAll(a => a.Left <= 0);
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hero being crossed by arc <paramref name="id"/> (<see cref="Bolted"/>) would be refused: it
    /// lingers no longer, was never struck, hurt that hero already, or is a lob, which hurts only by the patch it lays (<c>creatures.gone</c>).
    /// What the client asks before it sends.
    /// </summary>
    public string? BoltedProblem(int peer, int id) =>
        ArcIndex(id) is int at && State.Arcs[at] is { Leaves: null } arc && arc.Paid?.Contains(peer) != true ? null : "creatures.gone";

    private void Apply(Bolted c)
    {
        var arc = State.Arcs[ArcIndex(c.Arc)!.Value];
        // It may take the suit's last unit: the hero passes out, as a ram would leave it.
        _crewOps.Blow(c.From, Content.Shots.DamageOf(null, arc.Projectile), Harm.Shock);
        arc.Paid ??= [];
        arc.Paid.Add(c.From);
    }

    /// <summary>Where arc <paramref name="id"/> is among the lingering, in id order; null for none.</summary>
    private int? ArcIndex(int id)
    {
        var arcs = State.Arcs;
        int lo = 0, hi = arcs.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (arcs[mid].Id == id) return mid;
            if (arcs[mid].Id < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }
}
