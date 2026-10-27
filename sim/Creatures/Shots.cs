using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Fires the sender's gun <c>Tool</c> (<c>Content.Shots</c>) from its muzzle at (<c>X</c>, <c>Y</c>), in thousandths of a cell,
/// along <c>Heading</c> (<see cref="Content.Headings"/>): one of its ammo spent, none in creative, and a shot flying from the next
/// step on (shots-are-the-sims). Refused with no such gun carried (<c>creatures.noGun</c>), the muzzle off the grid
/// (<c>creatures.blocked</c>), no ammo (<c>items.notEnough</c>) and, for a gun that takes the belt's charge, too little of it
/// (<c>items.flat</c>), which it spends (2026-10-27-the-railgun). Where the muzzle is, how often it fires and how long it is held are
/// the client's word (0034).
/// </summary>
public sealed record Fire(string Tool, int X, int Y, int Heading) : Command;

/// <summary>A thing a shot flew on through, by what it is and its id (a creature's, a wreck's piece): what a client's copy skips after.</summary>
public readonly record struct ShotPass(ShotTarget What, int Id);

/// <summary>
/// A tick of a client's copy of a shot (<see cref="CreaturesModule.StepOf"/>): its motion next, the cell it met (-1 for none) or last
/// flew in, the hits it would take there and on the way, and everything it would have flown on through by then.
/// </summary>
public sealed record ShotTick(ShotMotion Next, int Met, int Last, IReadOnlyList<ShotHit> Hits, IReadOnlyList<ShotPass> Passed);

/// <summary>
/// Shots (shots-are-the-sims): fired by heroes' guns, flown by the sim, a tick at a time, meeting what is in their way: a solid
/// cell, then a creature, then a wreck; flying on through what a hit breaks or kills, their damage kept by their gun
/// (2026-10-27-shots-pierce).
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>The shots that ended this tick, in id order; derived, emptied as the next tick's shots fly.</summary>
    private readonly List<ShotEnd> _ended = [];

    /// <summary>The hits shots took this tick, in order; derived, emptied as the next tick's shots fly.</summary>
    private readonly List<ShotHit> _hits = [];

    /// <summary>The shots in flight, in id order; live and read-only.</summary>
    public IReadOnlyList<Shot> Shots => State.Shots;

    /// <summary>The shots that ended this tick, where each met something or ran out: what the client shows an end by.</summary>
    public IReadOnlyList<ShotEnd> Ended => _ended;

    /// <summary>Every hit a shot took this tick, in the order taken, the last of each ended shot's too: what the client shows a hit by.</summary>
    public IReadOnlyList<ShotHit> Hits => _hits;

    /// <summary>Why peer <paramref name="peer"/>'s <see cref="Fire"/> of <paramref name="tool"/> from (<paramref name="x"/>, <paramref name="y"/>) would be refused.</summary>
    public string? FireProblem(int peer, string tool, int x, int y)
    {
        if (Content.Shots.GunOf(tool) is not { } gun || _held.ToolStack(peer, gun.Item) == null) return "creatures.noGun";
        if (!ShotFlight.OnGrid(x, y, _grid.Width, _grid.Height)) return "creatures.blocked";
        if (AmmoOf(peer, gun) == null) return "items.notEnough";
        return gun.Energy > 0 && !_held.CanDrain(peer, gun.Energy) ? "items.flat" : null;
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
        if (gun.Energy > 0) _items.Drain(c.From, gun.Energy);
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
    /// Every shot's step, in id order, those fired this tick too: one that met something it did not break ends, as one that ran
    /// out or left the grid. The wrecks are taken once, the debris having drifted. Those its hits killed are swept out at the end,
    /// as the creatures' system sweeps a command's: the tick saves no creature dead and still listed.
    /// </summary>
    private void Fly()
    {
        _ended.Clear();
        _hits.Clear();
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
        var from = (s.X, s.Y);
        var (next, met, last) = ShotFlight.Step(s.Motion, ShotFlight.Gravity(Rules, p, Content.Shots.GunOf(s.Gun)), _grid.Width, _grid.Height, cell => Meets(s, p, cell, from));
        if (met >= 0)
        {
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

    /// <summary>
    /// What <paramref name="s"/> meets in <paramref name="cell"/>, as it walks there: every target in it in turn (<see cref="TargetIn"/>),
    /// past what it passed, each hit with what the shot has kept; it flies on through what it pierces (<see cref="Pierces"/>), what
    /// still stands then kept as passed, else it stops there, as it does at one it has nothing left for or that takes nothing,
    /// hitting nothing but listed, to show where it stopped. True where it stops.
    /// </summary>
    private bool Meets(Shot s, Content.Projectile p, int cell, (int X, int Y) from)
    {
        int peer = s.By.Kind == RefKind.Hero ? s.By.Id : FloorStack.Nobody;
        var gun = Content.Shots.GunOf(s.Gun);
        ShotPass? broken = null;
        while (TargetIn(cell, p, _wrecks, s.Passed) is { } target)
        {
            if (target.Pass == broken) throw new InvalidOperationException($"creatures.shot: {target.Pass} still in {cell} once broken");
            int damage = DamageOf(s.Gun, p, target.Pass.What, s.Pierced);
            bool takes = Takes(target.Pass, damage), broke = takes && HitAt(s, peer, target, damage);
            _hits.Add(new ShotHit(s.Id, s.By, s.Projectile, s.Gun, cell, from, target.Pass.What, takes ? damage : 0, broke));
            if (!Pierces(gun, takes, broke)) return true;
            s.Pierced += 1;
            if (broke) broken = target.Pass;
            else s.Passed.Add(target.Pass);
        }
        return false;
    }

    /// <summary>Whether a hit of <paramref name="damage"/> on <paramref name="target"/> takes anything: some left, and what is solid there breakable by a shot (<see cref="IBuildingQueries.ShotTakes"/>).</summary>
    private bool Takes(ShotPass target, int damage) => damage > 0 && (target.What != ShotTarget.Solid || _built.ShotTakes(target.Id));

    /// <summary>Whether a shot of <paramref name="gun"/> flies on through what its hit took (<paramref name="takes"/>) and broke or killed (<paramref name="broke"/>): everything it takes for a gun that pierces all, else what broke.</summary>
    private static bool Pierces(Content.Gun? gun, bool takes, bool broke) => gun?.Pierces == Content.Pierce.All ? takes : broke;

    /// <summary>What a shot of <paramref name="p"/> from <paramref name="gun"/> takes off <paramref name="what"/> after it <paramref name="pierced"/> that many: block damage off what is solid.</summary>
    private static int DamageOf(string? gun, Content.Projectile p, ShotTarget what, int pierced) => what == ShotTarget.Solid
        ? Content.Shots.BlockDamageOf(gun, p.Id, pierced)
        : Content.Shots.DamageOf(gun, p.Id, pierced);

    /// <summary>What a shot may hit in a cell: what it is, by <see cref="ShotPass"/>, and the creature, for one.</summary>
    private readonly record struct Target(ShotPass Pass, Creature? Creature);

    /// <summary>
    /// The first thing a shot of <paramref name="p"/> meets in <paramref name="cell"/>, past those in <paramref name="passed"/>: what is
    /// solid as for a body, else, for a projectile that hits bodies, the lowest-id live creature in it, else the first wreck of
    /// <paramref name="wrecks"/> still out; null for none.
    /// </summary>
    private Target? TargetIn(int cell, Content.Projectile p, IReadOnlyList<WreckBox> wrecks, IReadOnlyList<ShotPass> passed)
    {
        if (Solid(cell) && !passed.Contains(new ShotPass(ShotTarget.Solid, cell))) return new Target(new ShotPass(ShotTarget.Solid, cell), null);
        if (!p.HitsBodies) return null;
        if (CreatureIn(cell, passed) is { } creature) return new Target(new ShotPass(ShotTarget.Creature, creature.Id), creature);
        return WreckIn(cell, wrecks, passed) is { } wreck ? new Target(new ShotPass(ShotTarget.Wreck, wreck.Piece), null) : null;
    }

    /// <summary>
    /// <paramref name="s"/>'s hit of <paramref name="damage"/> on <paramref name="target"/>: what is solid takes it as block damage,
    /// where tools break things; a creature through the hit path, knocked by the gun's whole knock along the shot's way; a wreck
    /// through Debris. True when it broke or died.
    /// </summary>
    private bool HitAt(Shot s, int peer, Target target, int damage)
    {
        switch (target.Pass.What)
        {
            case ShotTarget.Solid:
                return _building.Hit(peer, target.Pass.Id, damage);
            case ShotTarget.Creature:
                var creature = target.Creature!;
                int away = s.VX != 0 ? Math.Sign(s.VX) : AwayFrom(creature, HeroCell(peer));
                // From a system: the debris has drifted this tick, so its clock already stamps this tick's blows.
                int knock = Math.Min(Spec.KnockPace, Content.Shots.GunOf(s.Gun)?.Knock ?? 0);
                return Hurt(creature, Spec.Get(creature.Species), damage, new Hit(peer, away, knock, _debris.Clock));
            default:
                return _debrisOps.Hit(peer, target.Pass.Id, damage);
        }
    }

    /// <summary>
    /// A tick of a client's copy of a shot of <paramref name="p"/> from <paramref name="gun"/> flying as <paramref name="f"/>, past
    /// what it has flown through, <paramref name="passed"/>, on this world as it stands: the sim's walk, judged by what each hit
    /// would do and changing nothing. A wreck or a creature its hit would break or kill it flies on through, and for a gun that
    /// pierces all whatever its hit would take; else what is solid stops it. Every pierce goes into <c>Passed</c>, which counts
    /// them, as <see cref="Shot.Pierced"/> does: a copy starts with none. For the shooter's client to fly its own shot ahead of the sim's (shots-are-the-sims, 2026-10-27-shots-pierce).
    /// </summary>
    public ShotTick StepOf(ShotMotion f, Content.Projectile p, string? gun, IReadOnlyList<ShotPass> passed)
    {
        var wrecks = p.HitsBodies && _debris.Pieces.Count > 0 ? _debris.Wrecks() : [];
        var hits = new List<ShotHit>();
        var through = new List<ShotPass>(passed);
        var from = (f.X, f.Y);
        var spec = Content.Shots.GunOf(gun);
        bool Judge(int cell)
        {
            while (TargetIn(cell, p, wrecks, through) is { } target)
            {
                int damage = DamageOf(gun, p, target.Pass.What, through.Count);
                bool takes = Takes(target.Pass, damage), breaks = takes && target.Pass.What switch
                {
                    ShotTarget.Creature => Dies(target.Creature!, damage),
                    ShotTarget.Wreck => _debris.Breaks(target.Pass.Id, damage),
                    _ => false,
                };
                hits.Add(new ShotHit(0, Ref.None, p.Id, gun, cell, from, target.Pass.What, takes ? damage : 0, breaks));
                if (!Pierces(spec, takes, breaks)) return true;
                through.Add(target.Pass);
            }
            return false;
        }
        var (next, met, last) = ShotFlight.Step(f, ShotFlight.Gravity(Rules, p, Content.Shots.GunOf(gun)), _grid.Width, _grid.Height, Judge);
        return new ShotTick(next, met, last, hits, through);
    }

    /// <summary>The wrecks a shot may meet this tick (<see cref="IDebrisQueries.Wrecks"/>), taken as the shots fly.</summary>
    private IReadOnlyList<WreckBox> _wrecks = [];

    /// <summary>The first wreck of <paramref name="wrecks"/>, in spawn order, still out and not in <paramref name="passed"/>, whose box covers part of <paramref name="cell"/>; null for none.</summary>
    private WreckBox? WreckIn(int cell, IReadOnlyList<WreckBox> wrecks, IReadOnlyList<ShotPass> passed)
    {
        foreach (var w in wrecks)
            if (w.Covers(cell, _grid.Width) && _debris.PieceById(w.Piece) != null && !passed.Contains(new ShotPass(ShotTarget.Wreck, w.Piece))) return w;
        return null;
    }

    /// <summary>The lowest-id live creature not in <paramref name="passed"/> whose body's box overlaps <paramref name="cell"/>; null for none.</summary>
    private Creature? CreatureIn(int cell, IReadOnlyList<ShotPass> passed)
    {
        int left = cell % _grid.Width * Unit, top = cell / _grid.Width * Unit;
        foreach (var c in State.Creatures)
        {
            if (c.Dead) continue;
            var body = Spec.Get(c.Species).Body;
            int half = body.Width * Unit / 2;
            if (c.X - half < left + Unit && c.X + half > left && c.Y - body.Height * Unit < top + Unit && c.Y > top
                && !passed.Contains(new ShotPass(ShotTarget.Creature, c.Id))) return c;
        }
        return null;
    }
}
