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
