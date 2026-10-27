using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Helmets and bodies worn and taken off, and the bare hero under them (2026-10-27-suits-are-worn).</summary>
public class SuitsTests
{
    /// <summary>Nine wide, doubled to 18 (0104): a sealed room with a vent above a tank, the spawn inside it.</summary>
    private const string Room = """
        #########
        #V..S...#
        #TT.....#
        #TT.....#
        #########
        """;

    /// <summary>A pod in a vacuum room, never aired.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private static readonly int Lungs = Content.Crew.Lungs, Cold = Content.Crew.BareCold, Full = Scenario.Full;
    private static readonly int HeadSlot = SlotOf(Content.Helmet.Slot), BodySlot = SlotOf(Content.Suit.Slot);
    private static int SlotOf(string kind) => Content.Inventory.Slots + Content.Inventory.Equipment.ToList().IndexOf(kind);
    private static string Helmet => Content.Helmet.Basic;
    private static string Body => Content.Suit.Basic;
    private const int Pod = 1;

    private static int Cell(int x, int y) => y * 18 + x;
    private static int AtPod(int x, int y) => y * 22 + x;
    private static readonly int Hatch = AtPod(6 + Content.Crew.PodHatch.X, 4 + Content.Crew.PodHatch.Y);
    /// <summary>The first cell right of the pod past its air's reach: vacuum, and no pod.</summary>
    private static readonly int Beyond = AtPod(14, 6);

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;
    private static Enter EnterAt(int cell, int from = 0) => new(cell) { From = from };

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>The pod room with hero 0 brought in at <paramref name="cell"/>, bare unless <paramref name="suited"/>.</summary>
    private static Scenario InVacuum(int cell, bool suited = false)
    {
        var w = new Scenario(PodRoom) { Bare = !suited };
        w.Do(EnterAt(cell)).Run();
        Assert.Empty(w.M.Atmosphere.AiredCells);
        return w;
    }

    /// <summary><see cref="Room"/> aired from its piped tank, hero 0 brought in at the spawn, bare unless <paramref name="suited"/>.</summary>
    private static Scenario Aired(bool suited)
    {
        var w = new Scenario(Room) { Bare = !suited }.Do(new SetCreative(true));
        w.Pipe(Cell(2, 2), Cell(2, 6)).Run();
        int tank = w.M.Grid.Machines.First(m => m.Kind == "tank").Id;
        w.Do(new LoadCanister(tank));
        w.Do(new SetCreative(false)).Run();
        int room = w.M.Rooms.RoomAt(Cell(8, 4))!.Value;
        for (int i = 0; i < 1000 && w.M.Atmosphere.StatusOf(room) != RoomStatus.Aired; i++) w.Run();
        return w.Do(EnterAt(w.M.Crew.Spawn)).Run();
    }

    private static Move Off(Scenario w, int slot, int peer) =>
        new(new Items.Place(slot), new Target(w.M.Items.Inventory(peer).ToList().IndexOf(null)), 1) { From = peer };

    private static Scenario TakeOff(Scenario w, int slot, int peer = 0) => w.Do(Off(w, slot, peer)).Run();

    private static Scenario PutOn(Scenario w, string item, int slot, int peer = 0)
    {
        int bag = w.M.Items.Inventory(peer).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Items.Place(bag), new Target(slot), 1) { From = peer }).Run();
    }

    [Fact]
    public void APeersFirstHeroIsPrintedBareWithItsLungsFull()
    {
        var w = InVacuum(Beyond);
        Assert.Null(w.M.Items.WornIn(0, Content.Helmet.Slot));
        Assert.Null(w.M.Items.WornIn(0, Content.Suit.Slot));
        Assert.Equal((Lungs, Lungs), (HeroOf(w).Air, w.M.Crew.CapacityOf(0)));
    }

    [Fact]
    public void BareInVacuumABreathCostsTheColdBesidesSuitedItDoesNot()
    {
        var bare = Breaths(InVacuum(Beyond), 3);
        Assert.Equal(Lungs - 3 * (1 + Cold), HeroOf(bare).Air);
        Assert.Equal(Cold, bare.M.Crew.Cold(0));
        var suited = Breaths(InVacuum(Beyond, suited: true), 3);
        Assert.Equal(Full - 3, HeroOf(suited).Air);
        Assert.Equal(0, suited.M.Crew.Cold(0));
    }

    [Fact]
    public void TheHelmetAndTheBodyTakenOffInAiredAirCutTheAirToTheLungsAndTheHeroBreathesOnColdFree()
    {
        var w = Aired(suited: true);
        Assert.Equal(Full, HeroOf(w).Air);
        TakeOff(w, HeadSlot);
        TakeOff(w, BodySlot);
        Assert.Empty(w.Refusals);
        Assert.Equal(Lungs, HeroOf(w).Air);
        var bag = w.M.Items.Inventory(0).Take(Content.Inventory.Slots).ToList();
        Assert.Equal((1, 1), (bag.Count(s => s?.Item == Helmet), bag.Count(s => s?.Item == Body)));
        Breaths(w, 5);
        Assert.Equal((Lungs, 0), (HeroOf(w).Air, w.M.Crew.Cold(0)));
    }

    [Fact]
    public void AHelmetPutOnKeepsTheAirThenFillsItFromTheRoom()
    {
        var w = Aired(suited: true);
        TakeOff(w, HeadSlot);
        PutOn(w, Helmet, HeadSlot);
        Assert.Equal((Lungs, Full), (HeroOf(w).Air, w.M.Crew.CapacityOf(0)));
        Breaths(w, 1);
        Assert.Equal(Lungs + Content.Crew.Refill, HeroOf(w).Air);
        Breaths(w, 10);
        Assert.Equal(Full, HeroOf(w).Air);
    }

    [Fact]
    public void AHelmetWithoutABodyHoldsTheAirButTheColdBitesABodyWithoutAHelmetTheOtherWay()
    {
        var helmeted = InVacuum(Beyond, suited: true);
        TakeOff(Breaths(helmeted, 1), BodySlot);
        int air = HeroOf(helmeted).Air;
        Breaths(helmeted, 3);
        Assert.Equal((Full, air - 3 * (1 + Cold), Cold), (helmeted.M.Crew.CapacityOf(0), HeroOf(helmeted).Air, helmeted.M.Crew.Cold(0)));

        var bodied = InVacuum(Beyond, suited: true);
        TakeOff(Breaths(bodied, 1), HeadSlot);
        Assert.Equal(Lungs, HeroOf(bodied).Air);
        Breaths(bodied, 3);
        Assert.Equal((Lungs, Lungs - 3, 0), (bodied.M.Crew.CapacityOf(0), HeroOf(bodied).Air, bodied.M.Crew.Cold(0)));
    }

    [Fact]
    public void AHeroDyingSuitedIsPrintedAgainSuitedAndFullItsSuitNotOnTheBody()
    {
        var w = InVacuum(Beyond, suited: true);
        for (int i = 0; i < 5000 && HeroOf(w).Out == 0; i++) w.Run();
        Assert.True(HeroOf(w).Out > 0);
        Assert.DoesNotContain(w.M.Items.Floor, f => f.Item == Body || f.Item == Helmet);
        w.Run(Content.Crew.PassOut);
        Assert.Equal((0, Full), (HeroOf(w).Out, HeroOf(w).Air));
        Assert.Equal((Helmet, Body), (w.M.Items.WornIn(0, Content.Helmet.Slot), w.M.Items.WornIn(0, Content.Suit.Slot)));
    }

    [Fact]
    public void AHeroDyingBareIsPrintedAgainBare()
    {
        var w = InVacuum(Beyond);
        for (int i = 0; i < 5000 && HeroOf(w).Out == 0; i++) w.Run();
        w.Run(Content.Crew.PassOut);
        Assert.Equal((0, Lungs), (HeroOf(w).Out, HeroOf(w).Air));
    }

    [Fact]
    public void APeerLeavingSuitedAndComingBackIsPrintedInItsKeptSuitFull()
    {
        var w = Breaths(InVacuum(Beyond, suited: true), 5);
        w.Do(new Leave { From = 0 }).Run();
        Assert.Null(w.M.Crew.HeroOf(0));
        w.Do(EnterAt(Beyond)).Run();
        Assert.Equal(Full, HeroOf(w).Air);
    }

    [Fact]
    public void BareByThePodInVacuumAtRestTheAirHoldsAUnitShort()
    {
        var w = Breaths(InVacuum(Hatch), 10);
        Assert.Equal(Lungs - Cold, HeroOf(w).Air);
        Breaths(w, 10);
        Assert.Equal(Lungs - Cold, HeroOf(w).Air);
    }

    [Fact]
    public void ACanisterBreathedBareFillsTheLungsNoFurther()
    {
        var w = Breaths(InVacuum(Beyond), 5).GiveItems(0, (Content.Crew.Canister, 2));
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal(Lungs, HeroOf(w).Air);
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal(["crew.suitFull"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void AJetpackThrustingBarePaysFromTheLungs()
    {
        var w = new Scenario(PodRoom) { Bare = true }.Wear(0, Content.Jetpack.Item);
        w.Do(EnterAt(Beyond)).Run();
        Breaths(w, 1);
        w.Do(new Thrust(true) { From = 0 }).Run();
        Breaths(w, 1);
        Assert.Equal(Lungs - 2 * (1 + Cold) - Content.Crew.BreathEvery / Content.Jetpack.TicksPerUnit, HeroOf(w).Air);
    }

    [Fact]
    public void ThePodMakesTheBasicHelmetAndBodyForNothingKnownFromTheStart()
    {
        foreach (string item in new[] { Helmet, Body })
        {
            Assert.Contains(item, Content.Research.StartKnown);
            Assert.Empty(Content.Crafts.Get(item).Cost);
            var w = new Scenario(Layout.Parse(PodRoom) with { Known = Content.Research.StartKnown }, doubled: true) { Bare = true };
            w.Do(EnterAt(w.M.Crew.Spawn)).Run();
            w.Do(new Craft(Pod, item, 1) { From = 0 }).Run();
            Assert.Empty(w.Refusals);
            w.Run(Content.Crafts.Get(item).Ticks * 1000 / Content.Stations.Get("lifepod").Speed);
            Assert.Contains(w.M.Items.Crate(Pod), s => s?.Item == item);
        }
    }

    private sealed record Step(string Type, int Hero = 0, int Cell = 0, int Ticks = 0);

    private static readonly Gen<Step> Steps = Gen.OneOf(
        from hero in Gen.Choose(0, 2)
        from cell in Gen.Elements(Hatch, Beyond, AtPod(3, 2), -5)
        select new Step("enter", hero, cell),
        from hero in Gen.Choose(0, 2)
        from which in Gen.Elements(0, 1)
        select new Step("swap", hero, which),
        from hero in Gen.Choose(0, 2)
        from thrusting in Gen.Elements(0, 1)
        select new Step("thrust", hero, Ticks: thrusting),
        from hero in Gen.Choose(0, 2) select new Step("refill", hero),
        Gen.Frequency((2, Gen.Choose(1, 20)), (1, Gen.Choose(1, 600))).Select(t => new Step("wait", Ticks: t)));

    [Property(MaxTest = 40)]
    public Property KeepsEveryHerosAirBetweenNothingAndItsCapacityThroughSwapsRefillsColdAndThrust()
    {
        var steps = from n in Gen.Choose(0, 25) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = new Scenario(PodRoom).Wear(0, Content.Jetpack.Item).Wear(1, Content.Jetpack.Item);
            for (int peer = 0; peer < 3; peer++) w.GiveItems(peer, (Content.Crew.Canister, 5));
            foreach (var s in all)
            {
                if (s.Type == "enter") w.Do(EnterAt(s.Cell, s.Hero));
                else if (s.Type == "thrust") w.Do(new Thrust(s.Ticks == 1) { From = s.Hero });
                else if (s.Type == "refill") w.Do(new Refill { From = s.Hero });
                else if (s.Type == "swap")
                {
                    var (item, slot) = s.Cell == 0 ? (Helmet, HeadSlot) : (Body, BodySlot);
                    int bag = w.M.Items.Inventory(s.Hero).ToList().FindIndex(x => x?.Item == item);
                    w.Do(bag >= 0 && bag < Content.Inventory.Slots
                        ? new Move(new Items.Place(bag), new Target(slot), 1) { From = s.Hero }
                        : Off(w, slot, s.Hero));
                }
                w.Run(s.Type == "wait" ? s.Ticks : 1);
                foreach (var hero in w.Crew().Heroes)
                    if (hero.Air < 0 || hero.Air > w.M.Crew.CapacityOf(hero.Id))
                        return false.Label($"hero {hero.Id} holds {hero.Air} of {w.M.Crew.CapacityOf(hero.Id)}");
            }
            return true.ToProperty();
        });
    }
}
