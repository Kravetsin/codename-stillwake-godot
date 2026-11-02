using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>How a creature's drive in charge moves its body: standing, walking or running.</summary>
internal enum Gait { Still, Walk, Run }

/// <summary>
/// A hit taking hold (2026-10-26-creatures-are-composed): by peer <paramref name="Peer"/>'s hero, the way across away from it,
/// the weapon's pace (0098), and <paramref name="Answered"/>, the debris clock through which a blow on its wreck is answered with
/// it: the clock and one from a command, which lands before the debris drifts, the clock from a system, which lands after.
/// </summary>
internal readonly record struct Hit(int Peer, int Away, int Pace, int Answered);

/// <summary>
/// The rule of one kind of drive (<see cref="Spec.Drive"/>). Rules hold no state: what a creature needs remembered is on it.
/// Every hook gets the drive's record and its place <c>at</c> in the brain.
/// </summary>
internal abstract class DriveRule
{
    /// <summary>The record it serves.</summary>
    internal abstract Type Serves { get; }

    /// <summary>Whether it takes charge, asked from the top when the drive in charge yields; never draws.</summary>
    internal virtual bool Wants(Creature c, Spec.Drive d) => false;

    /// <summary>Takes charge after one yielded; may draw.</summary>
    internal virtual void Enter(CreaturesModule m, Creature c, Spec.Drive d) { }

    /// <summary>Takes charge of a creature just let out or spawned; never draws.</summary>
    internal virtual void Born(Creature c, Spec.Drive d) { }

    /// <summary>The drive in charge, every tick before its clock, no act under way: false yields.</summary>
    internal virtual bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    /// <summary>A reflex: every tick, for every drive of the brain, whatever is in charge or under way.</summary>
    internal virtual void Reflex(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) { }

    /// <summary>Its ticks ran out: false yields, so the brain asks again from the top.</summary>
    internal abstract bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d);

    /// <summary>Told of a hit, in brain order till one claims the creature: whether it did.</summary>
    internal virtual bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit) => false;

    /// <summary>The body can go no further <paramref name="way"/> and found no leap: whether it began an act instead of turning.</summary>
    internal virtual bool Blocked(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way) => false;

    /// <summary>An act it began has ended, the creature to go <paramref name="way"/>, hopping back when <paramref name="hop"/>.</summary>
    internal virtual void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) { }

    /// <summary>Whether a creature in its charge may turn on a hero: it threatens heroes near it (<c>Threatens</c>).</summary>
    internal virtual bool Menaces => false;

    internal abstract Gait GaitOf(Creature c);

    /// <summary>How it shows: by its gait unless it says more.</summary>
    internal virtual Posture PostureOf(Creature c) => GaitOf(c) switch { Gait.Walk => Posture.Wander, Gait.Run => Posture.Flee, _ => Posture.Idle };
}

/// <summary>The rule of one kind of body (<see cref="Spec.Shape"/>): how it stands, steers and moves a tick.</summary>
internal abstract class BodyRule
{
    internal abstract Type Serves { get; }

    /// <summary>Whether it spent the tick getting free of something built into it since.</summary>
    internal abstract bool Unstick(CreaturesModule m, Creature c, Spec.Shape s);

    internal abstract bool Grounded(CreaturesModule m, Creature c, Spec.Shape s);

    /// <summary>Why one could not be let out in <paramref name="cell"/> (<see cref="Release"/>): its refusal, or null.</summary>
    internal abstract string? ReleaseProblem(CreaturesModule m, Spec.Species spec, int cell);

    /// <summary>Puts <paramref name="c"/>, just let out, in <paramref name="cell"/>, where <see cref="ReleaseProblem"/> found room.</summary>
    internal abstract void Place(CreaturesModule m, Creature c, Spec.Species spec, int cell);

    /// <summary>Its steering by the drive or act in charge, then its move.</summary>
    internal abstract void Step(CreaturesModule m, Creature c, Spec.Species spec, bool grounded);
}

/// <summary>The rule of one kind of move (<see cref="Spec.Move"/>): an act in phases.</summary>
internal abstract class MoveRule
{
    internal abstract Type Serves { get; }

    /// <summary>Begins move <paramref name="at"/> of the species.</summary>
    internal abstract void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at);

    /// <summary>A tick of its clock, its phase moving on when it runs out.</summary>
    internal abstract void Count(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv, bool grounded);

    /// <summary>Its steering, on the ground.</summary>
    internal abstract void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv);

    /// <summary>Whether a blow cuts it short.</summary>
    internal virtual bool Interruptible => true;

    /// <summary>Whether a hero may be hit by it now (<see cref="HitBy"/>).</summary>
    internal abstract bool Hits(Act a);

    /// <summary>Whether the body walks its way under it: kept against a wall till its steering turns it.</summary>
    internal abstract bool Walks(Act a);

    /// <summary>Whether a walker making it flies now: no gravity, no steps, its act over at the first solid cell on its way.</summary>
    internal virtual bool Flies(Spec.Species spec, Act a) => false;

    /// <summary>How it shows: crouched, then under way.</summary>
    internal virtual Posture PostureOf(Act a) => a.Phase == ActPhase.Active ? Posture.Charge : Posture.WindUp;
}

/// <summary>
/// Every rule, found once by reflection over this assembly and kept by the record type it serves: a lookup, never walked, so no
/// order reaches the simulation and adding a rule edits no list. A record served twice fails here; one served by none, in
/// <c>CreatureContractTests</c>.
/// </summary>
internal static class RuleBook
{
    private static readonly Dictionary<Type, DriveRule> Drives = Find<DriveRule>(r => r.Serves);
    private static readonly Dictionary<Type, BodyRule> Bodies = Find<BodyRule>(r => r.Serves);
    private static readonly Dictionary<Type, MoveRule> Moves = Find<MoveRule>(r => r.Serves);

    internal static DriveRule Drive(Spec.Drive d) => Drives[d.GetType()];

    internal static BodyRule Body(Spec.Shape s) => Bodies[s.GetType()];

    internal static MoveRule Move(Spec.Move mv) => Moves[mv.GetType()];

    /// <summary>Whether some rule serves the record type <paramref name="record"/>.</summary>
    internal static bool Serves(Type record) => Drives.ContainsKey(record) || Bodies.ContainsKey(record) || Moves.ContainsKey(record);

    /// <summary>Whether <paramref name="c"/> is in flight: a drive in charge that runs, no act under way.</summary>
    internal static bool FleeingOf(Creature c) => c.Act == null && Drive(Spec.Get(c.Species).Brain[c.Drive]).GaitOf(c) == Gait.Run;

    internal static Posture PostureOf(Creature c)
    {
        var spec = Spec.Get(c.Species);
        return c.Act is { } act ? Move(spec.Moves[act.Move]).PostureOf(act) : Drive(spec.Brain[c.Drive]).PostureOf(c);
    }

    private static Dictionary<Type, T> Find<T>(Func<T, Type> serves) where T : class
    {
        var rules = new Dictionary<Type, T>();
        foreach (var type in typeof(RuleBook).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(T).IsAssignableFrom(type)) continue;
            var rule = (T)Activator.CreateInstance(type, nonPublic: true)!;
            if (!rules.TryAdd(serves(rule), rule)) throw new InvalidOperationException($"creatures: two rules serve {serves(rule).Name}");
        }
        return rules;
    }
}
