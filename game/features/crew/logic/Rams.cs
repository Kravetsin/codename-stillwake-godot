using System;
using System.Collections.Generic;
using Stillwake.Sim.Fauna;
using Spec = Stillwake.Content.Hero;
using Tune = Stillwake.Content.Fauna;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A creature's ram as this hero's client meets it (0103): plain C#, tested. The client is the word on contact, as on a strike's
/// reach (0034): its hero's body touching a creature the sim says charges sends <c>HitBy</c>, and the hero is thrown at once.
/// </summary>
public static class Rams
{
    /// <summary>How far past a creature's body it touches a hero, in thousandths of a cell: its fur, and a little of a tick's run.</summary>
    public const int Margin = 300;

    /// <summary>
    /// The first creature in id order charging (the sim's <see cref="Mode.Charge"/>) whose body and <see cref="Margin"/> touch
    /// <paramref name="hero"/>'s body; none for none.
    /// </summary>
    public static Creature? Contact(IReadOnlyList<Creature> all, Hero hero)
    {
        foreach (var c in all)
            if (c.Mode == Mode.Charge && Touches(c, hero, Margin)) return c;
        return null;
    }

    /// <summary>Whether <paramref name="c"/>'s body and <paramref name="margin"/> round it, in thousandths of a cell, touch <paramref name="hero"/>'s body.</summary>
    public static bool Touches(Creature c, Hero hero, int margin)
    {
        var spec = Tune.Get(c.Species);
        int half = spec.Width * Hero.Unit / 2 + margin;
        return c.X - half < hero.X + Spec.Width / 2 && c.X + half > hero.X - Spec.Width / 2
            && c.Y - spec.Height * Hero.Unit - margin < hero.Y && c.Y + margin > hero.Y - Spec.Height;
    }

    /// <summary>
    /// How near a friend a creature whose charge ended must be for that friend to be the one it hit, in thousandths of a cell: the
    /// friend is drawn a little behind, the creature a tick on past it.
    /// </summary>
    public const int FriendMargin = 2000;

    /// <summary>
    /// Which way across a ram by <paramref name="c"/> throws <paramref name="hero"/>: the way it charges, so a hero it reached past
    /// its middle is not thrown back into it; still, away from its middle.
    /// </summary>
    public static int Away(Creature c, Hero hero) => c.VX != 0 ? Math.Sign(c.VX) : hero.X >= c.X ? 1 : -1;

    /// <summary>
    /// <paramref name="hero"/> thrown <paramref name="away"/> by a ram of <paramref name="ram"/> units: off any ladder, its jump's
    /// timers spent, across and up by the ram (<c>Hero.KnockAcross</c>, <c>Hero.KnockUp</c>).
    /// </summary>
    public static Hero Knocked(Hero hero, int away, int ram) => hero with
    {
        Vx = away * (Spec.KnockAcross + Spec.KnockAcrossPer * ram),
        Vy = -(Spec.KnockUp + Spec.KnockUpPer * ram),
        Climbing = false,
        LetGo = 0,
        Airborne = Spec.Coyote + 1,
        Buffered = 0,
        Rising = false,
        Leaping = false,
    };

    /// <summary>
    /// A client's guard (0103): after its own hit it sends no other for <c>Hero.RamGuard</c> ticks, so one ram is one hit however
    /// long the bodies touch; only this client's, the sim keeps none.
    /// </summary>
    public sealed class Guard
    {
        private int _sentAt = int.MinValue / 2;

        /// <summary>Whether a hit may go out on world tick <paramref name="tick"/>.</summary>
        public bool Open(int tick) => tick - _sentAt >= Spec.RamGuard || tick < _sentAt;

        /// <summary>A hit went out on <paramref name="tick"/>.</summary>
        public void Sent(int tick) => _sentAt = tick;
    }
}
