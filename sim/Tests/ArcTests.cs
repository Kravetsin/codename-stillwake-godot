using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>The sentry's arc (2026-10-27-sentry-arc): struck at once past its strike, it hurts each hero whose client says it crossed them.</summary>
public class ArcTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly Tune.Guard Guard = Tune.Sentry.Brain.OfType<Tune.Guard>().Single();
    private static readonly Tune.Shoot Shoot = (Tune.Shoot)Tune.Sentry.Moves[1];
    private static readonly int Damage = Spec.Shots.Bolt.Damage;

    /// <summary>The ticks from a hero entering thirty cells off to its sentry's arc: its alert, then its wind-up, begun as the alert ends.</summary>
    private static readonly int ToArc = Guard.AlertTicks + Shoot.WindUp;

    /// <summary>A sentry on its wreck with hero 0 thirty cells left of it, its arc just struck at it.</summary>
    private static (Scenario W, Creature Sentry, Arc Arc) Struck()
    {
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 80 - 30, 36).Run(ToArc);
        return (w, c, w.M.Creatures.Arcs.Single());
    }

    private static int Suit(Scenario w, int peer = 0) => w.M.Crew.HeroOf(peer)!.Air;

    /// <summary>The middle of the cell hero <paramref name="peer"/> is reported in, in thousandths of a cell.</summary>
    private static (int X, int Y) Reported(Scenario w, int peer = 0)
    {
        var (x, y) = w.M.Creatures.HeroAt(peer)!.Value;
        return (x * Unit + Unit / 2, y * Unit + Unit / 2);
    }

    [Fact]
    public void ItStrikesAtOnceFromItsMiddleToTheHerosCellStayingOnItsWreck()
    {
        var (w, c, arc) = Struck();
        Assert.Equal((Shoot.Projectile, c.X, c.Y - Tune.Sentry.Body.Height * Unit / 2), (arc.Projectile, arc.X0, arc.Y0));
        Assert.Equal(Reported(w), (arc.X1, arc.Y1));
        Assert.Equal(ActPhase.Active, c.Act!.Phase);
        Assert.True(CreaturesModule.Clamped(c));
        w.Run(Shoot.Recover);
        Assert.True(CreaturesModule.Clamped(c));
        Assert.Null(c.Act);
    }

    [Fact]
    public void ItsAimIsTakenItsLockBeforeTheArcAndHeldThoughTheHeroMoves()
    {
        // Moved the tick its aim is taken: struck where it went. Moved a tick after: struck where it was.
        foreach (int early in new[] { 1, 0 })
        {
            var (w, _, _) = SentryTests.OnAWreck();
            SentryTests.HeroAt(w, 80 - 30, 36).Run(ToArc - Shoot.Lock - early);
            var was = Reported(w);
            SentryTests.HeroAt(w, 80 - 30, 30).Run(Shoot.Lock + early);
            var arc = w.M.Creatures.Arcs.Single();
            Assert.Equal(early == 1 ? Reported(w) : was, (arc.X1, arc.Y1));
            Assert.NotEqual(was, Reported(w));
        }
    }

    [Fact]
    public void ItsTargetGoneBeforeItsAimIsTakenItStrikesNothing()
    {
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 80 - 30, 36).Run(ToArc - Shoot.Lock - 1);
        Assert.Equal((Tune.Move)Shoot, Tune.Sentry.Moves[c.Act!.Move]);
        // Off past its reach the tick before its aim.
        SentryTests.HeroAt(w, 2, 36).Run(Shoot.Lock + 1);
        Assert.Empty(w.M.Creatures.Arcs);
        Assert.Null(c.Act);
    }

    [Fact]
    public void AnArcIsCutAtItsReachAndStoppedWhereItEntersTheFirstWall()
    {
        // Struck from 78 cells: its arc reaches 60 of them, toward the striker.
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 2, 36).Run();
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item)).Run(Shoot.WindUp + 1);
        var arc = w.M.Creatures.Arcs.Single();
        Assert.Equal(Shoot.Reach * Unit, arc.X0 - arc.X1);

        var walled = new Scenario(string.Join('\n', Enumerable.Range(0, 80).Select(y => new string('.', 60) + "#" + new string('.', 99))), cells: true)
            .Do(new Crew.Enter(40 * 160 + 4)).Run();
        var debris = (DebrisState)walled.M.Debris.Slice;
        debris.Pieces.Add(new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 80_000, Y = 40_000, Start = 80_000, SeenAt = debris.Clock });
        SentryTests.Guarded(walled);
        walled.Do(new Release(Tune.Sentry.Id, 38 * 160 + 80)).Run();
        SentryTests.HeroAt(walled, 80 - 30, 36).Run(ToArc);
        // The wall is column 60: it stops at its right edge, the hero beyond untouched.
        Assert.Equal(61 * Unit, walled.M.Creatures.Arcs.Single().X1);
    }

    [Fact]
    public void AnArcLingersItsTicksThenGoes()
    {
        var (w, _, arc) = Struck();
        w.Run(Shoot.Lingers - 1);
        Assert.Contains(arc, w.M.Creatures.Arcs);
        w.Run();
        Assert.Empty(w.M.Creatures.Arcs);
    }

    [Fact]
    public void AnArcCostsEachHeroItCrossedItsDamageOnce()
    {
        var (w, _, arc) = Struck();
        SentryTests.HeroAt(w, 80 - 20, 36, peer: 1).Run();
        int suit0 = Suit(w), suit1 = Suit(w, 1);
        w.Do(new Bolted(arc.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(suit0 - Damage, Suit(w));
        // The same hero again: it has paid. A friend it crossed too: they pay.
        w.Do(new Bolted(arc.Id)).Do(new Bolted(arc.Id) { From = 1 }).Run();
        Assert.Equal("creatures.gone", w.Refusals.Single().Reason);
        Assert.Equal((suit0 - Damage, suit1 - Damage), (Suit(w), Suit(w, 1)));
    }

    [Fact]
    public void TwoHeroesSayingOneArcCrossedThemOnOneTickBothPay()
    {
        var (w, _, arc) = Struck();
        SentryTests.HeroAt(w, 80 - 20, 36, peer: 1).Run();
        int suit0 = Suit(w), suit1 = Suit(w, 1);
        w.Do(new Bolted(arc.Id)).Do(new Bolted(arc.Id) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((suit0 - Damage, suit1 - Damage), (Suit(w), Suit(w, 1)));
    }

    [Fact]
    public void AnArcLingeringNoLongerOrAGunsShotIsRefusedGone()
    {
        var (w, _, arc) = Struck();
        w.Run(Shoot.Lingers);
        w.Do(new Bolted(arc.Id)).Run();
        Assert.Equal("creatures.gone", w.Refusals.Single().Reason);

        var armed = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 60), 20)), cells: true)
            .Do(new Crew.Enter(10 * 60 + 2)).GiveItems(0, (Spec.Shots.Nailer.Item, 1), (Spec.Shots.Nail.Id, 1)).Run();
        armed.Do(new Fire(Spec.Shots.Nailer.Item, 10_500, 10_500, 0)).Run();
        armed.Do(new Bolted(armed.M.Creatures.Shots.Single().Id)).Run();
        Assert.Equal("creatures.gone", armed.Refusals.Single().Reason);
    }

    [Fact]
    public void ADeadSentrysArcStillHurts()
    {
        var (w, sentry, arc) = Struck();
        sentry.Damage = Tune.Sentry.Life.Hp - 1;
        w.Do(new Strike(sentry.Id, Spec.Weapons.Axe.Item)).Run();
        Assert.DoesNotContain(sentry, w.M.Creatures.Creatures);
        int suit = Suit(w);
        w.Do(new Bolted(arc.Id)).Run();
        Assert.Equal(suit - Damage, Suit(w));
    }

    [Fact]
    public void FiveNailsKillIt()
    {
        var (w, _, c) = SentryTests.OnAWreck();
        Assert.Equal(5, (Tune.Sentry.Life.Hp + Spec.Shots.DamageOf(Spec.Shots.Nailer.Item, Spec.Shots.Nail.Id) - 1) / Spec.Shots.DamageOf(Spec.Shots.Nailer.Item, Spec.Shots.Nail.Id));
        w.GiveItems(0, (Spec.Shots.Nailer.Item, 1), (Spec.Shots.Nail.Id, 5));
        for (int i = 0; i < 4; i++) w.Do(new Fire(Spec.Shots.Nailer.Item, c.X - 2000, c.Y - 1000, 0)).Run();
        Assert.Contains(c, w.M.Creatures.Creatures);
        w.Do(new Fire(Spec.Shots.Nailer.Item, c.X - 2000, c.Y - 1000, 0)).Run();
        Assert.DoesNotContain(c, w.M.Creatures.Creatures);
    }

    [Fact]
    public void StruckFromPastItsReachItStrikesAtTheStrikerNotAFriendNearer()
    {
        // Hero 0 strikes from 78 cells left; hero 1 stands 10 right: the arc goes left, at hero 0, falling short.
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 2, 36);
        SentryTests.HeroAt(w, 90, 36, peer: 1).Run();
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item)).Run(Shoot.WindUp + 1);
        var arc = w.M.Creatures.Arcs.Single();
        Assert.True(arc.X1 < arc.X0, $"{arc.X1} left of {arc.X0}");
    }

    [Fact]
    public void StruckOffItsPerchFlyingHomeItPouncesNeverHangingInTheAirToShoot()
    {
        var (w, _, c) = SentryTests.OnAWreck();
        // A pounce at a hero eight cells off, then the hero gone thirty cells off while it flies home.
        SentryTests.HeroAt(w, 72, 30).Run(Guard.AlertTicks + 1);
        for (int t = 0; t < 200 && !(Tune.Sentry.Brain[c.Drive] is Tune.Return && c.Act == null); t++) w.Run();
        Assert.False(CreaturesModule.Clamped(c));
        SentryTests.HeroAt(w, 80 - 30, 36).Run();
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item)).Run();
        Assert.IsType<Tune.Pounce>(Tune.Sentry.Moves[c.Act!.Move]);
    }

    [Fact]
    public void AWorldSavedAndLoadedEveryTickPlaysOnAsOneNeverSavedThroughItsArcsAndOneOnAHero()
    {
        var (a, _, _) = SentryTests.OnAWreck();
        var (b, _, _) = SentryTests.OnAWreck();
        foreach (var w in new[] { a, b }) SentryTests.HeroAt(w, 80 - 30, 36).Run();
        int since = 0, suit = 0;
        for (int t = 0; t < 160; t++)
        {
            // Three ticks after the first arc, the hero's client says it was crossed.
            if (since > 0 || a.M.Creatures.Arcs.Count > 0) since += 1;
            if (since == 3)
            {
                suit = Suit(a);
                foreach (var w in new[] { a, b }) w.Do(new Bolted(w.M.Creatures.Arcs.Single().Id));
            }
            a.Run();
            b.Reload().Run();
            Assert.Equal(a.World.Hash(), b.World.Hash());
            if (since == 3) Assert.Equal(suit - Damage, Suit(a));
        }
        Assert.True(((CreaturesState)a.M.Creatures.Slice).Fired > 1, "it struck again after");
        Assert.Empty(a.Refusals);
    }
}
