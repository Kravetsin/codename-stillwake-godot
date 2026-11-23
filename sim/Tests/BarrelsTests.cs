using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Fuel drums (fuel-drums): a wreck that bursts as it breaks, where it was, by whoever broke it; and every burst, a drum's, a charge's, a
/// shell's or a missile's, hurting every body in its reach and sight, heroes, the one who set it off among them, and creatures.
/// </summary>
public class BarrelsTests
{
    private const int Unit = CreaturesModule.Unit, W = 320;
    private const string DrumKind = "fuelDrum";
    private static readonly Spec.Blast Drum = Spec.Blasting.Drum;
    private static readonly int Hp = Spec.Wreckage.HpOf(DrumKind)!.Value;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Charge = Spec.Shots.Thrown.Item;

    /// <summary>Headings: along +x.</summary>
    private const int Right = 0;

    /// <summary>
    /// Open space, 320 cells by 20, a cell a character, with <paramref name="walls"/> (column, row) put in; hero 0 at (2, 10) and
    /// each of <paramref name="others"/>, peers 1 on, at its (column, row), all entering on one tick, none in creative.
    /// </summary>
    private static Scenario Space(IReadOnlyList<(int X, int Y)>? walls = null, params (int X, int Y)[] others)
    {
        var rows = Enumerable.Range(0, 20).Select(y => new string([.. Enumerable.Range(0, W).Select(x => walls?.Contains((x, y)) == true ? '#' : '.')]));
        var w = new Scenario(string.Join('\n', rows), cells: true).Do(new Enter(10 * W + 2));
        for (int i = 0; i < others.Length; i++) w.Do(new Enter(others[i].Y * W + others[i].X) { From = i + 1 });
        return w.Run();
    }

    /// <summary>A piece of <paramref name="item"/> put out by hand, its middle in the middle of cell (<paramref name="x"/>, <paramref name="y"/>), still, <paramref name="damage"/> taken.</summary>
    internal static Piece Put(Scenario w, int x, int y, int damage = 0, string item = DrumKind)
    {
        var debris = (DebrisState)w.M.Debris.Slice;
        var p = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = item, X = x * Unit + Unit / 2, Y = y * Unit + Unit / 2, Start = x * Unit + Unit / 2, SeenAt = debris.Clock, Damage = damage };
        debris.Pieces.Add(p);
        SentryTests.Guarded(w);
        return p;
    }

    /// <summary>The axe's blow by <paramref name="peer"/> on <paramref name="drum"/>, its edge on it alone.</summary>
    private static HewPieces Hew(Piece drum, int peer = 0) => new(Axe, [drum.Id]) { From = peer };

    private static List<PendingBurst> Bursting(Scenario w) => ((DebrisState)w.M.Debris.Slice).Bursting;

    private static int CellOf(int x, int y) => y * W + x;

    private static bool Out(Scenario w, Piece p) => w.M.Debris.PieceById(p.Id) != null;

    private static Hero HeroOf(Scenario w, int peer = 0) => w.M.Crew.HeroOf(peer)!;

    /// <summary>A creature of <paramref name="species"/> put out by state, its bottom middle at (<paramref name="x"/>, <paramref name="y"/>) in thousandths of a cell.</summary>
    private static Creature PutCreature(Scenario w, Tune.Species species, int x, int y)
    {
        var state = (CreaturesState)w.M.Creatures.Slice;
        var c = new Creature { Id = state.NextId++, Species = species.Id, X = x, Y = y, SeenAt = state.Clock };
        state.Creatures.Add(c);
        return c;
    }

    [Fact]
    public void AnAxedDrumBurstsThatTickAtItsCellByItsAxerItsScrapFloatingThere()
    {
        var w = Space();
        var drum = Put(w, 20, 10, damage: Hp - 1);
        w.Do(Hew(drum)).Run();
        Assert.Empty(w.Refusals);
        Assert.False(Out(w, drum));
        Assert.Equal([new PendingBurst(CellOf(20, 10), 0, DrumKind)], w.M.Creatures.DrumBursts);
        Assert.Empty(Bursting(w));
        Assert.Contains(w.M.Items.Floor, s => s.Item == "scrapChunk");
        w.Run();
        Assert.Empty(w.M.Creatures.DrumBursts);
    }

    [Fact]
    public void AShotDrumBurstsTheNextTickByItsShooter()
    {
        var w = ShotsTests.Space();
        var drum = Put(w, 20, 10, damage: Hp - 1);
        w.Do(new Fire(Spec.Shots.Nailer.Item, 3 * Unit + Unit / 2, 10 * Unit + Unit / 2, Right));
        for (int t = 0; t < 60 && Out(w, drum); t++) w.Run();
        Assert.False(Out(w, drum));
        Assert.Empty(w.M.Creatures.DrumBursts);
        Assert.Single(Bursting(w));
        w.Run();
        Assert.Equal([new PendingBurst(CellOf(20, 10), 0, DrumKind)], w.M.Creatures.DrumBursts);
        Assert.Empty(Bursting(w));
    }

    [Fact]
    public void AHeroInReachIsHurtItsBodyOneOutOfReachOrBehindAWallNot()
    {
        // The drum at (30, 10): hero 1 four cells off, hero 2 thirteen, past its wave of 12; hero 3 five cells off behind a wall.
        var wall = Enumerable.Range(5, 11).Select(y => (27, y)).ToList();
        var w = Space(wall, (34, 10), (43, 10), (25, 10));
        var drum = Put(w, 30, 10, damage: Hp - 1);
        var hurts = Enumerable.Range(0, 4).Select(p => HeroOf(w, p).Hurts).ToList();
        w.Do(Hew(drum)).Run();
        Assert.Single(w.M.Creatures.DrumBursts);
        Assert.Equal([hurts[0], hurts[1] + 1, hurts[2], hurts[3]], Enumerable.Range(0, 4).Select(p => HeroOf(w, p).Hurts));
        // Both breathe alike: the one in reach is down its body's share more.
        Assert.Equal(HeroOf(w, 2).Air - Drum.Body, HeroOf(w, 1).Air);
        // It cracked the wall, and only cracked it: 300 is short of a scrap cell's 400.
        Assert.All(wall, c => Assert.NotNull(w.M.Grid.WallAt(CellOf(c.Item1, c.Item2))));
    }

    [Fact]
    public void TheOneWhoBrokeItIsHurtToo()
    {
        var w = Space(null, (60, 10));
        var drum = Put(w, 5, 10, damage: Hp - 1);
        int hurts = HeroOf(w).Hurts;
        w.Do(Hew(drum)).Run();
        Assert.Equal(hurts + 1, HeroOf(w).Hurts);
        Assert.Equal(HeroOf(w, 1).Air - Drum.Body, HeroOf(w).Air);
    }

    [Fact]
    public void ABreakerInReachDiesOneFartherIsUntouched()
    {
        var w = Space();
        var drum = Put(w, 30, 10, damage: Hp - 1);
        var near = BreakerTests.Put(w, 33, 10);
        var far = BreakerTests.Put(w, 45, 10);
        w.Do(Hew(drum)).Run();
        Assert.True(near.Dead);
        Assert.DoesNotContain(near, w.M.Creatures.Creatures);
        Assert.Contains(far, w.M.Creatures.Creatures);
        Assert.Equal(0, far.Damage);
    }

    [Fact]
    public void AWreckInReachTakesSixtyAndOneItBreaksDropsItsLoot()
    {
        var w = Space();
        var drum = Put(w, 30, 10, damage: Hp - 1);
        var section = Put(w, 36, 10, item: "hullSection");
        var engine = Put(w, 30, 16, item: "engineBlock");
        w.Do(Hew(drum)).Run();
        Assert.False(Out(w, section));
        Assert.Contains(w.M.Items.Floor, s => s.Item == "hullPlate");
        Assert.Equal(Drum.WreckDamage, engine.Damage);
        // Every stream wreck breaks to it.
        Assert.All(Spec.Debris.Kinds.Where(k => Spec.Wreckage.HpOf(k.Item) != null), k => Assert.True(Spec.Wreckage.HpOf(k.Item) <= Drum.WreckDamage, k.Item));
    }

    [Fact]
    public void ItsWaveReachesWrecksPastWhatItDigsAWallThereUntouched()
    {
        // The drum at (30, 10) digs 6 round (Reach) and hurts 12 round (Wave): an engine block's box 10 cells right takes its damage, one
        // 13 cells left nothing, and a scrap wall 9 cells up stands uncracked.
        var w = Space([(30, 1)]);
        var drum = Put(w, 30, 10, damage: Hp - 1);
        var near = Put(w, 45, 10, item: "engineBlock");
        var far = Put(w, 12, 10, item: "engineBlock");
        Assert.InRange(Drum.Reach, 0, 8);
        Assert.InRange(Drum.Wave, 10, 12);
        w.Do(Hew(drum)).Run();
        Assert.Single(w.M.Creatures.DrumBursts);
        Assert.Equal(Drum.WreckDamage, near.Damage);
        Assert.Equal(0, far.Damage);
        Assert.NotNull(w.M.Grid.WallAt(CellOf(30, 1)));
        Assert.Empty(w.M.Building.Cracks);
    }

    [Fact]
    public void AChainGoesOffATickALinkAllTheFirstOnesBreakers()
    {
        var w = Space(null, (2, 3));
        // Eight cells apart, each within the last's wave of its box, the third past the first's.
        var drums = new[] { Put(w, 20, 10, damage: Hp - 1), Put(w, 28, 10), Put(w, 36, 10) };
        w.Do(Hew(drums[0], peer: 1)).Run();
        Assert.Equal([new PendingBurst(CellOf(20, 10), 1, DrumKind)], w.M.Creatures.DrumBursts);
        Assert.False(Out(w, drums[1]));
        Assert.True(Out(w, drums[2]));
        w.Run();
        Assert.Equal([new PendingBurst(CellOf(28, 10), 1, DrumKind)], w.M.Creatures.DrumBursts);
        Assert.False(Out(w, drums[2]));
        w.Run();
        Assert.Equal([new PendingBurst(CellOf(36, 10), 1, DrumKind)], w.M.Creatures.DrumBursts);
        w.Run();
        Assert.Empty(w.M.Creatures.DrumBursts);
    }

    [Fact]
    public void OnItsOwnHullOneAPlayerBrokeCracksByATenthOneACreatureBrokeInFull()
    {
        int Crack(int peer)
        {
            var w = new Scenario(Layout.Parse("..........\n.......#..\n..........").WithMaterials("..........\n.......w..\n.........."));
            var drum = Put(w, 4, 1, damage: Hp - 1);
            ((IDebrisOps)w.M.Debris).Hit(peer, drum.Id, 1);
            w.Run();
            Assert.Equal([new PendingBurst(14, peer, DrumKind)], w.M.Creatures.DrumBursts);
            Assert.NotNull(w.M.Grid.WallAt(17));
            return w.M.Building.Cracks.Single(c => c.Cell == 17).Damage;
        }
        int left = Drum.Power - 3 * Spec.Damage.Step;
        Assert.Equal(left * Spec.Damage.OwnShipTakes / 1000, Crack(0));
        Assert.Equal(left, Crack(FloorStack.Nobody));
    }

    [Fact]
    public void AThrownChargeHurtsItsThrowerAndABreakerInReachItsDirectTargetOnce()
    {
        var w = Space().GiveItems(0, (Charge, 1)).Wields(Charge);
        // A loper afloat across cells 4 to 7, rows 8 to 11: the charge thrown from cell 3 meets it on its first step.
        var loper = PutCreature(w, Tune.Loper, 6 * Unit, 12 * Unit);
        var breaker = BreakerTests.Put(w, 6, 14);
        int hurts = HeroOf(w).Hurts;
        w.Do(new Fire(Charge, 3 * Unit + 900, 10 * Unit + Unit / 2, Right)).Run();
        var end = Assert.Single(w.M.Creatures.Ended);
        Assert.Equal(CellOf(3, 10), end.Burst);
        Assert.Equal(Spec.Shots.DamageOf(Charge, Charge), loper.Damage);
        Assert.False(loper.Dead);
        Assert.True(breaker.Dead);
        Assert.Equal(hurts + 1, HeroOf(w).Hurts);
    }

    [Fact]
    public void ACatchersShellHurtsABreakerInReachItsBody()
    {
        var w = Space();
        var breaker = BreakerTests.Put(w, 32, 10);
        var state = (CreaturesState)w.M.Creatures.Slice;
        // A shell by no one, out of flight the next tick, at (30, 10).
        state.Fired += 1;
        state.Shots.Add(new Shot { Id = state.Fired, Projectile = Spec.Shots.Shell.Id, By = Ref.None, X = 30 * Unit + Unit / 2, Y = 10 * Unit + Unit / 2, Left = 1 });
        w.Run();
        Assert.True(Assert.Single(w.M.Creatures.Ended).Burst >= 0);
        Assert.Equal(Spec.Blasting.Shell.Body, breaker.Damage);
    }

    [Fact]
    public void TwoHeroesHittingOneDrumOnOneTickSetOffOneBurstTheFirstOnes()
    {
        var w = Space(null, (60, 10), (60, 12));
        var drum = Put(w, 30, 10, damage: Hp - 1);
        w.Do(Hew(drum, peer: 2)).Do(Hew(drum, peer: 1)).Run();
        Assert.Equal([new PendingBurst(CellOf(30, 10), 2, DrumKind)], w.M.Creatures.DrumBursts);
        Assert.Equal("debris.nothingToHit", Assert.Single(w.Refusals).Reason);
        w.Run();
        Assert.Empty(w.M.Creatures.DrumBursts);
    }

    [Fact]
    public void ASaveWithABurstWaitingLoadsAndBurstsOnceAsTheUnsavedRunDoes()
    {
        Scenario Broken()
        {
            var w = Space(null, (34, 10));
            var drum = Put(w, 30, 10, damage: Hp - 1);
            Put(w, 50, 10);
            ((IDebrisOps)w.M.Debris).Hit(0, drum.Id, 1);
            return w;
        }
        var kept = Broken();
        var loaded = Broken().Reload();
        Assert.Single(Bursting(loaded));
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
        for (int t = 0; t < 3; t++)
        {
            kept.Run();
            loaded.Run();
            Assert.Equal(kept.World.Hash(), loaded.World.Hash());
            Assert.Equal(t == 0 ? 1 : 0, loaded.M.Creatures.DrumBursts.Count);
            Assert.Equal(kept.M.Creatures.DrumBursts, loaded.M.Creatures.DrumBursts);
        }
        Assert.Equal(kept.World.Save(), loaded.World.Save());
    }
}
