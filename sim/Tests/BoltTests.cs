using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>The sentry's bolt (shots-are-the-sims): shot past its strike, it hurts the hero whose client says it touched.</summary>
public class BoltTests
{
    private static readonly Tune.Guard Guard = Tune.Sentry.Brain.OfType<Tune.Guard>().Single();
    private static readonly Tune.Shoot Shoot = (Tune.Shoot)Tune.Sentry.Moves[1];
    private static readonly int Damage = Spec.Shots.Bolt.Damage;

    /// <summary>A sentry on its wreck with hero 0 thirty cells left of it, its bolt just fired at it.</summary>
    private static (Scenario W, Creature Sentry, Shot Bolt) Fired()
    {
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 80 - 30, 36).Run(Guard.AlertTicks + Shoot.WindUp + 1);
        return (w, c, w.M.Creatures.Shots.Single());
    }

    private static int Suit(Scenario w, int peer = 0) => w.M.Crew.HeroOf(peer)!.Suit;

    [Fact]
    public void ABoltOnAHeroCostsItsSuitTheBoltsDamageOnceAndEndsIt()
    {
        var (w, _, bolt) = Fired();
        int suit = Suit(w);
        w.Do(new Bolted(bolt.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(suit - Damage, Suit(w));
        Assert.Empty(w.M.Creatures.Shots);
        // The same one again: it is spent.
        w.Do(new Bolted(bolt.Id)).Run();
        Assert.Equal("creatures.gone", w.Refusals.Single().Reason);
        Assert.Equal(suit - Damage, Suit(w));
    }

    [Fact]
    public void TwoHeroesClientsSayingOneBoltTouchedThemOnOneTickTheFirstPaysAndTheOtherIsRefused()
    {
        var (w, _, bolt) = Fired();
        SentryTests.HeroAt(w, 80 - 32, 36, peer: 1).Run();
        int suit0 = Suit(w), suit1 = Suit(w, 1);
        w.Do(new Bolted(bolt.Id)).Do(new Bolted(bolt.Id) { From = 1 }).Run();
        Assert.Equal("creatures.gone", w.Refusals.Single().Reason);
        Assert.Equal((suit0 - Damage, suit1), (Suit(w), Suit(w, 1)));
    }

    [Fact]
    public void ABoltThatRanOutOrAGunsShotIsRefusedGone()
    {
        var (w, _, bolt) = Fired();
        while (w.M.Creatures.Shots.Count > 0) w.Run();
        // Run out, it met nothing: no hit is shown where it stopped.
        Assert.False(w.M.Creatures.Ended.Single().Met);
        w.Do(new Bolted(bolt.Id)).Run();
        Assert.Equal("creatures.gone", w.Refusals.Single().Reason);

        var armed = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 60), 20)), cells: true)
            .Do(new Crew.Enter(10 * 60 + 2)).GiveItems(0, (Spec.Shots.Nailer.Item, 1), (Spec.Shots.Nail.Id, 1)).Run();
        armed.Do(new Fire(Spec.Shots.Nailer.Item, 10_500, 10_500, 0)).Run();
        armed.Do(new Bolted(armed.M.Creatures.Shots.Single().Id)).Run();
        Assert.Equal("creatures.gone", armed.Refusals.Single().Reason);
    }

    [Fact]
    public void ADeadSentrysBoltFliesOnAndStillHurts()
    {
        var (w, sentry, bolt) = Fired();
        sentry.Damage = Tune.Sentry.Life.Hp - 1;
        w.Do(new Strike(sentry.Id, Spec.Weapons.Axe.Item)).Run();
        Assert.DoesNotContain(sentry, w.M.Creatures.Creatures);
        Assert.Contains(bolt, w.M.Creatures.Shots);
        int suit = Suit(w);
        w.Do(new Bolted(bolt.Id)).Run();
        Assert.Equal(suit - Damage, Suit(w));
    }

    [Fact]
    public void ItsTargetGoneDuringItsWindUpItFiresNothing()
    {
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 80 - 30, 36).Run(Guard.AlertTicks + 1);
        Assert.Equal((Tune.Move)Shoot, Tune.Sentry.Moves[c.Act!.Move]);
        // Off past its reach before the bolt leaves.
        SentryTests.HeroAt(w, 2, 36).Run(Shoot.WindUp + 1);
        Assert.Empty(w.M.Creatures.Shots);
        Assert.Null(c.Act);
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
    public void StruckFromPastItsReachItShootsAtTheStrikerNotAFriendNearer()
    {
        // Hero 0 strikes from 78 cells left; hero 1 stands 10 right: the bolt flies left, at hero 0, falling short.
        var (w, _, c) = SentryTests.OnAWreck();
        SentryTests.HeroAt(w, 2, 36);
        SentryTests.HeroAt(w, 90, 36, peer: 1).Run();
        w.Do(new Strike(c.Id, Spec.Weapons.Axe.Item)).Run(Shoot.WindUp + 1);
        var bolt = w.M.Creatures.Shots.Single();
        Assert.Equal(-Shoot.Speed, bolt.VX);
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
    public void AWorldSavedAndLoadedEveryTickPlaysOnAsOneNeverSavedThroughItsShotsAndABoltOnAHero()
    {
        var (a, _, _) = SentryTests.OnAWreck();
        var (b, _, _) = SentryTests.OnAWreck();
        foreach (var w in new[] { a, b }) SentryTests.HeroAt(w, 80 - 30, 36).Run();
        int flying = 0, suit = 0;
        for (int t = 0; t < 160; t++)
        {
            // Five ticks into the first bolt's flight, the hero's client says it touched.
            if (flying > 0 || a.M.Creatures.Shots.Count > 0) flying += 1;
            if (flying == 5)
            {
                suit = Suit(a);
                foreach (var w in new[] { a, b }) w.Do(new Bolted(w.M.Creatures.Shots.Single().Id));
            }
            a.Run();
            b.Reload().Run();
            Assert.Equal(a.World.Hash(), b.World.Hash());
            if (flying == 5) Assert.Equal(suit - Damage, Suit(a));
        }
        Assert.True(((CreaturesState)a.M.Creatures.Slice).Fired > 1, "it shot again after");
        Assert.Empty(a.Refusals);
    }
}
