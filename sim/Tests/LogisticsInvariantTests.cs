using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Xunit;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What drones must always keep (0062), across random stock, roles, requests, walls built and taken down inside the
/// ship and the door worked while they fly: every item kept, each drone once, no request overfilled.
/// </summary>
public class LogisticsInvariantTests
{
    /// <summary>What a run starts with and does: drones docked, the crates' roles and stock, the requests, then steps.</summary>
    public sealed record Run(
        int Drones, bool LeftProvides, bool RightRequests, (string Item, int Count)[] Left, (string Item, int Count)[] Right,
        (string Item, int Count)[] Furnace, (string Item, int Count)[] Wanted, Step[] Steps);

    /// <summary>A step: a wall built or taken down at a cell inside the hull, the door worked, then some ticks.</summary>
    public sealed record Step(string Type, int Cell, int Ticks);

    /// <summary>Nothing that burns and nothing a furnace would smelt without fuel: what is there only moves.</summary>
    private static readonly string[] Stock = ["iron", "plate", "scrapChunk", "gear", "wire", "rock"];

    private const int Width = 24;
    private static readonly int Door = 10 + 2 * Width;

    private static Gen<(string Item, int Count)[]> Stacks(int most) =>
        from n in Gen.Choose(0, most)
        from all in (from item in Gen.Elements(Stock) from count in Gen.Choose(1, 60) select (item, count)).ArrayOf(n)
        select all;

    /// <summary>Requests of distinct items, so what one machine holds of an item is held against one request.</summary>
    private static Gen<(string Item, int Count)[]> Requests(int most) =>
        from n in Gen.Choose(0, most)
        from items in Gen.Shuffle(Stock)
        from counts in Gen.Choose(1, 40).ArrayOf(n)
        select items.Take(n).Zip(counts).Select(p => (p.First, p.Second)).ToArray();

    private static Gen<Run> Runs()
    {
        var inside = from x in Gen.Choose(1, Width - 2) from y in Gen.Choose(1, 3) select y * Width + x;
        var step = Gen.Frequency(
            (3, from c in inside from t in Gen.Choose(1, 60) select new Step("wall", c, t)),
            (3, from c in inside from t in Gen.Choose(1, 60) select new Step("clear", c, t)),
            (2, from t in Gen.Choose(1, 60) select new Step("door", Door, t)),
            (4, from t in Gen.Choose(1, 200) select new Step("wait", 0, t)));
        return from drones in Gen.Choose(1, 4)
               from leftProvides in Gen.Elements(true, false)
               from rightRequests in Gen.Elements(true, false)
               from left in Stacks(6)
               from right in Stacks(4)
               from furnace in Requests(Content.Machines.Get("furnace").Inputs!.Value)
               from wanted in Requests(4)
               from n in Gen.Choose(0, 12)
               from steps in step.ArrayOf(n)
               select new Run(drones, leftProvides, rightRequests, left, right, furnace, wanted, steps);
    }

    /// <summary>Every stack in the world: the crates, the furnace, the bay, the floor, the drones in flight and their cargo.</summary>
    private static List<(string Item, int Count)> Totals(Scenario w)
    {
        var all = new List<(string Item, int Count)>();
        foreach (var m in w.M.Grid.Machines)
            foreach (var s in w.M.Items.Crate(m.Id))
                if (s != null) all.Add((s.Item, s.Count));
        all.AddRange(w.M.Items.Floor.Select(f => (f.Item, f.Count)));
        foreach (var d in w.M.Logistics.Drones)
        {
            all.Add((d.Stack.Item, d.Stack.Count));
            if (d.Cargo is { } c) all.Add((c.Item, c.Count));
        }
        return [.. all.GroupBy(e => e.Item).Select(g => (g.Key, g.Sum(e => e.Count))).OrderBy(e => e.Key, StringComparer.Ordinal)];
    }

    /// <summary>Each drone once: docked in a bay, in flight, or lying on the floor.</summary>
    private static int Drones(Scenario w) =>
        LogisticsTests.Docked(w, LogisticsTests.Bay) + w.M.Logistics.Drones.Count + LogisticsTests.OnFloor(w, Tune.Drone);

    private static void NoneOverfilled(Scenario w, int machine)
    {
        foreach (var r in w.M.Logistics.RequestsOf(machine))
            Assert.True(w.M.Items.HeldFor(machine, r.Item) <= r.Count, $"machine {machine} holds {w.M.Items.HeldFor(machine, r.Item)} {r.Item} of {r.Count} asked");
    }

    [Property(MaxTest = 60)]
    public Property ItemsAreKeptEachDroneIsOnceAndNoRequestIsOverfilled() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = LogisticsTests.TwoRooms(run.Drones);
            if (run.Left.Length > 0) w.FillCrate(LogisticsTests.Left, run.Left);
            // A requester starts empty: what it holds at the start was never asked for.
            if (run.Right.Length > 0 && !run.RightRequests) w.FillCrate(LogisticsTests.Right, run.Right);
            w.Do(new SetRole(LogisticsTests.Left, run.LeftProvides ? Role.Provider : Role.Storage))
                .Do(new SetRole(LogisticsTests.Right, run.RightRequests ? Role.Requester : Role.Storage))
                .Do(new SetRequests(LogisticsTests.Furnace, [.. run.Furnace.Select(r => new Request(r.Item, r.Count))]));
            if (run.RightRequests) w.Do(new SetRequests(LogisticsTests.Right, [.. run.Wanted.Select(r => new Request(r.Item, r.Count))]));
            var start = Totals(w);
            foreach (var s in run.Steps.Append(new Step("wait", 0, 300)))
            {
                if (s.Type == "wall") w.Do(new Place(s.Cell, Placement.Wall()));
                else if (s.Type == "clear") w.Do(new ClearAt(s.Cell, "structure"));
                else if (s.Type == "door") w.Do(new ToggleDoor(s.Cell));
                for (int t = 0; t < s.Ticks; t++)
                {
                    w.Run();
                    Assert.Equal(start, Totals(w));
                    Assert.Equal(run.Drones, Drones(w));
                    Assert.Equal(w.M.Logistics.Drones.Count, w.M.Logistics.Drones.Select(d => d.Id).Distinct().Count());
                    NoneOverfilled(w, LogisticsTests.Furnace);
                    NoneOverfilled(w, LogisticsTests.Right);
                }
            }
        });
}
