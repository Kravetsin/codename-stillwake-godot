using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The swarm (the breakers, companion-fights): a pack's one drive. It flies at the nearest hero out in space, to its own place of a
/// ring round it, and pounces at it when the pack's gate is open; knocked, it drifts till slow, then steers again. With no hero out
/// it eases to rest. What it keeps: when it last began a pounce (the clock plus one) and at whom (the peer plus one).
/// </summary>
internal sealed class SwarmDrive : DriveRule
{
    internal const string BeganAt = "beganAt", BeganFor = "beganFor";

    /// <summary>Its step while it drifts from a knock.</summary>
    internal const int Thrown = 1;

    /// <summary>The ring's eight places in thousandths of its radius, taken by id: opposite ones first, so a small pack surrounds.</summary>
    private static readonly (int X, int Y)[] Places =
        [(1000, 0), (-1000, 0), (0, -1000), (0, 1000), (707, -707), (-707, 707), (-707, -707), (707, 707)];

    internal override Type Serves => typeof(Spec.Swarm);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override bool Menaces => true;

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    internal override Gait GaitOf(Creature c) => Gait.Still;

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var swarm = (Spec.Swarm)d;
        if (c.Step == Thrown)
        {
            m.Slow(c, spec);
            if (Math.Max(Math.Abs(c.VX), Math.Abs(c.VY)) <= swarm.Speed / 4) c.Step = 0;
            return true;
        }
        const int unit = CreaturesModule.Unit;
        int x = c.X, y = c.Y - spec.Body.Height * unit / 2;
        if (m.NearestOut(CreaturesModule.Down(c.X, unit), CreaturesModule.Down(c.Y - 1, unit)) is not var (peer, at, distance))
        {
            m.Slow(c, spec);
            return true;
        }
        if (distance <= swarm.Strike && m.MayPounce(peer, swarm.Every) && m.InLine(c, spec, at))
        {
            c.Target = Ref.Hero(peer);
            c.Keep(BeganAt, m.Clock + 1);
            c.Keep(BeganFor, peer + 1);
            RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
            return true;
        }
        var place = Places[c.Id % Places.Length];
        long dx = m.Middle(at.X) + place.X * swarm.Ring - x, dy = m.Middle(at.Y) + place.Y * swarm.Ring - y;
        long far = Math.Max(Math.Abs(dx), Math.Abs(dy));
        // Slower over the last of its way, so it rests on its place rather than shaking about it; toward none.
        long speed = Math.Min(swarm.Speed, far / 4);
        int wantX = far == 0 ? 0 : (int)(dx * speed / far), wantY = far == 0 ? 0 : (int)(dy * speed / far);
        c.VX += Math.Clamp(wantX - c.VX, -swarm.Accel, swarm.Accel);
        c.VY += Math.Clamp(wantY - c.VY, -swarm.Accel, swarm.Accel);
        return true;
    }

    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        c.Target = Ref.None;
        if (m.Drifts(spec)) c.Step = Thrown;
        return true;
    }

    /// <summary>Its pounce over: off the hero it struck it drifts back the way it came, at half the dart, toward none.</summary>
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop)
    {
        c.Target = Ref.None;
        if (!hop) return;
        (c.VX, c.VY) = (-c.VX / 2, -c.VY / 2);
        c.Step = Thrown;
    }
}

public sealed partial class CreaturesModule
{
    /// <summary>
    /// Whether one of a pack may begin a pounce at peer <paramref name="peer"/>'s hero: none living began its last at it under
    /// <paramref name="every"/> ticks ago. One killed frees the next sooner.
    /// </summary>
    internal bool MayPounce(int peer, int every)
    {
        foreach (var c in State.Creatures)
            if (!c.Dead && c.Recall(SwarmDrive.BeganFor) == peer + 1 && Clock + 1 - c.Recall(SwarmDrive.BeganAt) < every) return false;
        return true;
    }
}
