using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Xunit;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What drones must always keep (0062): every item kept and each drone once, across random stock, machines switched off
/// and on, walls built and taken down and the door worked while they fly; and no drone ever flat in the air, however
/// far round the walls a job takes it.
/// </summary>
public class LogisticsInvariantTests
{
    /// <summary>What a run starts with and does: drones docked and their charge, the stock of the crates and the furnace, then steps.</summary>
    public sealed record Run(
        int[] Charges, (string Item, int Count)[] Left, (string Item, int Count)[] Right, (string Item, int Count)? Input,
        (string Item, int Count)[] Outputs, Step[] Steps);

    /// <summary>A step: a wall built or taken down at a cell inside the hull, the door worked, a machine switched, then some ticks.</summary>
    public sealed record Step(string Type, int Cell, int Ticks);

    /// <summary>Nothing that burns and nothing a furnace would smelt without fuel: what is there only moves.</summary>
    private static readonly string[] Stock = ["iron", "plate", "scrapChunk", "gear", "wire", "rock"];

    private const int Width = 24;
    private static readonly int Door = 10 + 2 * Width;
    private static readonly int[] Switched = [LogisticsTests.Furnace, LogisticsTests.Left, LogisticsTests.Right];

    private static Gen<(string Item, int Count)> Stacked =>
        from item in Gen.Elements(Stock) from count in Gen.Choose(1, Content.Items.Get(item).Stack) select (item, count);

    private static Gen<int[]> Charges => Gen.Choose(Tune.LaunchAt, Content.Charge.Drone).ArrayOf().Where(c => c.Length is > 0 and <= 4);

    private static Gen<Run> Runs()
    {
        var inside = from x in Gen.Choose(1, Width - 2) from y in Gen.Choose(1, 3) select y * Width + x;
        var step = Gen.Frequency(
            (3, from c in inside from t in Gen.Choose(1, 60) select new Step("wall", c, t)),
            (3, from c in inside from t in Gen.Choose(1, 60) select new Step("clear", c, t)),
            (2, from t in Gen.Choose(1, 60) select new Step("door", Door, t)),
            (2, from m in Gen.Elements(Switched) from t in Gen.Choose(1, 60) select new Step("off", m, t)),
            (2, from m in Gen.Elements(Switched) from t in Gen.Choose(1, 60) select new Step("on", m, t)),
            (4, from t in Gen.Choose(1, 200) select new Step("wait", 0, t)));
        return from charges in Charges
               from n in Gen.Choose(0, 6)
               from left in Stacked.ArrayOf(n)
               from m in Gen.Choose(0, 4)
               from right in Stacked.ArrayOf(m)
               from hasInput in Gen.Elements(true, false)
               from input in Stacked
               from k in Gen.Choose(0, 3)
               from outputs in Stacked.ArrayOf(k)
               from s in Gen.Choose(0, 12)
               from steps in step.ArrayOf(s)
               select new Run(charges, left, right, hasInput ? input : null, outputs, steps);
    }

    /// <summary>Every stack in the world by item: the crates, the furnace, the bay, the floor, the drones in flight and their cargo.</summary>
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
    private static int Drones(Scenario w, int bay) =>
        LogisticsTests.Docked(w, bay) + w.M.Logistics.Drones.Count + LogisticsTests.OnFloor(w, Tune.Drone);

    /// <summary>Puts the drones in bay <paramref name="bay"/>, each holding its charge.</summary>
    private static void Dock(Scenario w, int bay, int[] charges)
    {
        for (int i = 0; i < charges.Length; i++) w.Put(i, LogisticsTests.Drone(charges[i]), crate: bay);
    }

    [Property(MaxTest = 60)]
    public Property ItemsAreKeptEachDroneIsOnceAndNoTwoGoForOneStack() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = LogisticsTests.TwoRooms(0);
            Dock(w, LogisticsTests.Bay, run.Charges);
            if (run.Left.Length > 0) w.FillCrate(LogisticsTests.Left, run.Left);
            if (run.Right.Length > 0) w.FillCrate(LogisticsTests.Right, run.Right);
            if (run.Input is { } input) w.Put(0, new Stack(input.Item, input.Count), crate: LogisticsTests.Furnace);
            for (int i = 0; i < run.Outputs.Length; i++) w.Put(2 + i, new Stack(run.Outputs[i].Item, run.Outputs[i].Count), crate: LogisticsTests.Furnace);
            var start = Totals(w);
            foreach (var s in run.Steps.Append(new Step("wait", 0, 300)))
            {
                if (s.Type == "wall") w.Do(new Place(s.Cell, Placement.Wall()));
                else if (s.Type == "clear") w.Do(new ClearAt(s.Cell, "structure"));
                else if (s.Type == "door") w.Do(new ToggleDoor(s.Cell));
                else if (s.Type is "on" or "off") w.Do(new SetServed(s.Cell, s.Type == "on"));
                for (int t = 0; t < s.Ticks; t++)
                {
                    w.Run();
                    Assert.Equal(start, Totals(w));
                    Assert.Equal(run.Charges.Length, Drones(w, LogisticsTests.Bay));
                    var drones = w.M.Logistics.Drones;
                    Assert.Equal(drones.Count, drones.Select(d => d.Id).Distinct().Count());
                    var going = drones.Where(d => d.Leg == Leg.Source).Select(d => (d.Source, d.Slot)).ToList();
                    Assert.Equal(going.Count, going.Distinct().Count());
                }
            }
        });

    /// <summary>
    /// The Former's two rooms, a panel under the stars feeding it alone: the bay is unpowered, so a drone works until its
    /// charge runs low and stays home. Width 24, the hull from row 2.
    /// </summary>
    private static readonly string[] Pressing =
    [
        "PPP                     ",
        "                        ",
        "########################",
        "#.........D............#",
        "#RR....TT.D...MM.......#",
        "#RR.KK.TTVD..VMM..KK...#",
        "########################",
    ];

    private static readonly string[] PressingUnder = ["", "", "", "", "       o", "       -------"];

    /// <summary>Columns a wall may run down, all three rows open: one row left open in each makes every way through zigzag.</summary>
    private static readonly int[] Columns = [3, 6, 11, 12, 16, 17, 20, 21, 22];

    /// <summary>
    /// Endless plates at the Former from a crate of iron, drones charged anywhere from <c>LaunchAt</c> to full and walls
    /// down the rooms with a gap in each, so a job takes a drone far round them, well past the Manhattan length it was
    /// checked against: every tick, no drone in flight has spent its last charge, and none has dropped. Nothing changes
    /// where drones may fly while they do; the door is worked.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property NoDroneIsEverFlatInTheAir() =>
        Prop.ForAll(Charges.ToArbitrary(), Gen.SubListOf(Columns).SelectMany(c => Gen.Choose(3, 5).ArrayOf(c.Count).Select(g => c.Zip(g).ToArray())).ToArbitrary(), Gen.Choose(0, 3).ToArbitrary(), (charges, walls, doors) =>
        {
            var rows = Pressing.Select(r => r.ToCharArray()).ToArray();
            foreach (var (x, gap) in walls)
                for (int y = 3; y <= 5; y++)
                    if (y != gap) rows[y][x] = '#';
            var w = LogisticsTests.Aired([.. rows.Select(r => new string(r))], PressingUnder, 0, [((0, 0), (14, 4))]);
            int bay = w.M.Grid.MachinesOf(Tune.Bay).Single().Id, former = w.M.Grid.MachinesOf("former").Single().Id;
            Dock(w, bay, charges);
            w.FillCrate(w.M.Grid.MachinesOf("crate")[0].Id, ("iron", 500), ("iron", 500));
            w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run();
            w.Do(new SetEndless(former, 1, true));
            for (int t = 0; t < 3000; t++)
            {
                if (doors > 0 && t % 400 == 0) w.Do(new ToggleDoor(Door + 2 * Width));
                w.Run();
                Assert.All(w.M.Logistics.Drones, d => Assert.True(d.Stack.Charge > 0, $"tick {t}: drone {d.Id} flat at cell {d.Cell}"));
                Assert.Equal(0, LogisticsTests.OnFloor(w, Tune.Drone));
            }
        });
}
