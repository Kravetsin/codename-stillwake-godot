using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>An electric tool runs and draws its power by the tick, touching anything or nothing (0055).</summary>
public class RunTests
{
    private const string Drill = "drill";
    private const int S = Charge.Scale;
    private static readonly int Draw = Content.Items.Get(Drill).Draw!.Value;
    /// <summary>A tick of the drill's draw, stored.</summary>
    private static readonly int Tick = Draw * S;
    private static readonly int Full = Charge.Battery;
    private static readonly int Belt = Inventory.Slots + 2;

    /// <summary>Hull with no room: no air, so a hero aboard breathes its suit dry and passes out.</summary>
    private const string Vacuum = "S.......";

    private static Stack Battery(int charge) => new("battery", 1, charge);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int?[] BeltOf(Scenario w, int peer = 0) =>
        [.. w.M.Items.Inventory(peer).Skip(Belt).Take(3).Select(s => s?.Charge)];

    /// <summary>Hero 0 aboard with the drill in its bag and <paramref name="batteries"/> on its belt, left to right.</summary>
    private static Scenario Holding(params int[] batteries)
    {
        var w = new Scenario(Vacuum).GiveItems(0, (Drill, 1));
        for (int i = 0; i < batteries.Length; i++) w.Put(Belt + i, Battery(batteries[i]));
        return w.Do(new Enter(1)).Run();
    }

    [Fact]
    public void DrawsTheDrawEveryTickItRunsFromTheBeltInOrderTouchingNothing()
    {
        var w = Holding(3 * Tick + 2, Full).Do(new Run(Drill, true)).Run(3);
        Assert.Equal(Drill, w.M.Items.Running(0));
        Assert.Equal([2, Full, null], BeltOf(w));
        // The first cannot pay a whole tick now: the next one pays it, left to right.
        w.Run(2);
        Assert.Equal([2, Full - 2 * Tick, null], BeltOf(w));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void StopsByItselfOnTheFirstTickTheBeltCannotPay()
    {
        var w = Holding(2 * Tick + 1).Do(new Run(Drill, true)).Run(2);
        Assert.Equal(Drill, w.M.Items.Running(0));
        w.Run();
        Assert.Null(w.M.Items.Running(0));
        // What is short of a tick stays where it is.
        Assert.Equal([1, null, null], BeltOf(w));
        w.Run(10);
        Assert.Equal([1, null, null], BeltOf(w));
    }

    [Fact]
    public void StopsDrawingOnRunOffWhateverItNames()
    {
        var w = Holding(Full).Do(new Run(Drill, true)).Run(5).Do(new Run(Drill, false)).Run(10);
        Assert.Null(w.M.Items.Running(0));
        Assert.Equal(Full - 5 * Tick, BeltOf(w)[0]);
        // Off with nothing running is no refusal: the client says what it wants until the sim agrees.
        w.Do(new Run(Drill, false)).Run();
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void RunsOneThingAHeroAndTwoHeroesEachFromItsOwnBelt()
    {
        var w = Holding(Full).GiveItems(1, (Drill, 1)).Put(Belt, Battery(Full), peer: 1).Do(new Enter(1) { From = 1 });
        w.Do(new Run(Drill, true)).Do(new Run(Drill, true)).Do(new Run(Drill, true) { From = 1 }).Run(4);
        Assert.Equal((Drill, Drill), (w.M.Items.Running(0), w.M.Items.Running(1)));
        Assert.Equal(Full - 4 * Tick, BeltOf(w, 0)[0]);
        Assert.Equal(Full - 4 * Tick, BeltOf(w, 1)[0]);
    }

    [Fact]
    public void PassingOutStopsTheRunAndOneLandingOnAHeroOutStopsOnItsFirstTick()
    {
        var w = Holding(Full, Full, Full).Do(new Run(Drill, true));
        int ticks = 0;
        for (; ticks < 4000 && w.M.Crew.HeroOf(0)!.Out == 0; ticks++) w.Run();
        Assert.True(w.M.Crew.HeroOf(0)!.Out > 0);
        Assert.Null(w.M.Items.Running(0));
        int left = w.M.Items.BeltCharge(0).Charge;
        // It drew every tick till the hero passed out, that tick too: items draws before crew.
        Assert.Equal(3 * Full - ticks * Tick, left);
        // A run landing on a hero already out pays its first tick before crew stops it (accepted, 0055).
        w.Do(new Run(Drill, true)).Run();
        Assert.Null(w.M.Items.Running(0));
        Assert.Equal(left - Tick, w.M.Items.BeltCharge(0).Charge);
        w.Run(20);
        Assert.Equal(left - Tick, w.M.Items.BeltCharge(0).Charge);
    }

    [Fact]
    public void LeavingStopsTheRunSoNoOneDrainsABeltLeftBehind()
    {
        var w = Holding(Full).Do(new Run(Drill, true)).Run(2).Do(new Leave()).Run(10);
        Assert.Null(w.M.Items.Running(0));
        Assert.Equal(Full - 2 * Tick, BeltOf(w)[0]);
    }

    [Fact]
    public void StopsOnceItsToolIsNoLongerCarriedAndRunsFromAnySlot()
    {
        var w = new Scenario(Vacuum).Put(20, new Stack(Drill, 1)).Put(Belt, Battery(Full)).Do(new Enter(1)).Run();
        w.Do(new Run(Drill, true)).Run(3);
        Assert.Equal(Drill, w.M.Items.Running(0));
        w.Do(new Drop(new Slot(20), 1, 4, 1)).Run();
        Assert.Null(w.M.Items.Running(0));
        Assert.Equal(Full - 3 * Tick, BeltOf(w)[0]);
    }

    [Fact]
    public void DrawsNothingInCreativeModeWithOrWithoutABattery()
    {
        var w = Holding(Tick).Do(new SetCreative(true)).Do(new Run(Drill, true)).Run(100);
        var bare = Holding().Do(new SetCreative(true)).Do(new Run(Drill, true)).Run(100);
        Assert.Empty(w.Refusals);
        Assert.Empty(bare.Refusals);
        Assert.Equal((Drill, Drill), (w.M.Items.Running(0), bare.M.Items.Running(0)));
        Assert.Equal(Tick, BeltOf(w)[0]);
    }

    [Fact]
    public void RefusesWhatHasNoDrawWhatIsNotCarriedAndABeltThatCannotPayATick()
    {
        var w = Holding();
        w.Do(new Run("fireAxe", true)).Do(new Run("battery", false)).Do(new Run(Drill, true) { From = 1 }).Do(new Run(Drill, true)).Run();
        Assert.Equal(["items.notElectric", "items.notElectric", "items.notHeld", "items.flat"], Reasons(w));
        // A battery short of a tick on the belt, a full one in the bag: neither runs it.
        var short_ = Holding(Tick - 1).Put(3, Battery(Full)).Do(new Run(Drill, true)).Run();
        Assert.Equal(["items.flat"], Reasons(short_));
        Assert.Equal([Tick - 1, null, null], BeltOf(short_));
        Assert.Equal(Battery(Full), short_.M.Items.Inventory(0)[3]);
        Assert.Null(w.M.Items.Running(0));
        Assert.Null(w.M.Items.Running(1));
    }

    [Fact]
    public void KeepsARunOverASaveAndALoad()
    {
        var w = Holding(Full).Do(new Run(Drill, true)).Run(2).Reload().Run(3);
        Assert.Equal(Drill, w.M.Items.Running(0));
        Assert.Equal(Full - 5 * Tick, BeltOf(w)[0]);
    }

    // Invariants.

    private abstract record Step;
    private sealed record Sending(Command Sent) : Step;
    private sealed record Waiting(int Ticks) : Step;

    private static readonly Gen<int> Places = Gen.Frequency((3, Gen.Choose(0, 4)), (3, Gen.Choose(Belt, Belt + 2)));

    private static Gen<Step[]> Steps()
    {
        var peer = Gen.Choose(0, 1);
        var run = from turnsOn in Gen.Frequency((3, Gen.Constant(true)), (1, Gen.Constant(false)))
                  from item in Gen.Frequency((6, Gen.Constant(Drill)), (1, Gen.Constant("fireAxe")))
                  from p in peer
                  select (Step)new Sending(new Run(item, turnsOn) { From = p });
        var move = from src in Places
                   from dst in Places
                   from p in peer
                   select (Step)new Sending(new Move(new Slot(src), new Target(dst), 1) { From = p });
        var drop = from src in Places from p in peer select (Step)new Sending(new Drop(new Slot(src), 1, 4, 1) { From = p });
        var wait = Gen.Frequency((3, Gen.Choose(1, 20)), (1, Gen.Choose(100, 600))).Select(t => (Step)new Waiting(t));
        var step = Gen.Frequency((4, run), (3, move), (1, drop), (4, wait));
        return from n in Gen.Choose(1, 40) from s in step.ArrayOf(n) select s;
    }

    /// <summary>Two heroes aboard with a drill each and batteries of many charges, on the belt and in the bag.</summary>
    private static Scenario Stocked() =>
        new Scenario(Vacuum).Put(0, new Stack(Drill, 1)).Put(0, new Stack(Drill, 1), peer: 1)
            .Put(Belt, Battery(10 * Tick + 1)).Put(Belt + 1, Battery(Full / 20)).Put(1, Battery(Tick + 2)).Put(2, Battery(Full))
            .Put(Belt, Battery(5 * Tick), peer: 1).Put(1, Battery(Tick - 1), peer: 1)
            .Do(new Enter(1)).Do(new Enter(2) { From = 1 }).Run();

    private static bool Carries(Scenario w, int peer, string item) => w.M.Items.Inventory(peer).Any(s => s?.Item == item);

    [Property(MaxTest = 200)]
    public Property ChargeNeverGoesBelowNothingAndARunNeverOutlivesABeltThatCannotPay() =>
        Prop.ForAll(Steps().ToArbitrary(), steps =>
        {
            var w = Stocked();
            int ran = 0;
            void Holds()
            {
                foreach (int peer in new[] { 0, 1 })
                {
                    foreach (var s in w.M.Items.Inventory(peer).OfType<Stack>())
                        Assert.InRange(s.Charge, 0, Content.Items.Get(s.Item).Capacity ?? 0);
                    if (w.M.Items.Running(peer) is { } item) Assert.True(Carries(w, peer, item), $"{peer} runs {item} it does not carry");
                }
            }
            foreach (var step in steps)
            {
                if (step is Sending c)
                {
                    w.Do(c.Sent).Run();
                    Holds();
                    continue;
                }
                for (int t = 0; t < ((Waiting)step).Ticks; t++)
                {
                    var before = new[] { 0, 1 }.Select(p => (Run: w.M.Items.Running(p), Belt: w.M.Items.BeltCharge(p).Charge, Pays: w.M.Items.CanDrain(p, Draw))).ToArray();
                    w.Run();
                    Holds();
                    foreach (int peer in new[] { 0, 1 })
                    {
                        var (run, belt, pays) = before[peer];
                        string? now = w.M.Items.Running(peer);
                        int charge = w.M.Items.BeltCharge(peer).Charge;
                        bool fainted = w.M.Crew.HeroOf(peer)!.Out > 0;
                        if (run == null) Assert.Equal((null, belt), (now, charge));
                        // A run goes on only by paying its tick; one the belt cannot pay stops, paying nothing.
                        else if (now != null) { Assert.Equal(belt - Tick, charge); ran++; }
                        else if (pays) Assert.True(fainted && charge == belt - Tick, "a run that could pay stopped");
                        else Assert.Equal(belt, charge);
                    }
                }
            }
            return true.ToProperty().Classify(ran > 0, "ran");
        });
}
