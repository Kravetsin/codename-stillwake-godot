using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using Hero = Stillwake.Sim.Crew.Hero;
using Move = Stillwake.Sim.Items.Move;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The regen suit (2026-11-03-the-regen-suit): its mode on, after each breath it gives back what the breath took from the
/// helmet's air, at most <c>Suit.RegenMost</c>, paying <c>Suit.RegenCost</c> power-ticks a unit from the belt; never the cold.
/// </summary>
public class RegenSuitTests
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

    private const int S = Charge.Scale;
    private static readonly int Full = Charge.Battery, Cost = Suit.RegenCost * S;
    private static int SlotOf(string kind) => Inventory.Slots + Inventory.Equipment.ToList().IndexOf(kind);
    private static readonly int BodySlot = SlotOf(Suit.Slot), Belt = SlotOf(Charge.Belt);
    private static int AtPod(int x, int y) => y * 22 + x;
    private static readonly int Hatch = AtPod(6 + Content.Crew.PodHatch.X, 4 + Content.Crew.PodHatch.Y);
    /// <summary>The first cell right of the pod past its air's reach: vacuum, and no pod.</summary>
    private static readonly int Beyond = AtPod(14, 6);

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;
    private static int Charge0(Scenario w) => w.M.Items.BeltCharge(0).Charge;
    private static string[] Reasons(Scenario w) => [.. w.Refusals.Select(r => r.Reason)];

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>
    /// Hero 0 in the heavy helmet and the regen suit at <paramref name="cell"/>, a battery of each charge on its belt, left to
    /// right, its mode switched on unless <paramref name="on"/> says otherwise, run to just after a breath.
    /// </summary>
    private static Scenario Regen(int cell, bool on = true, params int[] batteries)
    {
        var w = new Scenario(Layout.Parse(PodRoom) with { Known = [] }, doubled: true) { Bare = true }.Wear(0, Helmet.Heavy).Wear(0, Suit.Regen);
        for (int i = 0; i < batteries.Length; i++) w.Put(Belt + i, new Stack("battery", 1, batteries[i]));
        w.Do(new Enter(cell) { From = 0 }).Run();
        if (on) w.Do(new SuitMode(true) { From = 0 }).Run();
        return Breaths(w, 1);
    }

    [Fact]
    public void AtRestInVacuumTheAirHoldsAndEachBreathPaysFifteen()
    {
        var w = Regen(Beyond, batteries: [Full]);
        int air = HeroOf(w).Air, charge = Charge0(w);
        Breaths(w, 3);
        Assert.Equal((air, charge - 3 * Cost), (HeroOf(w).Air, Charge0(w)));
        Assert.True(w.M.Crew.SuitOn(0));
    }

    [Fact]
    public void RunningLosesOneABreathAndTheSuitPaysOnlyOne()
    {
        var w = Regen(Beyond, batteries: [Full]).Do(new Exert(true) { From = 0 }).Run();
        Breaths(w, 1);
        int air = HeroOf(w).Air, charge = Charge0(w);
        Breaths(w, 3);
        Assert.Equal((air - 3 * (Content.Crew.Exerted - Suit.RegenMost), charge - 3 * Cost), (HeroOf(w).Air, Charge0(w)));
    }

    [Fact]
    public void ThrustingLosesTwoASecondInsteadOfThree()
    {
        var w = Regen(Beyond, batteries: [Full]).Wear(0, Jetpack.Item).Do(new Thrust(true) { From = 0 }).Run();
        Breaths(w, 1);
        int air = HeroOf(w).Air;
        Breaths(w, 2);
        Assert.Equal(air - 2 * Content.Crew.BreathEvery / Jetpack.TicksPerUnit, HeroOf(w).Air);
    }

    [Fact]
    public void ByThePodItMakesAndPaysNothing()
    {
        var w = Regen(Hatch, batteries: [Full]);
        Breaths(w, 3);
        Assert.Equal((w.M.Crew.CapacityOf(0), Full), (HeroOf(w).Air, Charge0(w)));
    }

    [Fact]
    public void SwitchedOffOrOnAnEmptyBeltItIsAPlainSuit()
    {
        foreach (var w in new[] { Regen(Beyond, on: false, batteries: [Full]), Regen(Beyond) })
        {
            int air = HeroOf(w).Air;
            Breaths(w, 3);
            Assert.Equal(air - 3, HeroOf(w).Air);
        }
        Assert.Equal(Full, Charge0(Breaths(Regen(Beyond, on: false, batteries: [Full]), 3)));
    }

    [Fact]
    public void TwoBatteriesOfTenMakeNothingForNeitherHoldsAWholePayment()
    {
        var w = Regen(Beyond, batteries: [10 * S, 10 * S]);
        int air = HeroOf(w).Air;
        Breaths(w, 2);
        Assert.Equal((air - 2, 20 * S), (HeroOf(w).Air, Charge0(w)));
    }

    [Fact]
    public void ARunningDrillPaysFirstAndTheSuitHasWhatIsLeft()
    {
        // Sixteen on the belt at the breath: the drill's three first, then thirteen pay no unit.
        var w = Regen(Beyond, batteries: [Full]).GiveItems(0, ("drill", 1));
        w.Run(Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery - 1);
        w.Put(Belt, new Stack("battery", 1, 16 * S));
        int air = HeroOf(w).Air;
        w.Do(new Run("drill", true) { From = 0 }).Run();
        Assert.Equal(("drill", air - 1, 13 * S), (w.M.Items.Running(0), HeroOf(w).Air, Charge0(w)));
    }

    [Fact]
    public void PastTheFieldTheBreathIsGivenBackAndTheColdPaidInFull()
    {
        // The map in the reference world, its field's left edge 384 cells in: halfway there the cold takes some, not all.
        var w = new Scenario(Layout.Parse(PodRoom).Doubled().InWorld()) { Bare = true }.Wear(0, Helmet.Heavy).Wear(0, Suit.Regen);
        w.Put(Belt, new Stack("battery", 1, Full));
        int left = (Content.World.Width - Content.World.ZoneWidth) / 2 - Field.Margin, cell = Content.World.Height / 2 * Content.World.Width + left / 2;
        int cold = w.M.Crew.EdgeCost(cell);
        Assert.InRange(cold, 1, 40);
        w.Do(new Enter(cell) { From = 0 }).Run().Do(new SuitMode(true) { From = 0 }).Run();
        Breaths(w, 1);
        int air = HeroOf(w).Air, charge = Charge0(w);
        Breaths(w, 2);
        Assert.Equal((air - 2 * cold, charge - 2 * Cost), (HeroOf(w).Air, Charge0(w)));
    }

    [Fact]
    public void InCreativeModeItDrainsNothing()
    {
        var w = Regen(Beyond, batteries: [Full]).Do(new SetCreative(true) { From = 0 }).Run();
        int charge = Charge0(w);
        Breaths(w, 3);
        Assert.Equal(charge, Charge0(w));
    }

    [Fact]
    public void SwitchingOnIsRefusedWithoutABodyWithAModeOrAHeroAndOffIsAlwaysTaken()
    {
        var w = new Scenario(PodRoom).Do(new Enter(Beyond) { From = 0 }).Run();
        w.Do(new SuitMode(true) { From = 0 }).Do(new SuitMode(false) { From = 0 }).Do(new SuitMode(true) { From = 1 }).Run();
        Assert.Equal(["crew.noSuitMode", "crew.noHero"], Reasons(w));
        Assert.Equal("crew.noSuitMode", w.M.Crew.SuitModeProblem(0));
    }

    [Fact]
    public void TakenOffTheModeMakesNothingAndWornAgainItWorksAgain()
    {
        var w = Regen(Beyond, batteries: [Full]).GiveItems(0, (Suit.Basic, 1));
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Suit.Basic);
        w.Do(new Move(new Items.Place(bag), new Target(BodySlot), 1) { From = 0 }).Run();
        Assert.False(w.M.Crew.SuitOn(0));
        Assert.True(HeroOf(w).SuitOn);
        Breaths(w, 1);
        int air = HeroOf(w).Air, charge = Charge0(w);
        Breaths(w, 2);
        Assert.Equal((air - 2, charge), (HeroOf(w).Air, Charge0(w)));
        bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Suit.Regen);
        w.Do(new Move(new Items.Place(bag), new Target(BodySlot), 1) { From = 0 }).Run();
        Assert.True(w.M.Crew.SuitOn(0));
    }

    [Fact]
    public void TheModeIsKeptThroughASaveAndALoad()
    {
        var w = Regen(Beyond, batteries: [Full]).Reload();
        Assert.True(w.M.Crew.SuitOn(0));
    }

    [Fact]
    public void ABatteryGivesAHundredAndTwentyAir() => Assert.Equal(120, Charge.Battery / (Suit.RegenCost * S));

    private sealed record Step(string Type, int Ticks = 0);

    private static readonly Gen<Step> Steps = Gen.OneOf(
        Gen.Elements(0, 1).Select(on => new Step("exert", on)),
        Gen.Elements(0, 1).Select(on => new Step("thrust", on)),
        Gen.Choose(1, 30).Select(t => new Step("wait", t)));

    /// <summary>
    /// Two worlds alike but for the mode: whatever the hero does, the one with it on holds exactly a unit more air for each
    /// payment its belt made. Proven to bite by letting a unit be made for nothing once.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property EveryUnitMadeIsPaidAndEveryPaymentMakesAUnit()
    {
        var steps = from n in Gen.Choose(0, 20) from s in Steps.ArrayOf(n) select s;
        // Small enough that the belt runs dry within a run: the suit then makes nothing.
        var charges = Gen.Choose(0, 40).Select(c => c * S).ArrayOf(3);
        return Prop.ForAll(steps.ToArbitrary(), charges.ToArbitrary(), (all, belt) =>
        {
            var worlds = new[] { Regen(Beyond, true, belt), Regen(Beyond, false, belt) };
            foreach (var w in worlds) w.Wear(0, Jetpack.Item);
            int charge = Charge0(worlds[0]), ahead = HeroOf(worlds[0]).Air - HeroOf(worlds[1]).Air;
            foreach (var s in all)
                foreach (var w in worlds)
                {
                    if (s.Type == "exert") w.Do(new Exert(s.Ticks == 1) { From = 0 });
                    else if (s.Type == "thrust") w.Do(new Thrust(s.Ticks == 1) { From = 0 });
                    w.Run(s.Type == "wait" ? s.Ticks : 1);
                }
            if (HeroOf(worlds[0]).Out > 0 || HeroOf(worlds[1]).Out > 0) return true.ToProperty();
            int made = HeroOf(worlds[0]).Air - HeroOf(worlds[1]).Air - ahead, paid = (charge - Charge0(worlds[0])) / Cost;
            return (made == paid && (charge - Charge0(worlds[0])) % Cost == 0).Label($"made {made}, paid {paid}");
        });
    }
}
