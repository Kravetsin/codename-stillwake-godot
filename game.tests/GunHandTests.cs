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
}
