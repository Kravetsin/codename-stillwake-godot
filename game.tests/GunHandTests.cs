using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The guns in this player's hand (shots-are-the-sims): the cooldown, the dry click, and its own shots flown as the sim flies them.</summary>
public class GunHandTests
{
    private static readonly Spec.Gun Nailer = Spec.Shots.Nailer;

    [Fact]
    public void AClickFiresOnceAPaceClicksDryWithNoAmmoAndSaysWhenReadyAgain()
    {
        var hand = new GunHand();
        Assert.NotNull(hand.Step(16, pressed: true, Nailer, (500, 500), 0, Spec.Shots.Nail).Fire);
        Assert.Null(hand.Step(16, pressed: true, Nailer, (500, 500), 0, Spec.Shots.Nail).Fire);
        Assert.False(hand.Ready);
        Assert.True(hand.Step(GunHand.PaceMs(Nailer), pressed: false, Nailer, (500, 500), 0, Spec.Shots.Nail).Ready);
        var dry = hand.Step(16, pressed: true, Nailer, (500, 500), 0, ammo: null);
        Assert.True(dry.Dry);
        Assert.Null(dry.Fire);
        Assert.True(hand.Ready);
        Assert.Single(hand.Shots);
    }

    [Fact]
    public void ItsOwnShotFliesWhereTheSimsFliesAndEndsInTheCellTheSimsEnds()
    {
        string open = new('.', 60), ground = new('#', 60);
        var layout = Layout.Parse([.. Enumerable.Repeat(open, 20), ground, ground]) with { Kind = Spec.Surfaces.TestStrip.Id };
        var w = new TestWorld(layout).Step(new Sim.Crew.Enter(19 * 60 + 2)).Give(0, (Nailer.Item, 1), (Spec.Shots.Nail.Id, 5));
        var hand = new GunHand();
        // Up and right at 45°, from just over the ground.
        var fire = hand.Step(16, pressed: true, Nailer, (2500, 19_900), 224, Spec.Shots.Nail).Fire!;
        w.Step(fire);
        int? simEnd = null;
        for (int t = 0; t < 40 && simEnd == null; t++)
        {
            hand.Tick(w.M.Creatures.StepOf, w.M.Grid.Width, w.M.Grid.Height);
            if (w.M.Creatures.Ended.Count > 0) simEnd = w.M.Creatures.Ended.Single().Cell;
            else
            {
                var sim = w.M.Creatures.Shots.Single();
                Assert.Equal((sim.X, sim.Y, sim.VY), (hand.Shots[0].Now.X, hand.Shots[0].Now.Y, hand.Shots[0].Now.VY));
                w.Step();
            }
        }
        Assert.NotNull(simEnd);
        Assert.Equal(simEnd, hand.TakeEnded().Single().EndCell);
    }

    [Fact]
    public void AHostDrawsEveryShotAsTheSimsAGuestItsOwnAsCopiesAndTheOthersAsTheSims()
    {
        var (mine, theirs) = (Ref.Hero(0), Ref.Hero(1));
        Assert.Equal((false, true, true), (ShotDraws.FliesOwn(hosts: true), ShotDraws.DrawsSims(mine, 0, hosts: true), ShotDraws.DrawsSims(theirs, 0, hosts: true)));
        Assert.Equal((true, false, true), (ShotDraws.FliesOwn(hosts: false), ShotDraws.DrawsSims(mine, 0, hosts: false), ShotDraws.DrawsSims(theirs, 0, hosts: false)));
    }

    [Fact]
    public void AGuestsCopyFliesOnThroughALoperItWouldKillHittingTheNextAsTheSimDoes()
    {
        string open = new('.', 60), ground = new('#', 60);
        var layout = Layout.Parse([.. Enumerable.Repeat(open, 20), ground, ground]) with { Kind = Spec.Surfaces.TestStrip.Id };
        var w = new TestWorld(layout).Step(new Sim.Crew.Enter(19 * 60 + 2)).Give(0, (Nailer.Item, 1), (Spec.Shots.Nail.Id, 5))
            .Step(new Release(Spec.Creatures.Loper.Id, 19 * 60 + 30)).Step(new Release(Spec.Creatures.Loper.Id, 19 * 60 + 34));
        var first = w.M.Creatures.Creatures[0];
        (int x, int y) = (first.X - 3000, first.Y - 2000);
        // A nail of the sim's alone first: 6 of the loper's 12, so the next kills it.
        w.Step(new Fire(Nailer.Item, x, y, 0));
        var hand = new GunHand { Flies = ShotDraws.FliesOwn(hosts: false) };
        var fire = hand.Step(16, pressed: true, Nailer, (x, y), 0, Spec.Shots.Nail).Fire!;
        var drawn = new List<(ShotTarget, bool)>();
        // As a guest's frame runs, its copy a round trip ahead: flown on the world before its Fire even applies.
        for (int t = 0; t < 40 && hand.Shots.Count > 0; t++)
        {
            hand.Tick(w.M.Creatures.StepOf, w.M.Grid.Width, w.M.Grid.Height);
            drawn.AddRange(hand.TakeHits().Select(h => (h.What, h.Broke)));
            if (hand.Shots.Count > 0 && hand.Shots[0].EndCell == null) Assert.True(hand.Shots[0].Passed.Count <= 1);
            hand.TakeEnded();
        }
        w.Step(fire);
        var sims = new List<(ShotTarget, bool)>(w.M.Creatures.Hits.Select(h => (h.What, h.Broke)));
        for (int t = 0; t < 40 && w.M.Creatures.Shots.Count > 0; t++)
        {
            w.Step();
            sims.AddRange(w.M.Creatures.Hits.Select(h => (h.What, h.Broke)));
        }
        Assert.Equal([(ShotTarget.Creature, true), (ShotTarget.Creature, false)], sims);
        Assert.Equal(sims, drawn);
    }

    [Fact]
    public void TheHostDrawsTheSimsOwnShotNotACopyANailThatKillsFlyingOnWithItsHitShown()
    {
        string open = new('.', 60), ground = new('#', 60);
        var layout = Layout.Parse([.. Enumerable.Repeat(open, 20), ground, ground]) with { Kind = Spec.Surfaces.TestStrip.Id };
        var w = new TestWorld(layout).Step(new Sim.Crew.Enter(19 * 60 + 2)).Give(0, (Nailer.Item, 1), (Spec.Shots.Nail.Id, 5))
            .Step(new Release(Spec.Creatures.Loper.Id, 19 * 60 + 30));
        var loper = w.M.Creatures.Creatures.Single();
        (int x, int y) = (loper.X - 3000, loper.Y - 2000);
        w.Step(new Fire(Nailer.Item, x, y, 0));
        var hand = new GunHand { Flies = ShotDraws.FliesOwn(hosts: true) };
        var fire = hand.Step(16, pressed: true, Nailer, (x, y), 0, Spec.Shots.Nail).Fire!;
        // As the host's frame runs: the world's tick, then this hand's shots flown to it.
        w.Step(fire);
        Assert.Empty(w.M.Creatures.Creatures);
        hand.Tick(w.M.Creatures.StepOf, w.M.Grid.Width, w.M.Grid.Height);
        // No copy flown on the host: one stepped now would meet a world its own hit already changed, flying on past the loper unhit.
        Assert.Empty(hand.Shots);
        Assert.Empty(hand.TakeHits());
        // The host draws the sim's own shot instead, flying on through the loper it killed, its hit shown.
        Assert.True(ShotDraws.DrawsSims(Ref.Hero(0), own: 0, hosts: true));
        Assert.Contains(w.M.Creatures.Hits, hit => hit.What == ShotTarget.Creature && hit.Broke && hit.By == Ref.Hero(0));
        Assert.Single(w.M.Creatures.Shots);
    }

    private static readonly Spec.Gun Rail = Spec.Shots.Railgun;

    [Fact]
    public void ARailgunHeldFullFiresOnceLetGoAndLetGoSoonerFiresNothing()
    {
        var hand = new GunHand();
        double full = GunHand.ChargeMs(Rail);
        var begun = hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        Assert.Null(begun.Fire);
        Assert.InRange(begun.Charge, 0, 0.1);
        // Let go at half: nothing fired, nothing to wait for.
        Assert.Null(hand.Step(full / 2, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true).Fire);
        var early = hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false);
        Assert.Equal((null, 0.0), (early.Fire, early.Charge));
        Assert.True(hand.Ready);
        // Held full: full once, staying full however long, then fired let go.
        hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        var charged = hand.Step(full, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        Assert.Equal((1.0, true), (charged.Charge, charged.Charged));
        var still = hand.Step(5000, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        Assert.Equal((1.0, false, null), (still.Charge, still.Charged, still.Fire));
        var fired = hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false);
        Assert.Equal(Rail.Item, fired.Fire!.Tool);
        Assert.False(hand.Ready);
    }

    [Fact]
    public void ARailgunWithNothingToPayClicksDryAndAChargeIsLostPutAway()
    {
        var hand = new GunHand();
        var dry = hand.Step(16, pressed: true, Rail, (500, 500), 0, ammo: null, held: true);
        Assert.Equal((true, 0.0), (dry.Dry, dry.Charge));
        hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Step(GunHand.ChargeMs(Rail), pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        // Put away full, then out again and let go: nothing fires.
        Assert.Equal(0.0, hand.Step(16, pressed: false, null, null, 0, null, held: true).Charge);
        Assert.Null(hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false).Fire);
    }

    [Fact]
    public void ARailgunFullWhoseBeltWasEmptiedMidHoldClicksDryLetGo()
    {
        var hand = new GunHand();
        hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Step(GunHand.ChargeMs(Rail), pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        // The battery taken off the belt: nothing to pay, so nothing to fire.
        var let = hand.Step(16, pressed: false, Rail, (500, 500), 0, ammo: null, held: false);
        Assert.Equal((null, true), (let.Fire, let.Dry));
        Assert.True(hand.Ready);
    }

    [Fact]
    public void ARailgunChargesOnlyWithTheCooldownOutFiresOnceAndLosesItsChargeWithNoMuzzleOrDropped()
    {
        var hand = new GunHand();
        double full = GunHand.ChargeMs(Rail);
        Fire(hand, full);
        // One frame after firing nothing fires again; pressed in the cooldown it does not charge.
        Assert.Null(hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false).Fire);
        Assert.Equal(0.0, hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true).Charge);
        hand.Step(GunHand.PaceMs(Rail), pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false);
        // Full, then no longer held up for a frame (no muzzle): let go, nothing.
        hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Step(full, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Step(16, pressed: false, Rail, null, 0, Spec.Shots.Nail, held: true);
        Assert.Null(hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false).Fire);
        // Full, then dropped (a change of surface): let go, nothing.
        hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Step(full, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Drop();
        Assert.Null(hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false).Fire);
    }

    /// <summary>A full charge of the railgun let go: its <c>Fire</c>.</summary>
    private static void Fire(GunHand hand, double full)
    {
        hand.Step(16, pressed: true, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        hand.Step(full, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: true);
        Assert.NotNull(hand.Step(16, pressed: false, Rail, (500, 500), 0, Spec.Shots.Nail, held: false).Fire);
    }
}
