using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The cape worn in its own slot, sparing a fifth of enemies' blows and of the cold (2026-10-27-the-cape-is-worn).</summary>
public class CapeTests
{
    /// <summary>A pod in a vacuum room, never aired.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private static readonly int Lungs = Content.Crew.Lungs, Full = Scenario.Full;
    private static readonly int CapeSlot = SlotOf(Content.Cape.Slot), BackSlot = SlotOf("back");
    private static int SlotOf(string kind) => Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(kind);
    private static string Cape => Content.Cape.Item;

    private static int AtPod(int x, int y) => y * 22 + x;
    /// <summary>The first cell right of the pod past its air's reach: vacuum, and no pod.</summary>
    private static readonly int Beyond = AtPod(14, 6);

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>The pod room with hero 0 brought in beyond the pod's air, in a cape when <paramref name="caped"/>, bare unless <paramref name="suited"/>.</summary>
    private static Scenario InVacuum(bool caped, bool suited = false)
    {
        var w = new Scenario(PodRoom) { Bare = !suited };
        if (caped) w.Wear(0, Cape);
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        return Breaths(w, 1);
    }

    /// <summary>The cape taken straight out of its slot, as <see cref="ItemsTesting.Wear"/> puts it in.</summary>
    private static Scenario Unwear(Scenario w, int peer)
    {
        var state = (ItemsState)w.M.Items.Slice;
        var slots = Stacks.InventoryOf(state, peer).ToArray();
        slots[CapeSlot] = null;
        Stacks.SetInventory(state, peer, slots);
        return w;
    }

    [Fact]
    public void TheCapeIsWornInItsOwnSlotBesideAJetpackAndRefusedOnTheBack()
    {
        var w = new Scenario(PodRoom).Wear(0, Content.Jetpack.Item).GiveItems(0, (Cape, 1));
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Cape);
        w.Do(new Move(new Items.Place(bag), new Target(BackSlot), 1) { From = 0 }).Run();
        Assert.Equal("items.notEquipment", w.Refusals.Single().Reason);
        w.Do(new Move(new Items.Place(bag), new Target(CapeSlot), 1) { From = 0 }).Run();
        Assert.Single(w.Refusals);
        Assert.True(w.M.Items.Equipped(0, Cape));
        Assert.True(w.M.Items.Equipped(0, Content.Jetpack.Item));
        Assert.Equal(Cape, w.M.Items.WornIn(0, Content.Cape.Slot));
    }

    [Fact]
    public void BareInVacuumTheCapeSparesAFifthOfTheColdSoFiveBreathsCostNineNotTen()
    {
        var bare = InVacuum(caped: false);
        int before = HeroOf(bare).Air;
        Breaths(bare, 5);
        Assert.Equal(before - 10, HeroOf(bare).Air);
        var caped = InVacuum(caped: true);
        Breaths(caped, 5);
        Assert.Equal(before - 9, HeroOf(caped).Air);
        // The breaths themselves are paid in full; of the cold, six fifths with the breath brought in on: one unit spared, a fifth kept.
        Assert.Equal(200, HeroOf(caped).Spared);
        Assert.Equal((Content.Cape.Warmth, 0), (caped.M.Crew.Warmth(0), bare.M.Crew.Warmth(0)));
    }

    [Fact]
    public void WithABodyWornAndNoColdTheCapeChangesNothing()
    {
        var w = InVacuum(caped: true, suited: true);
        int before = HeroOf(w).Air;
        Breaths(w, 5);
        Assert.Equal(before - 5, HeroOf(w).Air);
        Assert.Equal(0, HeroOf(w).Spared);
    }

    [Fact]
    public void ABlowIsSparedAFifthTheRestCarriedSoTwoRamsOfThreeCostThreeThenTwo()
    {
        var w = InVacuum(caped: true, suited: true);
        int air = HeroOf(w).Air;
        w.M.Crew.Blow(0, 3);
        Assert.Equal((air - 3, 600), (HeroOf(w).Air, HeroOf(w).Spared));
        w.M.Crew.Blow(0, 3);
        Assert.Equal((air - 5, 200), (HeroOf(w).Air, HeroOf(w).Spared));
        // A pounce of 6 then: 1200 and the 200 kept, one unit spared and 400 kept.
        w.M.Crew.Blow(0, 6);
        Assert.Equal((air - 10, 400), (HeroOf(w).Air, HeroOf(w).Spared));
    }

    [Fact]
    public void TheCapeTakenOffABlowCostsInFullItsRemainderKept()
    {
        var w = InVacuum(caped: true, suited: true);
        w.M.Crew.Blow(0, 3);
        Unwear(w, 0);
        int air = HeroOf(w).Air;
        w.M.Crew.Blow(0, 5);
        Assert.Equal((air - 5, 600), (HeroOf(w).Air, HeroOf(w).Spared));
        // Worn again, the remainder counts toward the next unit spared.
        w.Wear(0, Cape);
        w.M.Crew.Blow(0, 2);
        Assert.Equal((air - 5 - 1, 0), (HeroOf(w).Air, HeroOf(w).Spared));
    }

    [Fact]
    public void AShockIsNoBlowTheCapeSparesNoneOfIt()
    {
        // A ruin's zap or the cradle's shock (0091, 0096): not an enemy's.
        var w = InVacuum(caped: true, suited: true);
        int air = HeroOf(w).Air;
        w.M.Crew.Shock(0, Content.Ruins.CradleShock);
        Assert.Equal((air - Content.Ruins.CradleShock, 0), (HeroOf(w).Air, HeroOf(w).Spared));
    }

    [Fact]
    public void ABlowOnAHeroInCreativeOutOrNoneTakesNothingAndKeepsNoRemainder()
    {
        var creative = InVacuum(caped: true, suited: true).Do(new SetCreative(true)).Run();
        creative.M.Crew.Blow(0, 3);
        Assert.Equal((Full, 0), (HeroOf(creative).Air, HeroOf(creative).Spared));

        var w = InVacuum(caped: true, suited: true);
        w.M.Crew.Shock(0, Full);
        Assert.True(HeroOf(w).Out > 0);
        int spared = HeroOf(w).Spared;
        w.M.Crew.Blow(0, 3);
        Assert.Equal(spared, HeroOf(w).Spared);
        w.M.Crew.Blow(1, 3);
        Assert.Null(w.M.Crew.HeroOf(1));
    }

    [Fact]
    public void TheCapeKeepsItsRemainderThroughASaveAndLoad()
    {
        var w = InVacuum(caped: true, suited: true);
        w.M.Crew.Blow(0, 3);
        w.Reload();
        Assert.Equal(600, HeroOf(w).Spared);
    }

    /// <summary>A lifepod and a Former on a panel of its own.</summary>
    private static Scenario Workshop() => new(Layout.Parse(
        ["         ", "         ", "#########", "#OOO....#", "#OOO....#", "#OOO.II.#", "#########"]), doubled: true);

    private static int Carried(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    /// <summary>The cape of two cloth and two of a grazer's hides, sewn at the workbench: with one hide short, refused, nothing taken.</summary>
    [Fact]
    public void TheCapeIsSewnOfTwoClothAndTwoHidesAtTheWorkbench()
    {
        Assert.Equal([("cloth", 2), ("hide", 2)], Content.Crafts.Get(Cape).Cost);
        var w = Workshop().GiveItems(0, ("cloth", 2), ("hide", 1));
        int bench = w.M.Grid.MachinesOf("workbench").Single().Id;
        w.Do(new Crafting.Craft(bench, Cape, 1)).Run();
        Assert.Equal(["items.notEnough"], w.Refusals.Select(r => r.Reason));
        Assert.Equal((2, 1), (Carried(w, "cloth"), Carried(w, "hide")));
        w.GiveItems(0, ("hide", 1)).Do(new Crafting.Craft(bench, Cape, 1)).Run(Content.Crafts.Get(Cape).Ticks * 1000 / Content.Stations.Get("workbench").Speed + 1);
        Assert.Single(w.Refusals);
        Assert.Equal((0, 0), (Carried(w, "cloth"), Carried(w, "hide")));
        Assert.Equal(1, w.M.Items.Crate(bench).Sum(s => s?.Item == Cape ? s.Count : 0));
    }

    [Fact]
    public void ItsSharesAreAFifthEachWithinAWhole()
    {
        Assert.Equal((200, 200), (Content.Cape.Guard, Content.Cape.Warmth));
        Assert.InRange(Content.Cape.Guard, 0, 1000);
        Assert.InRange(Content.Cape.Warmth, 0, 1000);
        Assert.Equal(Content.Cape.Slot, Content.Items.Get(Cape).Equip);
        Assert.True(Content.Items.Get(Cape).Gear);
    }

    private sealed record Step(string Type, int Units = 0);

    private static readonly Gen<Step> Steps = Gen.OneOf(
        Gen.Choose(0, 8).Select(n => new Step("blow", n)),
        Gen.Constant(new Step("breath")),
        Gen.Constant(new Step("toggle")));

    /// <summary>
    /// A bare hero in vacuum, its cape on and off, struck and breathing: every step charges what the cape leaves of its cost,
    /// spared units rounded down from the share and the remainder kept, the remainder within a unit. The air is filled before
    /// each step so none passes out.
    /// </summary>
    [Property(MaxTest = 60)]
    public Property ChargesWhatTheCapeLeavesOfEveryBlowAndColdItsRemainderWithinAUnit()
    {
        var steps = from n in Gen.Choose(0, 30) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = InVacuum(caped: true);
            foreach (var s in all)
            {
                var hero = HeroOf(w);
                hero.Air = Lungs;
                int kept = hero.Spared;
                bool worn = w.M.Items.Equipped(0, Cape);
                (int Charged, int Kept) Soften(int units, int share)
                {
                    if (!worn || units == 0) return (units, kept);
                    int thousandths = units * share + kept;
                    return (units - thousandths / 1000, thousandths % 1000);
                }
                (int charged, int left) = (0, kept);
                if (s.Type == "toggle")
                {
                    if (worn) Unwear(w, 0);
                    else w.Wear(0, Cape);
                    continue;
                }
                if (s.Type == "blow")
                {
                    (charged, left) = Soften(s.Units, Content.Cape.Guard);
                    w.M.Crew.Blow(0, s.Units);
                }
                else
                {
                    // A breath, paid in full, and the bare cold, softened.
                    (int cold, left) = Soften(Content.Crew.BareCold, Content.Cape.Warmth);
                    charged = 1 + cold;
                    Breaths(w, 1);
                }
                if (Lungs - hero.Air != charged || hero.Spared != left)
                    return false.Label($"{s}: charged {Lungs - hero.Air} kept {hero.Spared}, not {charged} and {left}");
                if (hero.Spared is < 0 or > 999) return false.Label($"a remainder of {hero.Spared}");
            }
            return true.ToProperty();
        });
    }
}
