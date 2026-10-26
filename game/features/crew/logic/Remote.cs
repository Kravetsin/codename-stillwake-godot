using System;
using System.Collections.Generic;
using System.IO;
using Stillwake.Net;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero as others see it: its movement, the turn of its head toward its pointer, whether it holds the
/// plan, the item it wields (an item's id, such as the fire axe's), what its hands are busy with and its
/// harpoon's rod while out of the gun. Only drawn, never part of the world.
/// </summary>
public readonly record struct Seen(Hero Hero, double? Look = null, bool HoldsPlan = false, string? Wields = null, Busy? Busy = null, RodSeen? Rod = null);

/// <summary>
/// A hero's harpoon rod out of the gun, as others see it (<see cref="HarpoonShot.Seen"/>): where it is,
/// <see cref="Rod"/>, and <see cref="At"/>, in thousandths of a cell, a point that holds for the whole state, so
/// a rod adds no sends of its own: flying, the end of the range it flies toward; stuck in a <see cref="Piece"/>,
/// where it sticks from the piece's middle; stuck in the structure, where it sticks; none on the way back.
/// Others play it out from these (<see cref="RemoteRod"/>, 0065).
/// </summary>
public readonly record struct RodSeen(Rod Rod, Point? At = null, int? Piece = null);

/// <summary>What a hero's hands work: a plan with the screwdriver (0030), a wreck with what they wield (0034), plans beamed in by the planner, the drill running aimed at a point (0054).</summary>
public enum Handwork { Work, Chop, Beam, Drill }

/// <summary>
/// What a hero's hands are busy with, and the point they work at, in thousandths of a cell as the hero's
/// own position; chopping, the point its latest swing is aimed at (<see cref="AxeSwing.At"/>) and how many
/// swings it has begun (<see cref="AxeSwing.Count"/>), so others swing once for each, where it did.
/// </summary>
public readonly record struct Busy(Handwork Work, Point At, int Swings = 0)
{
    /// <summary>Whether <paramref name="now"/> is a beam not shown yet: a beam is sent for as long as it fades, and drawn once.</summary>
    public static bool BeamsAnew(Busy? was, Busy? now) => now is { Work: Handwork.Beam } && now != was;

    /// <summary>
    /// The chop to follow another's hero by once <paramref name="now"/> is heard: a new one, else the
    /// <paramref name="last"/>, kept while its hands are idle, so a swing sent as it ended is still swung,
    /// at its point (0038, 0039).
    /// </summary>
    public static Busy? Chopping(Busy? last, Busy? now) => now is { Work: Handwork.Chop } ? now : last;
}

/// <summary>A hero as sent (<c>SentHero</c> of remote.ts): stamped with the time of the controller step it stands at, on its own clock.</summary>
public readonly record struct SentHero(Seen Seen, double? At = null)
{
    private sealed record Wire(
        int X, int Y, int Vx, int Vy, bool Climbing, int Facing, Point? Anchor, double? Look, bool Plan, double? At,
        string? Wields, Busy? Busy, RodSeen? Harpoon);

    /// <summary>What <c>Client.SendHero</c> carries, packed (<see cref="Packing"/>): net never reads it.</summary>
    public byte[] Encode()
    {
        var h = Seen.Hero;
        return Packing.Pack(new Wire(h.X, h.Y, h.Vx, h.Vy, h.Climbing, h.Facing, h.Anchor, Seen.Look, Seen.HoldsPlan, At, Seen.Wields, Seen.Busy, Seen.Rod));
    }

    /// <summary>A hero another peer sent, as <see cref="Encode"/> wrote it; none for what it did not write.</summary>
    public static SentHero? Decode(ReadOnlySpan<byte> state)
    {
        try
        {
            if (Packing.Unpack<Wire>(state) is not { Facing: 1 or -1 } w) return null;
            return new SentHero(new Seen(new Hero(w.X, w.Y, w.Vx, w.Vy, w.Climbing, w.Facing, w.Anchor), w.Look, w.Plan, w.Wields, w.Busy, w.Harpoon), w.At);
        }
        catch (InvalidDataException)
        {
            // A peer of the same build writes nothing else; the dev runs' --auto beats carry no hero.
            return null;
        }
    }
}

/// <summary>
/// Other peers' heroes as they arrive, and where to draw each: between the two samples around
/// <see cref="BehindMs"/> ago (features/crew/remote.ts). Samples are placed by the sender's stamps, not by
/// when they arrive, so a link that delivers them unevenly does not make a hero speed up and slow down.
/// Times are this machine's milliseconds, counted by the caller.
/// </summary>
public sealed class RemoteHeroes
{
    /// <summary>A hero is sent this often while it moves: 20 times a second (0002).</summary>
    public const double SendMs = 50;
    /// <summary>And this often standing still, so a peer that joins late sees it.</summary>
    public const double BeatMs = 1000;
    /// <summary>Others' heroes are drawn this far behind their newest news: three sends of slack, for the jitter of a link across the internet.</summary>
    public const double BehindMs = 150;
    /// <summary>
    /// How long a hero walks on along its last velocity once its samples run out, before it stands: a
    /// short gap in delivery stays unseen, a long one shows as a stop rather than a figure frozen mid-stride (0013).
    /// </summary>
    public const double ExtrapolateMs = 100;
    /// <summary>A hero silent this long has gone, its peer with it.</summary>
    public const double GoneMs = 3000;
    /// <summary>Farther than this between two samples is a jump, such as waking at the spawn, not a walk.</summary>
    private const int Snap = 6 * Hero.Unit;
    /// <summary>How fast the estimate of a peer's clock may slip later, per millisecond heard: a path that grows slower is followed, slowly.</summary>
    private const double Slip = 0.01;
    /// <summary>A sample this much later than the estimate says starts it over: the sender was asleep.</summary>
    private const double RestartMs = 500;

    private readonly record struct Sample(double At, Seen Seen);

    private sealed class Peer
    {
        public readonly List<Sample> Samples = [];
        /// <summary>This machine's clock less the sender's, as the quickest sample so far says; with the path.</summary>
        public double? Offset;
        /// <summary>When it was last heard from, on this machine's clock.</summary>
        public double Heard;
        /// <summary>The sender's stamp of the last sample.</summary>
        public double? SentAt;
    }

    // Peers in the order first heard: the order they are drawn in.
    private readonly List<(int Id, Peer Peer)> _peers = [];

    public void Add(int from, SentHero sent, double arrived)
    {
        int index = _peers.FindIndex(p => p.Id == from);
        if (index < 0)
        {
            _peers.Add((from, new Peer()));
            index = _peers.Count - 1;
        }
        var peer = _peers[index].Peer;
        double at = arrived;
        if (sent.At is double stamp)
        {
            double offset = arrived - stamp;
            double? slipped = peer.Offset is double o ? o + (arrived - peer.Heard) * Slip : null;
            peer.Offset = slipped is not double s || offset - s > RestartMs ? offset : Math.Min(s, offset);
            at = stamp + peer.Offset.Value;
        }
        // A hero standing still is sent only on the beat: it stood where it was heard last until a send
        // ago, so it sets off from there instead of sliding all the way from the last beat.
        if (peer.Samples.Count > 0)
        {
            var last = peer.Samples[^1];
            double gap = sent.At is double st && peer.SentAt is double was ? st - was : at - last.At;
            if (gap > 2 * SendMs) peer.Samples.Add(new Sample(at - SendMs, last.Seen));
        }
        peer.Samples.Add(new Sample(at, sent.Seen));
        peer.Heard = arrived;
        peer.SentAt = sent.At;
    }

    /// <summary>Each hero heard from lately, by peer id in arrival order, where it stood <see cref="BehindMs"/> ago.</summary>
    public List<(int Id, Seen Seen)> At(double now)
    {
        var shown = new List<(int, Seen)>();
        double then = now - BehindMs;
        foreach (var (id, peer) in _peers)
        {
            var list = peer.Samples;
            if (list.Count == 0 || now - peer.Heard > GoneMs) continue;
            // Samples before the one at or before `then` are needed no more.
            int i = 0;
            while (i + 1 < list.Count && list[i + 1].At <= then) i++;
            list.RemoveRange(0, i);
            shown.Add((id, Between(list[0], list.Count > 1 ? list[1] : null, then)));
        }
        return shown;
    }

    private static Seen Between(Sample a, Sample? next, double then)
    {
        if (then <= a.At) return a.Seen;
        if (next is not Sample b) return Onwards(a, then - a.At);
        var (ha, hb) = (a.Seen.Hero, b.Seen.Hero);
        if (Math.Abs(hb.X - ha.X) > Snap || Math.Abs(hb.Y - ha.Y) > Snap) return a.Seen;
        double t = (then - a.At) / (b.At - a.At);
        double? look = a.Seen.Look is double la && b.Seen.Look is double lb ? la + (lb - la) * t : a.Seen.Look;
        var hero = ha with { X = (int)Math.Round(ha.X + (hb.X - ha.X) * t), Y = (int)Math.Round(ha.Y + (hb.Y - ha.Y) * t) };
        return a.Seen with { Hero = hero, Look = look };
    }

    /// <summary>Where a hero <paramref name="late"/> ms past its newest sample is: walking on for a while, then standing there.</summary>
    private static Seen Onwards(Sample sample, double late)
    {
        var h = sample.Seen.Hero;
        double steps = Math.Min(late, ExtrapolateMs) / Hero.StepMs;
        var moved = h with { X = (int)Math.Round(h.X + h.Vx * steps), Y = (int)Math.Round(h.Y + h.Vy * steps) };
        return sample.Seen with { Hero = late > ExtrapolateMs ? moved with { Vx = 0, Vy = 0 } : moved };
    }
}

/// <summary>
/// When to send this machine's hero (<c>createHeroSender</c> of remote.ts): every <see cref="RemoteHeroes.SendMs"/>
/// if it changed since last sent, and every <see cref="RemoteHeroes.BeatMs"/> whatever it does.
/// </summary>
public sealed class HeroSender(Action<SentHero> send)
{
    /// <summary>A turn of the head smaller than this, in radians, is not worth a send.</summary>
    private const double LookStep = 0.03;

    private double _sinceSent = double.PositiveInfinity;
    private Seen? _sent;

    /// <summary>
    /// Takes the real time passed, the hero now, and the time of the controller step it stands at, which
    /// goes with it as its stamp. Its harpoon's rod changing goes at once, so others play the rod out from
    /// the moment it fired, stuck or came free (<see cref="RemoteRod"/>).
    /// </summary>
    public void Step(double elapsedMs, Seen seen, double at)
    {
        _sinceSent += elapsedMs;
        bool rodNews = _sent is { } before && before.Rod != seen.Rod;
        if (_sinceSent < RemoteHeroes.SendMs && !rodNews) return;
        bool moved = _sent is not { } was
            // Speed too: a hero that stops is sent standing, not left mid-stride until the beat. A
            // tether tied or let go, the plan or a tool taken out, work begun or ended: news even standing still.
            || was with { Look = seen.Look } != seen
            || Math.Abs((was.Look ?? 0) - (seen.Look ?? 0)) > LookStep;
        if (!moved && _sinceSent < RemoteHeroes.BeatMs) return;
        send(new SentHero(seen, at));
        _sent = seen;
        _sinceSent = 0;
    }
}
