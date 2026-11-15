using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Fuel;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Xunit;
using FuelContent = Stillwake.Content.Fuel;
using ItemPlace = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Fuel (fuel-liquid): biofuel in litres in the stores of the machines that hold it, poured in from a hopper a can a tick, piped
/// on from store to store, drawn out as cans, lost with a store taken apart; and old saves' cans poured into the stores.
/// </summary>
public class FuelTests
{
    /// <summary>
    /// A cell a character: fuel tanks A at (0, 0) and B at (6, 0), inputs (0, 2) and (6, 2), outputs (3, 2) and (9, 2); a biofuel
    /// generator at (12, 0), its fuel input (12, 1). Uncabled, so nothing wants its fire.
    /// </summary>
    private const string Line = """
        !!!!..!!!!..qqqqqq
        !!!!..!!!!..qqqqqq
        !!!!..!!!!..qqqqqq
        !!!!..!!!!..qqqqqq
        """;

    private const int L = FuelContent.Litre;

    private static Scenario World() => new Scenario(Line, cells: true).GiveItems(0, (FuelContent.Item, 20), ("tube", 20));

    /// <summary>A's output to B's input; B's output by a joint at (11, 2) and one at (11, 1) to the generator's input.</summary>
    private static Scenario Piped()
    {
        var w = World();
        return w.Do(new LayPipe(w.At(3, 2), w.At(6, 2))).Do(new LayPipe(w.At(9, 2), w.At(11, 2))).Do(new LayPipe(w.At(11, 2), w.At(11, 1)))
            .Do(new LayPipe(w.At(11, 1), w.At(12, 1))).Run();
    }

    private static int A(Scenario w) => w.M.Grid.MachineAt(w.At(0, 0))!.Id;
    private static int B(Scenario w) => w.M.Grid.MachineAt(w.At(6, 0))!.Id;
    private static int Generator(Scenario w) => w.M.Grid.MachineAt(w.At(12, 0))!.Id;
    private static int[] Held(Scenario w) => [w.M.Fuel.FuelIn(A(w)), w.M.Fuel.FuelIn(B(w)), w.M.Fuel.FuelIn(Generator(w))];
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Cans(Scenario w, int peer) => w.M.Items.Totals(peer).Where(t => t.Item == FuelContent.Item).Sum(t => t.Count);

    /// <summary>Moves <paramref name="count"/> cans peer 0 carries into machine <paramref name="machine"/>'s hopper.</summary>
    private static Scenario Hop(Scenario w, int machine, int count)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == FuelContent.Item);
        return w.Do(new Move(new ItemPlace(from), new Target(0, machine), count));
    }

    [Fact]
    public void ItsHopperPoursACanATickAndWaitsWhileFull()
    {
        var w = World();
        int tank = A(w);
        Hop(w, tank, 3).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((L, 2), (w.M.Fuel.FuelIn(tank), w.M.Items.Crate(tank)[0]?.Count));
        w.Run(2);
        Assert.Equal((3 * L, null), (w.M.Fuel.FuelIn(tank), w.M.Items.Crate(tank)[0]));
        w.M.Fuel.Fill(tank, 97 * L);
        Hop(w, tank, 1).Run(3);
        Assert.Equal((100 * L, 1), (w.M.Fuel.FuelIn(tank), w.M.Items.Crate(tank)[0]?.Count));
        Assert.Equal(0, w.M.Fuel.RoomIn(tank));
    }

    [Fact]
    public void DrawsCansOutOfAnyStoreOrSaysWhyNot()
    {
        var w = World();
        int tank = A(w), carried = Cans(w, 0);
        w.M.Fuel.Fill(tank, 5 * L + L / 2);
        w.Do(new DrawFuel(tank, 2)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((3 * L + L / 2, carried + 2), (w.M.Fuel.FuelIn(tank), Cans(w, 0)));
        // A count under one is one; a part litre is no can.
        w.Do(new DrawFuel(tank, 0)).Run().Do(new DrawFuel(tank, 3)).Do(new DrawFuel(999)).Run();
        w.GiveItems(2, ("planks", Content.Inventory.Slots * Content.Items.Get("planks").Stack));
        w.Do(new DrawFuel(tank) { From = 2 }).Run();
        Assert.Equal(["fuel.low", "fuel.noStore", "items.full"], Reasons(w));
        Assert.Equal((2 * L + L / 2, carried + 3), (w.M.Fuel.FuelIn(tank), Cans(w, 0)));
        // The generator's store is drawn as a tank's.
        w.M.Fuel.Fill(Generator(w), 2 * L);
        w.Do(new DrawFuel(Generator(w), 2)).Run();
        Assert.Equal(0, w.M.Fuel.FuelIn(Generator(w)));
    }

    [Fact]
    public void PoursALineIntoItsLastStoreFuelFlowATick()
    {
        var w = Piped();
        Assert.Empty(w.Refusals);
        w.M.Fuel.Fill(A(w), 10 * L);
        w.Run();
        // A gives B its flow and B, later in id order, pours it on in the same tick.
        Assert.Equal([10 * L - FuelContent.Flow, 0, FuelContent.Flow], Held(w));
        w.Run(10 * L / FuelContent.Flow - 1);
        Assert.Equal([0, 0, 10 * L], Held(w));
    }

    /// <summary>The generator full at 20 litres, the tank before it fills; nothing is made or lost.</summary>
    [Fact]
    public void FillsTheLastStoreAndThenTheOneBeforeIt()
    {
        var w = Piped();
        w.M.Fuel.Fill(A(w), 30 * L);
        w.Run(30 * L / FuelContent.Flow + 10);
        Assert.Equal([0, 10 * L, 20 * L], Held(w));
    }

    [Fact]
    public void LosesItsFuelTakenApart()
    {
        var w = World();
        w.M.Fuel.Fill(A(w), 5 * L);
        int tank = A(w);
        w.Do(new ClearAt(w.At(0, 0), "machine")).Run();
        Assert.Null(w.M.Grid.MachineById(tank));
        Assert.Equal(0, w.M.Fuel.FuelIn(tank));
    }

    /// <summary>The shared line (Stores): a store never pours into itself, one loaded fuller than it holds takes nothing, a loop evens nothing.</summary>
    [Fact]
    public void ALinePoursOnlyInToRoomNeverIntoItselfAndLosesNothing()
    {
        List<Stored> stores = [new() { Machine = 1, Amount = 50 }, new() { Machine = 2, Amount = 120 }, new() { Machine = 3, Amount = 0 }];
        stores.RemoveAll(s => s.Amount == 0);
        // 1 → [1, 2, 3]; 2 → [3]; 3 → [1]: a loop; 2 holds over its 100.
        IEnumerable<int> Takers(int giver) => giver switch { 1 => [1, 2, 3], 2 => [3], _ => [1] };
        Stores.Pour(stores, [1, 2, 3], Takers, _ => 100, 30);
        // 1 skips itself and 2 (over its capacity), gives 3 its 30; 2 gives 3 its 30; 3 gives 1 its 30 back.
        Assert.Equal([(1, 50), (2, 90), (3, 30)], stores.Select(s => (s.Machine, s.Amount)));
        Assert.Equal(170, stores.Sum(s => s.Amount));
    }

    /// <summary>
    /// Fuel moves, never made or lost (fuel-liquid): cans hopped in, cans drawn out, stores piped on and waits, in any order, keep the
    /// litres in stores, hoppers and the hero's hands as they were; and every store holds between nothing and its capacity.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property KeepsEveryLitreAsItMovesBetweenHoppersStoresAndHands()
    {
        var step = Gen.OneOf(
            Gen.Elements(0, 1, 2).SelectMany(m => Gen.Choose(1, 6).Select(n => ("hop", m, n))),
            Gen.Elements(0, 1, 2).SelectMany(m => Gen.Choose(1, 4).Select(n => ("draw", m, n))),
            Gen.Frequency((3, Gen.Choose(1, 60)), (1, Gen.Choose(60, 1200))).Select(t => ("wait", 0, t)));
        var steps = from n in Gen.Choose(1, 15) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = Piped();
            int[] machines = [A(w), B(w), Generator(w)];
            int Total() => machines.Sum(m => w.M.Fuel.FuelIn(m) + w.M.Items.HeldFor(m, FuelContent.Item) * L) + Cans(w, 0) * L;
            int start = Total();
            foreach (var (kind, m, n) in all)
            {
                if (kind == "hop" && Cans(w, 0) > 0) Hop(w, machines[m], Math.Min(n, Cans(w, 0))).Run();
                else if (kind == "draw") w.Do(new DrawFuel(machines[m], n)).Run();
                else w.Run(n);
                if (Total() != start) return false.Label($"{Total()} after {kind} {m} {n}, from {start}");
                foreach (int id in machines)
                    if (w.M.Fuel.FuelIn(id) is < 0 or > 100 * L) return false.Label($"store {id} holds {w.M.Fuel.FuelIn(id)}");
                if (w.M.Fuel.FuelIn(Generator(w)) > 20 * L) return false.Label("the generator over its 20 litres");
            }
            return true.ToProperty();
        });
    }

    /// <summary>
    /// Old saves (format 65): the cans in a fermenter's output, a pod's and a generator's slot go into their stores, over capacity if
    /// they were; the fermenter keeps its input alone; a crate keeps its cans; a flight carries its cans as fuel.
    /// </summary>
    [Fact]
    public void AnOldSavesCansArePouredIntoTheStores()
    {
        var root = JsonNode.Parse("""
            {"slices": {
              "grid": {"machines": [{"id": 3, "kind": "fermenter"}, {"id": 4, "kind": "dropPod"}, {"id": 5, "kind": "crate"}, {"id": 6, "kind": "biofuelGenerator"}]},
              "items": {"crates": [
                {"id": 3, "slots": [{"item": "biomass", "count": 2}, {"item": "biofuel", "count": 64}]},
                {"id": 4, "slots": [{"item": "biofuel", "count": 20}]},
                {"id": 5, "slots": [{"item": "biofuel", "count": 100}, null]},
                {"id": 6, "slots": [{"item": "biofuel", "count": 7}]}]},
              "landing": {"flights": [{"peer": 0, "load": {"item": "biofuel", "count": 15}}]}}}
            """)!.AsObject();
        var slices = Migrations.Steps[65 - Migrations.Oldest](root)["slices"]!;
        Assert.Equal("""[{"machine":3,"amount":64000},{"machine":4,"amount":20000},{"machine":6,"amount":7000}]""", slices["fuel"]!["stores"]!.ToJsonString());
        Assert.Equal("""[{"id":3,"slots":[{"item":"biomass","count":2}]},{"id":5,"slots":[{"item":"biofuel","count":100},null]}]""", slices["items"]!["crates"]!.ToJsonString());
        Assert.Equal("""[{"peer":0,"load":null,"fuel":15000}]""", slices["landing"]!["flights"]!.ToJsonString());
    }
}
