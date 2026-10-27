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
        long run = Middle(cell % _grid.Width) - x, rise = Middle(cell / _grid.Width) - y, far = Math.Max(Math.Abs(run), Math.Abs(rise));
        if (far == 0) return false;
        long reach = (long)shoot.Reach * Unit;
        if (far > reach) (run, rise) = (run * reach / far, rise * reach / far);
        // Where it enters the first solid cell, as a share num / den of the way; the whole way with none.
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
        State.Fired += 1;
        State.Arcs.Add(new Arc
        {
            Id = State.Fired,
            Projectile = shoot.Projectile,
            X0 = x,
            Y0 = y,
            X1 = (int)(x + run * num / den),
            Y1 = (int)(y + rise * num / den),
            Left = shoot.Lingers,
        });
        return true;
    }

    /// <summary>Each arc a tick older, those whose lingering is over gone; before the creatures strike this tick's.</summary>
    private void Linger()
    {
        var arcs = State.Arcs;
        foreach (var arc in arcs) arc.Left -= 1;
        arcs.RemoveAll(a => a.Left <= 0);
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hero being crossed by arc <paramref name="id"/> (<see cref="Bolted"/>) would be refused: it
    /// lingers no longer, was never struck, or hurt that hero already (<c>creatures.gone</c>). What the client asks before it sends.
    /// </summary>
    public string? BoltedProblem(int peer, int id) =>
        ArcIndex(id) is int at && State.Arcs[at].Paid?.Contains(peer) != true ? null : "creatures.gone";

    private void Apply(Bolted c)
    {
        var arc = State.Arcs[ArcIndex(c.Arc)!.Value];
        // It may take the suit's last unit: the hero passes out, as a ram would leave it.
        _crewOps.Shock(c.From, Content.Shots.DamageOf(null, arc.Projectile));
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
