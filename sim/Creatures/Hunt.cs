using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The hunt (the catcher, tracker-catchers): still its warning where it came, then flying straight at its quarry's reported cell, its
/// flyer's body cutting through grown rock, its first move begun at it within its strike in line; out of time it calls the hunt
/// off and leaves straight up through anything. Its quarry out, gone or off the surface, crew has reset the meter: it leaves.
/// A blow from another hero it answers with its second move, at most every <c>AnswerEvery</c> ticks, then turns back. What it
/// keeps, in its <c>Mem</c>: its quarry (its peer plus one), the clocks its warning and its hunt end at, and of its next answer.
/// </summary>
internal sealed class HuntDrive : DriveRule
{
    internal const string Quarry = "quarry", WarnTill = "warnTill", HuntTill = "huntTill", AnswerAt = "answerAt";

    internal override Type Serves => typeof(Spec.Hunt);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override bool Menaces => true;

    /// <summary><paramref name="c"/>, just come, starts hunting <paramref name="peer"/>'s hero: its warning from now.</summary>
    internal static void Start(CreaturesModule m, Creature c, Spec.Species spec, int peer)
    {
        var hunt = spec.Brain.OfType<Spec.Hunt>().Single();
        c.Keep(Quarry, peer + 1);
        c.Keep(WarnTill, m.Clock + hunt.Warn);
        c.Keep(HuntTill, m.Clock + hunt.Warn + hunt.Ticks);
        c.Target = Ref.Hero(peer);
    }

    /// <summary>The peer whose hero <paramref name="c"/> hunts; null for one that hunts none.</summary>
    internal static int? QuarryOf(Creature c) => c.Recall(Quarry) is int q and > 0 ? q - 1 : null;

    /// <summary>Whether <paramref name="c"/> has left the hunt, flying off through anything.</summary>
    internal static bool Leaving(Creature c) => c.Recall(FlyerBody.Through) == 1;

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var hunt = (Spec.Hunt)d;
        if (Leaving(c))
        {
            (c.VX, c.VY) = (0, -hunt.Speed);
            return true;
        }
        if (QuarryOf(c) is not int peer || m.HeroAt(peer, awake: true) is not { } at)
        {
            Leave(c);
            return true;
        }
        if (m.Clock >= c.Recall(HuntTill))
        {
            m.CallOff(peer);
            Leave(c);
            return true;
        }
        if (m.Clock < c.Recall(WarnTill))
        {
            (c.VX, c.VY) = (0, 0);
            return true;
        }
        int middle = c.Y - spec.Body.Height * Unit / 2;
        int far = Math.Max(Math.Abs(at.X - Down(c.X, Unit)), Math.Abs(at.Y - Down(middle, Unit)));
        if (far <= hunt.Strike && m.InLine(c, spec, at) && RuleBook.Move(spec.Moves[0]).Ready(m, c, spec.Moves[0]))
        {
            RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
            return true;
        }
        // Its speed on the longer axis, the other its share, rounded toward none, as a pounce flies.
        int dx = m.Middle(at.X) - c.X, dy = m.Middle(at.Y) - middle, most = Math.Max(Math.Abs(dx), Math.Abs(dy));
        (c.VX, c.VY) = most == 0 ? (0, 0) : ((int)((long)hunt.Speed * dx / most), (int)((long)hunt.Speed * dy / most));
        return true;
    }

    /// <summary>Its hunt over: it flies off through anything, its quarry let be.</summary>
    private static void Leave(Creature c)
    {
        c.Keep(FlyerBody.Through, 1);
        c.Target = Ref.None;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    /// <summary>
    /// Struck by a hero not its quarry, free of any move, hunting and with its answer ready: its second move at the striker. Every
    /// blow is claimed, so nothing else answers.
    /// </summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        if (hit.Peer == QuarryOf(c) || c.Act != null || Leaving(c) || m.Clock < c.Recall(AnswerAt) || m.HeroAt(hit.Peer, awake: true) == null) return true;
        c.Keep(AnswerAt, m.Clock + ((Spec.Hunt)d).AnswerEvery);
        c.Target = Ref.Hero(hit.Peer);
        RuleBook.Move(spec.Moves[1]).Begin(m, c, spec, 1);
        return true;
    }

    /// <summary>A move over: it hangs where it stopped, turned back to its quarry.</summary>
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop)
    {
        (c.VX, c.VY) = (0, 0);
        c.Target = QuarryOf(c) is int peer && !Leaving(c) ? Ref.Hero(peer) : Ref.None;
    }

    internal override Gait GaitOf(Creature c) => Gait.Walk;
}
