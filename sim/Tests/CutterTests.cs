using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Inventory = Stillwake.Content.Inventory;
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The salvage tools on wrecks (salvage-tools): the plasma cutter's bonus, half again a wreck's loot with the chance of
/// its share of the hit points, and the arc module in the spear and the grinder.
/// </summary>
public class CutterTests
{
    private const string Kind = "hullSection";
    private static readonly int Hp = WreckageSpec.HpOf(Kind)!.Value;
    private static readonly string Cutter = Weapons.PlasmaCutter.Item, Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;
    private static readonly int Belt = Inventory.Slots + 2;

    /// <summary>A hull section in open space, hero 0 by it in creative mode, carrying the axe, the drill and the cutter, on <paramref name="seed"/>.</summary>
    private static Scenario Wreck(uint seed)
    {
        var w = new Scenario(DebrisZonesTests.Open(20, 8, 1, 1, w: 1, h: 1), seed).Run(DebrisSpec.Every);
        w.M.Debris.Pieces[0].Item = Kind;
        return w.GiveItems(0, (Axe, 1), (Drill, 1), (Cutter, 1)).Do(new SetCreative(true)).Do(new Enter(1 * 20 + 12)).Run();
    }

    /// <summary><paramref name="times"/> hits of <paramref name="tool"/> by hero 0, an electric one run first, all on the tick to come.</summary>
    private static Scenario Hits(Scenario w, string tool, int times)
    {
        if (Stillwake.Content.Items.Get(tool).Draw != null) w.Do(new Run(tool, true));
        for (int i = 0; i < times; i++) w.Do(HewTesting.Hit(w, 1, tool));
        return w;
    }

    /// <summary>What lies on the floor, by item, in item order.</summary>
    private static (string Item, int Count)[] Floor(Scenario w) =>
        [.. w.M.Items.Floor.GroupBy(s => s.Item, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => (g.Key, g.Sum(s => s.Count)))];

    /// <summary><paramref name="plain"/>'s drop with the bonus won: each line half again, rounded down.</summary>
    private static (string Item, int Count)[] HalfAgain((string Item, int Count)[] plain) => [.. plain.Select(l => (l.Item, l.Count * 3 / 2))];

    /// <summary>The same wreck on the same seed broken by the axe on the same tick: the drop with no bonus.</summary>
    private static (string Item, int Count)[] Plain(uint seed) => Floor(Hits(Wreck(seed), Axe, (Hp + 1) / 2).Run());

    [Fact]
    public void AWreckCutWholeAlwaysDropsHalfAgainItsLoot()
    {
        for (uint seed = 1; seed <= 12; seed++)
        {
            var w = Hits(Wreck(seed), Cutter, Hp).Run();
            Assert.Empty(w.Refusals);
            Assert.Empty(w.M.Debris.Pieces);
            var plain = Plain(seed);
            Assert.NotEmpty(plain);
            Assert.Equal(HalfAgain(plain), Floor(w));
        }
    }

    [Fact]
    public void OneDrilledThenFinishedByTheCutterWinsItAsOftenAsTheCuttersShare()
    {
        // Ten of fifteen drilled, five cut: a third of the time.
        int won = 0, lost = 0;
        for (uint seed = 1; seed <= 30; seed++)
        {
            var w = Hits(Hits(Wreck(seed), Drill, 10), Cutter, Hp - 10).Run();
            Assert.Empty(w.Refusals);
            Assert.Empty(w.M.Debris.Pieces);
            var plain = Plain(seed);
            if (Floor(w).SequenceEqual(plain)) lost++;
            else
            {
                Assert.Equal(HalfAgain(plain), Floor(w));
                won++;
            }
        }
        // Pinned by the seeds: about a third.
        Assert.Equal((13, 17), (won, lost));
    }

    [Fact]
    public void OneCutThenFinishedByAShotRollsByItsCutToo()
    {
        int won = 0;
        for (uint seed = 1; seed <= 12; seed++)
        {
            var w = Hits(Wreck(seed), Cutter, Hp - 1).Run();
            Assert.Equal((Hp - 1, Hp - 1), (w.M.Debris.Pieces[0].Damage, w.M.Debris.Pieces[0].Cut));
            // As a shot's hit lands (shots-are-the-sims), on a wreck the axe would break on this tick too.
            var plain = Wreck(seed);
            Hits(plain, Axe, (Hp - 1) / 2 - 1).Run();
            Assert.True(plain.M.Debris.Hit(0, 1, Hp));
            Assert.True(w.M.Debris.Hit(0, 1, Hp));
            var drop = Floor(plain);
            if (!Floor(w).SequenceEqual(drop))
            {
                Assert.Equal(HalfAgain(drop), Floor(w));
                won++;
            }
        }
        // Fourteen of fifteen cut: won all but now and then.
        Assert.InRange(won, 9, 12);
    }

    [Fact]
    public void AHalfCutWreckSavedAndLoadedBreaksAlike()
    {
        var a = Hits(Wreck(7), Cutter, 7).Run();
        var b = Hits(Wreck(7), Cutter, 7).Run().Reload();
        Assert.Equal(7, b.M.Debris.Pieces[0].Cut);
        Hits(a, Axe, 4).Run();
        Hits(b, Axe, 4).Run();
        Assert.Empty(a.M.Debris.Pieces);
        Assert.Equal(Floor(a), Floor(b));
        Assert.Equal(a.World.Hash(), b.World.Hash());
    }

    [Fact]
    public void AnUncutWreckWritesNoCutInItsSave()
    {
        var w = Hits(Wreck(3), Axe, 2).Run();
        Assert.DoesNotContain("\"cut\"", w.World.Save());
        Assert.Contains("\"cut\"", Hits(w, Cutter, 1).Run().World.Save());
    }

    // The arc module in the salvage tools.

    [Fact]
    public void TheArcSpearDealsSixOnABeltOfNothingAndIsNeverRefusedFlat()
    {
        var w = Wreck(5).Do(new SetCreative(false)).Run();
        w.Put(0, new Stack(Weapons.Spear.Item, 1, Module: Stillwake.Content.Modules.Arc.Item));
        Assert.Equal(6, w.M.Items.Damage(0, Weapons.Spear.Item));
        Hits(w, Weapons.Spear.Item, 1).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(6, w.M.Debris.Pieces[0].Damage);
    }

    [Fact]
    public void TheArcGrinderDealsSixAndDrawsEight()
    {
        var w = Wreck(5).Do(new SetCreative(false)).Run();
        var grinder = new Stack(Weapons.Grinder.Item, 1, Module: Stillwake.Content.Modules.Arc.Item);
        w.Put(0, grinder).Put(Belt, new Stack("battery", 1, Stillwake.Content.Charge.Battery));
        Assert.Equal((6, 8), (w.M.Items.Damage(0, Weapons.Grinder.Item), ToolStats.Of(grinder).Draw));
        Hits(w, Weapons.Grinder.Item, 1).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(6, w.M.Debris.Pieces[0].Damage);
        Assert.Equal(Stillwake.Content.Charge.Battery - 8 * Stillwake.Content.Charge.Scale, w.M.Items.Inventory(0)[Belt]!.Charge);
    }

    // What always holds.

    /// <summary>
    /// Any run of hits by the axe, the drill and the cutter on a wreck: while it floats its cut is at most its damage, which is below
    /// its hit points; with no cutter among them, it is never cut.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property ALivePiecesCutIsAtMostItsDamageBelowItsHitPoints() =>
        Prop.ForAll((from n in Gen.Choose(0, 12) from t in Gen.Elements(0, 1, 2).ArrayOf(n) select t).ToArbitrary(), tools =>
        {
            var w = Wreck(9);
            string[] by = [Axe, Drill, Cutter];
            foreach (int t in tools)
            {
                Hits(w, by[t], 1).Run();
                if (w.M.Debris.Pieces.FirstOrDefault(p => p.Id == 1) is not { } piece) continue;
                if (!(piece.Cut <= piece.Damage && piece.Damage < Hp)) return false;
                if (!tools.Contains(2) && piece.Cut != 0) return false;
            }
            return true;
        });
}
