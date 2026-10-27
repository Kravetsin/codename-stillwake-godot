using Stillwake.Sim.Debris;
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
/// The sender's hero was hit by creature shot <c>Shot</c> (the sentry's bolt), as its client saw them touch (the ADR: hits on a
/// hero are that hero's): its suit pays the projectile's damage and the shot ends, so one bolt hurts one hero. Refused for one no
/// longer in flight, or a gun's (<c>creatures.gone</c>). A bolt whose sentry has died flies and hurts on.
/// </summary>
public sealed record Bolted(int Shot) : Command;

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
        if (!ShotFlight.OnGrid(x, y, _grid.Width, _grid.Height)) return "creatures.blocked";
        return AmmoOf(peer, gun) == null ? "items.notEnough" : null;
    }

    /// <summary>
    /// What peer <paramref name="peer"/>'s <paramref name="gun"/> fires next: the first of its ammo the hero carries (any, the first,
    /// in creative); null for none. What the shooter's client flies its own shot as.
    /// </summary>
    public Content.Projectile? AmmoOf(int peer, Content.Gun gun)
    {
        foreach (string ammo in gun.Ammo)
            if (_held.ShortOf(peer, [(ammo, 1)]) == null) return Content.Shots.Get(ammo);
        return null;
    }

    private void Apply(Fire c)
    {
        var gun = Content.Shots.GunOf(c.Tool)!;
        var ammo = AmmoOf(c.From, gun)!;
        _items.Take(c.From, [(ammo.Id, 1)]);
        var f = ShotFlight.Launch(gun, c.X, c.Y, c.Heading);
        State.Fired += 1;
        State.Shots.Add(new Shot
        {
            Id = State.Fired,
            Projectile = ammo.Id,
            Gun = gun.Item,
            By = Ref.Hero(c.From),
            X = f.X,
            Y = f.Y,
            VX = f.VX,
            VY = f.VY,
            Left = f.Left,
        });
    }

    /// <summary>
    /// <paramref name="c"/>'s <paramref name="shoot"/> fired from its middle at the middle of cell (<paramref name="x"/>,
    /// <paramref name="y"/>): its speed on the longer axis, the other its share, rounded toward none, as a pounce's; no gun, so its
    /// damage is its projectile's alone; its flight's ticks rounded down. False, and nothing fired, with its target on its own middle.
    /// </summary>
    internal bool Launch(Creature c, Spec.Species spec, Spec.Shoot shoot, int x, int y)
    {
        int fromY = c.Y - spec.Body.Height * Unit / 2;
        int dx = Middle(x) - c.X, dy = Middle(y) - fromY, far = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (far == 0) return false;
        State.Fired += 1;
        State.Shots.Add(new Shot
        {
            Id = State.Fired,
            Projectile = shoot.Projectile,
            By = Ref.None,
            X = c.X,
            Y = fromY,
            VX = (int)((long)shoot.Speed * dx / far),
            VY = (int)((long)shoot.Speed * dy / far),
            Left = shoot.Reach * Unit / shoot.Speed,
        });
        return true;
    }

    /// <summary>
    /// Why the sender's hero being hit by shot <paramref name="id"/> (<see cref="Bolted"/>) would be refused: it is no creature's
    /// shot in flight, ended or never fired, or a gun's (<c>creatures.gone</c>). What the client asks before it sends.
    /// </summary>
    public string? BoltedProblem(int id) => ShotIndex(id) is int at && State.Shots[at].Gun == null ? null : "creatures.gone";

    private void Apply(Bolted c)
    {
        int at = ShotIndex(c.Shot)!.Value;
        var shot = State.Shots[at];
        // It may take the suit's last unit: the hero passes out, as a ram would leave it.
        _crewOps.Shock(c.From, Content.Shots.DamageOf(null, shot.Projectile));
        State.Shots.RemoveAt(at);
    }

    /// <summary>Where shot <paramref name="id"/> is among the shots in flight, in id order; null for none.</summary>
    private int? ShotIndex(int id)
    {
        var shots = State.Shots;
        int lo = 0, hi = shots.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (shots[mid].Id == id) return mid;
            if (shots[mid].Id < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <summary>
    /// Every shot's step, in id order, those fired this tick too: one that met something hits it and ends, as one that ran out or
    /// left the grid. The wrecks are taken once, the debris having drifted. Those its hits killed are swept out at the end, as the
    /// creatures' system sweeps a command's: the tick saves no creature dead and still listed.
    /// </summary>
    private void Fly()
    {
        _ended.Clear();
        var shots = State.Shots;
        if (shots.Count == 0) return;
        _wrecks = _debris.Pieces.Count > 0 ? _debris.Wrecks() : [];
        int kept = 0;
        for (int i = 0; i < shots.Count; i++)
            if (Step(shots[i])) shots[kept++] = shots[i];
        shots.RemoveRange(kept, shots.Count - kept);
        State.Creatures.RemoveAll(c => c.Dead);
    }

    /// <summary>A tick of <paramref name="s"/>'s flight: true while it flies on.</summary>
    private bool Step(Shot s)
    {
        var p = Content.Shots.Get(s.Projectile);
        var (next, met, last) = StepOf(s.Motion, p, _wrecks);
        if (met >= 0)
        {
            HitAt(s, p, met);
            _ended.Add(new ShotEnd(s.Id, s.Projectile, s.Gun, met, Met: true));
            return false;
        }
        if (next.Left <= 0 || !ShotFlight.OnGrid(next.X, next.Y, _grid.Width, _grid.Height))
        {
            _ended.Add(new ShotEnd(s.Id, s.Projectile, s.Gun, last, Met: false));
            return false;
        }
        (s.X, s.Y, s.VY, s.Left) = (next.X, next.Y, next.VY, next.Left);
        return true;
    }

    /// <summary>Whether a shot of <paramref name="p"/> stops in <paramref name="cell"/>: solid as for a body, or holding a creature or a wreck it may hit.</summary>
    private bool Stops(int cell, Content.Projectile p, IReadOnlyList<WreckBox> wrecks) =>
        Solid(cell) || p.HitsBodies && (CreatureIn(cell) != null || WreckIn(cell, wrecks) != null);

    /// <summary>A tick of a shot of <paramref name="p"/> flying as <paramref name="f"/> (<see cref="ShotFlight.Step"/>), meeting what <paramref name="wrecks"/> holds.</summary>
    private (ShotMotion Next, int Met, int Last) StepOf(ShotMotion f, Content.Projectile p, IReadOnlyList<WreckBox> wrecks) =>
        ShotFlight.Step(f, ShotFlight.Gravity(Rules, p), _grid.Width, _grid.Height, cell => Stops(cell, p, wrecks));

    /// <summary>
    /// A tick of a shot of <paramref name="p"/> flying as <paramref name="f"/>, on this world as it stands: the step the sim flies it by,
    /// for the shooter's client to fly its own shot ahead of the sim's (shots-are-the-sims).
    /// </summary>
    public (ShotMotion Next, int Met, int Last) StepOf(ShotMotion f, Content.Projectile p) =>
        StepOf(f, p, p.HitsBodies && _debris.Pieces.Count > 0 ? _debris.Wrecks() : []);

    /// <summary>
    /// <paramref name="s"/>'s hit in <paramref name="cell"/>, the first it meets there, its gun's and its own added: what is solid takes
    /// its block damage, where tools break things; else the lowest-id creature in it takes its damage, knocked by its gun along the
    /// shot's way; else the first wreck.
    /// </summary>
    private void HitAt(Shot s, Content.Projectile p, int cell)
    {
        int peer = s.By.Kind == RefKind.Hero ? s.By.Id : FloorStack.Nobody;
        if (Solid(cell))
        {
            if (Content.Shots.BlockDamageOf(s.Gun, p.Id) is int block and > 0) _building.Hit(peer, cell, block);
            return;
        }
        if (CreatureIn(cell) is { } creature)
        {
            int away = s.VX != 0 ? Math.Sign(s.VX) : AwayFrom(creature, HeroCell(peer));
            // From a system: the debris has drifted this tick, so its clock already stamps this tick's blows.
            int knock = Math.Min(Spec.KnockPace, Content.Shots.GunOf(s.Gun)?.Knock ?? 0);
            Hurt(creature, Spec.Get(creature.Species), Content.Shots.DamageOf(s.Gun, p.Id), new Hit(peer, away, knock, _debris.Clock));
            return;
        }
        if (WreckIn(cell, _wrecks) is { } wreck) _debrisOps.Hit(peer, wreck.Piece, Content.Shots.DamageOf(s.Gun, p.Id));
    }

    /// <summary>The wrecks a shot may meet this tick (<see cref="IDebrisQueries.Wrecks"/>), taken as the shots fly.</summary>
    private IReadOnlyList<WreckBox> _wrecks = [];

    /// <summary>The first wreck of <paramref name="wrecks"/>, in spawn order, still out, whose box covers part of <paramref name="cell"/>; null for none.</summary>
    private WreckBox? WreckIn(int cell, IReadOnlyList<WreckBox> wrecks)
    {
        foreach (var w in wrecks)
            if (w.Covers(cell, _grid.Width) && _debris.PieceById(w.Piece) != null) return w;
        return null;
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
