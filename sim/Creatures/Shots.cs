using Stillwake.Sim.Items;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Fires the sender's gun <c>Tool</c> (<c>Content.Shots</c>) from its muzzle at (<c>X</c>, <c>Y</c>), in thousandths of a cell,
/// along <c>Heading</c> (<see cref="Content.Headings"/>): one of its ammo spent, none in creative, and a shot flying from the next
/// step on (shots-are-the-sims). Refused with no such gun carried (<c>creatures.noGun</c>), the muzzle off the grid
/// (<c>creatures.blocked</c>) and no ammo (<c>items.notEnough</c>). Where the muzzle is and how often it fires is the client's
/// word (0034).
/// </summary>
public sealed record Fire(string Tool, int X, int Y, int Heading) : Command;

/// <summary>
/// Shots (shots-are-the-sims): fired by heroes' guns, flown by the sim, a tick at a time, meeting the first thing in their way:
/// a solid cell, then a creature, then a wreck.
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>The shots that ended this tick, in id order; derived, emptied as the next tick's shots fly.</summary>
    private readonly List<ShotEnd> _ended = [];

    /// <summary>The shots in flight, in id order; live and read-only.</summary>
    public IReadOnlyList<Shot> Shots => State.Shots;

    /// <summary>The shots that ended this tick, where each met something or ran out: what the client shows an end by.</summary>
    public IReadOnlyList<ShotEnd> Ended => _ended;

    /// <summary>Why peer <paramref name="peer"/>'s <see cref="Fire"/> of <paramref name="tool"/> from (<paramref name="x"/>, <paramref name="y"/>) would be refused.</summary>
    public string? FireProblem(int peer, string tool, int x, int y)
    {
        if (Content.Shots.GunOf(tool) is not { } gun || _held.ToolStack(peer, gun.Item) == null) return "creatures.noGun";
        if (x < 0 || y < 0 || x >= _grid.Width * Unit || y >= _grid.Height * Unit) return "creatures.blocked";
        return _held.ShortOf(peer, [(gun.Ammo, 1)]);
    }

    private void Apply(Fire c)
    {
        var gun = Content.Shots.GunOf(c.Tool)!;
        _items.Take(c.From, [(gun.Ammo, 1)]);
        var f = ShotFlight.Launch(gun.Projectile, c.X, c.Y, c.Heading);
        State.Fired += 1;
        State.Shots.Add(new Shot
        {
            Id = State.Fired,
            Projectile = gun.Projectile.Id,
            By = Ref.Hero(c.From),
            X = f.X,
            Y = f.Y,
            VX = f.VX,
            VY = f.VY,
            Left = f.Left,
        });
    }

    /// <summary>Every shot's step, in id order, those fired this tick too: one that met something hits it and ends, as one that ran out or left the grid.</summary>
    private void Fly()
    {
        _ended.Clear();
        var shots = State.Shots;
        int kept = 0;
        for (int i = 0; i < shots.Count; i++)
            if (Step(shots[i])) shots[kept++] = shots[i];
        shots.RemoveRange(kept, shots.Count - kept);
    }

    /// <summary>A tick of <paramref name="s"/>'s flight: true while it flies on.</summary>
    private bool Step(Shot s)
    {
        var p = Content.Shots.Get(s.Projectile);
        var (next, met) = ShotFlight.Step(s.Motion, ShotFlight.Gravity(Rules, p), _grid.Width, _grid.Height, cell => Stops(cell, p));
        if (met >= 0)
        {
            HitAt(s, p, met);
            _ended.Add(new ShotEnd(s.Id, s.Projectile, met));
            return false;
        }
        if (next.Left <= 0 || !ShotFlight.OnGrid(next, _grid.Width, _grid.Height))
        {
            _ended.Add(new ShotEnd(s.Id, s.Projectile, OnGridCell(next) ?? OnGridCell(s.Motion)!.Value));
            return false;
        }
        (s.X, s.Y, s.VY, s.Left) = (next.X, next.Y, next.VY, next.Left);
        return true;
    }

    /// <summary>The cell under <paramref name="f"/>'s point; null off the grid.</summary>
    private int? OnGridCell(ShotMotion f) =>
        ShotFlight.OnGrid(f, _grid.Width, _grid.Height) ? f.Y / Unit * _grid.Width + f.X / Unit : null;

    /// <summary>Whether a shot of <paramref name="p"/> stops in <paramref name="cell"/>: solid as for a body, or holding a creature or a wreck it may hit.</summary>
    private bool Stops(int cell, Content.Projectile p) =>
        Solid(cell % _grid.Width, cell / _grid.Width) || p.HitsBodies && (CreatureIn(cell) != null || _debris.WreckAt(cell) != null);

    /// <summary>
    /// <paramref name="s"/>'s hit in <paramref name="cell"/>, the first it meets there: what is solid takes its block damage, where tools
    /// break things; else the lowest-id creature in it takes its damage, knocked along the shot's way; else the first wreck.
    /// </summary>
    private void HitAt(Shot s, Content.Projectile p, int cell)
    {
        int peer = s.By.Kind == RefKind.Hero ? s.By.Id : FloorStack.Nobody;
        // Its yield to its shooter if its hero is on this surface; else it falls there, anyone's.
        int? here = s.By.Kind == RefKind.Hero && _crew.HeroOf(peer) != null ? peer : null;
        if (Solid(cell % _grid.Width, cell / _grid.Width))
        {
            if (p.BlockDamage > 0) _building.Hit(here, cell, p.BlockDamage);
            return;
        }
        if (CreatureIn(cell) is { } creature)
        {
            int away = s.VX != 0 ? Math.Sign(s.VX) : AwayFrom(creature, HeroCell(peer));
            // From a system: the debris has drifted this tick, so its clock already stamps this tick's blows.
            Hurt(creature, Spec.Get(creature.Species), p.Damage, new Hit(peer, away, Math.Min(Spec.KnockPace, p.Pace), _debris.Clock));
            return;
        }
        if (_debris.WreckAt(cell) is { } wreck) _debrisOps.Hit(peer, wreck.Id, p.Damage);
    }

    /// <summary>The lowest-id live creature whose body's box overlaps <paramref name="cell"/>; null for none.</summary>
    private Creature? CreatureIn(int cell)
    {
        int left = cell % _grid.Width * Unit, top = cell / _grid.Width * Unit;
        foreach (var c in State.Creatures)
        {
            if (c.Dead) continue;
            var body = Spec.Get(c.Species).Body;
            int half = body.Width * Unit / 2;
            if (c.X - half < left + Unit && c.X + half > left && c.Y - body.Height * Unit < top + Unit && c.Y > top) return c;
        }
        return null;
    }
}
