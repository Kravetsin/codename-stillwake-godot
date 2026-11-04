using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Xunit;
using Structure = Stillwake.Content.Structure;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What salvage drones must always keep (salvage-drones), across random walls built and cleared out in space, the door
/// worked, pieces and floating stacks strewn about the zone and past it, crates filled to the brim or not, and canisters.
/// </summary>
public class SalvageInvariantTests
{
    /// <summary>What a run starts with and does: the salvage drones' charges, canisters, pieces, stacks, how full each crate is, then steps.</summary>
    public sealed record Run(
        int[] Charges, int Canisters, Bit[] Pieces, Bit[] Stacks, int FarFull, int NearFull, Step[] Steps);

    /// <summary>A piece of <c>Item</c> or a stack of <c>Count</c> of it at (<c>X</c>, <c>Y</c>), drifting at <c>Speed</c> or still.</summary>
    public sealed record Bit(string Item, int Count, int X, int Y, int Speed);

    /// <summary>A step: a wall built or cleared at a cell out in space, the door worked, then some ticks.</summary>
    public sealed record Step(string Type, int Cell, int Ticks);

    /// <summary>What a net would catch and what it would not, to be taken or left.</summary>
    private static readonly string[] Kinds = ["scrapChunk", "cargoCrate", "scrapChunk", "asteroid"];
    private static readonly string[] Stock = ["iron", "plate", "scrap", "wire"];

    private static readonly int Width = SalvageTests.Width;
    private static readonly int Door = SalvageTests.Cell(26, 2);
    private static readonly int Slots = Content.Machines.Get("crate").Slots!.Value;

    /// <summary>Out in space, the zone and a few columns past it.</summary>
    private static Gen<(int X, int Y)> Spot => from x in Gen.Choose(28, 43) from y in Gen.Choose(0, 9) select (x, y);

    private static Gen<Run> Runs(bool pieces = true)
    {
        var piece = from kind in Gen.Elements(Kinds) from at in Spot from speed in Gen.Elements(0, 0, 20, 80) select new Bit(kind, 1, at.X, at.Y, speed);
        var stack = from item in Gen.Elements(Stock)
                    from count in Gen.Choose(1, 20)
                    from at in Spot
                    from still in Gen.Elements(true, false)
                    select new Bit(item, count, at.X, at.Y, still ? 0 : 1);
        var step = Gen.Frequency(
            (3, from at in Spot from t in Gen.Choose(1, 60) select new Step("wall", SalvageTests.Cell(at.X, at.Y), t)),
            (2, from at in Spot from t in Gen.Choose(1, 60) select new Step("clear", SalvageTests.Cell(at.X, at.Y), t)),
            (1, from t in Gen.Choose(1, 60) select new Step("door", Door, t)),
            (4, from t in Gen.Choose(1, 200) select new Step("wait", 0, t)));
        return from charges in Gen.Choose(Tune.LaunchAt, SalvageTests.Full).ArrayOf().Where(c => c.Length is > 0 and <= 4)
               from canisters in Gen.Choose(0, 6)
               from n in Gen.Choose(0, pieces ? 6 : 0)
               from strewn in piece.ArrayOf(n)
               from m in Gen.Choose(0, 6)
               from floating in stack.ArrayOf(m)
               from far in Gen.Elements(0, 0, Slots - 1, Slots)
               from near in Gen.Elements(0, 0, Slots - 1, Slots)
               from s in Gen.Choose(0, 8)
               from steps in step.ArrayOf(s)
               select new Run(charges, canisters, strewn, floating, far, near, steps);
    }

    /// <summary>The world <paramref name="run"/> starts with.</summary>
    private static Scenario Start(Run run)
    {
        var w = SalvageTests.Out(run.Canisters, run.Charges);
        foreach (var p in run.Pieces) SalvageTests.Drift(w, p.Item, p.X, p.Y, p.Speed);
        foreach (var s in run.Stacks) SalvageTests.Float(w, s.Item, s.Count, s.X, s.Y, still: s.Speed == 0);
        var rock = new Stack("rock", Content.Items.Get("rock").Stack);
        for (int i = 0; i < run.FarFull; i++) w.Put(i, rock, crate: SalvageTests.Far(w));
        for (int i = 0; i < run.NearFull; i++) w.Put(i, rock, crate: SalvageTests.Near(w));
        return w;
    }

    /// <summary>Plays <paramref name="run"/>'s steps and a long wait after, calling <paramref name="check"/> after every tick.</summary>
    private static void Play(Scenario w, Run run, Action check)
    {
        foreach (var s in run.Steps.Append(new Step("wait", 0, 500)))
        {
            if (s.Type == "wall") w.Do(new Place(s.Cell, Placement.Wall()));
            else if (s.Type == "clear") w.Do(new ClearAt(s.Cell, "structure"));
            else if (s.Type == "door") w.Do(new SetDoor(s.Cell, w.M.Grid.StructureAt(s.Cell) == Structure.DoorClosed));
            for (int t = 0; t < s.Ticks; t++)
            {
                w.Run();
                check();
            }
        }
    }

    [Property(MaxTest = 40)]
    public Property NoTwoDronesEverShareATarget() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = Start(run);
            Play(w, run, () =>
            {
                var out_ = SalvageTests.Salvaging(w).Where(d => d.Leg == Leg.Source).ToList();
                var pieces = out_.Where(d => d.Piece != 0).Select(d => d.Piece).ToList();
                var stacks = out_.Where(d => d.Floor != 0).Select(d => d.Floor).ToList();
                Assert.Equal(pieces.Count, pieces.Distinct().Count());
                Assert.Equal(stacks.Count, stacks.Distinct().Count());
            });
        });

    [Property(MaxTest = 40)]
    public Property EveryCanisterSpentLaunchedOneSalvageDrone() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = Start(run);
            var launched = new HashSet<int>();
            Play(w, run, () =>
            {
                foreach (var d in SalvageTests.Salvaging(w)) launched.Add(d.Id);
                Assert.Equal(run.Canisters - SalvageTests.Canisters(w), launched.Count);
                Assert.All(SalvageTests.Salvaging(w), d => Assert.True(d.Air <= Tune.TripAir));
            });
        });

    [Property(MaxTest = 40)]
    public Property NoSalvageDroneIsInASolidCellAndAirNeverGoesNegative() =>
        Prop.ForAll(Runs().ToArbitrary(), run =>
        {
            var w = Start(run);
            Play(w, run, () =>
            {
                foreach (var d in SalvageTests.Salvaging(w))
                {
                    Assert.True(d.Air >= 0, $"drone {d.Id} at {d.Air} air");
                    // Standing at a cell's centre it is never in a wall; between two, a wall just built may close behind it.
                    if (d.X % 1000 == 500 && d.Y % 1000 == 500 && d.X / 1000 + d.Y / 1000 * Width == d.Cell)
                    {
                        int s = w.M.Grid.StructureAt(d.Cell);
                        Assert.True(Doorways.IsDoorway(s) || !Content.Structures.Solid(s), $"drone {d.Id} in a wall at {d.Cell % Width},{d.Cell / Width}");
                    }
                }
            });
        });

    [Property(MaxTest = 40)]
    public Property ItemsAreKeptWhenOnlyStacksAreTaken() =>
        Prop.ForAll(Runs(pieces: false).ToArbitrary(), run =>
        {
            var w = Start(run);
            var start = Totals(w);
            var gone = new List<(string Item, int Count)>();
            var before = Edge(w);
            Play(w, run, () =>
            {
                var kept = w.M.Items.Floor.Select(f => f.Id).ToHashSet();
                foreach (var f in before.Where(f => !kept.Contains(f.Id) && !Lifted(w, f)))
                    gone.Add((f.Item, f.Count));
                before = Edge(w);
                Assert.Equal(start, Sum(Totals(w).Concat(gone)));
            });
        });

    /// <summary>A floor stack as a tick starts: its claiming drone, if any, and whether the tick's move may take it past the grid's edge.</summary>
    private sealed record Lying(int Id, string Item, int Count, int? Drone);

    /// <summary>
    /// The floor stacks this tick's move may take past the grid's edge, where Items' own rule has them gone: afloat they fly on
    /// and drift, elsewhere they fall at most <c>MaxFall</c>. A wall built on a stack in space can send it so; no drone is needed.
    /// </summary>
    private static List<Lying> Edge(Scenario w)
    {
        int width = w.M.Grid.Width, height = w.M.Grid.Height;
        var by = w.M.Logistics.Drones.Where(d => d.Floor != 0).ToDictionary(d => d.Floor, d => d.Id);
        return [.. w.M.Items.Floor.Where(f =>
        {
            bool afloat = w.M.Rooms.Afloat(f.Cell);
            int x = f.X + f.Vx - (afloat && w.M.Grid.BackLayer[f.Cell] != Back.Wall ? Content.Floor.SpaceDrift : 0);
            int y = f.Y + (afloat ? f.Vy : Content.Floor.MaxFall);
            return x < 0 || y <= 0 || x >= width * 1000 || y > height * 1000;
        }).Select(f => new Lying(f.Id, f.Item, f.Count, by.TryGetValue(f.Id, out int d) ? d : null))];
    }

    /// <summary>Whether the drone going for <paramref name="f"/> took it this tick: it holds it now.</summary>
    private static bool Lifted(Scenario w, Lying f) =>
        w.M.Logistics.Drones.Any(d => d.Id == f.Drone && d.Floor == 0 && d.Loaded.Any(s => s.Item == f.Item && s.Count == f.Count));

    private static List<(string Item, int Count)> Sum(IEnumerable<(string Item, int Count)> stacks) =>
        [.. stacks.GroupBy(e => e.Item).Select(g => (g.Key, g.Sum(e => e.Count))).OrderBy(e => e.Key, StringComparer.Ordinal)];

    /// <summary>Every stack in the world by item but the canisters a launch spends: the crates and the bay, the floor, the drones in flight and what they carry.</summary>
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
            all.AddRange(d.Loaded.Select(s => (s.Item, s.Count)));
        }
        return Sum(all.Where(e => e.Item != Content.Crew.Canister));
    }

    [Property(MaxTest = 30)]
    public Property SavingAndLoadingAtARandomTickMidTripPlaysOnAlike() =>
        Prop.ForAll(Runs().ToArbitrary(), Gen.Choose(0, 150).ToArbitrary(), (run, after) =>
        {
            var w = Start(run);
            for (int i = 0; i < 400 && !SalvageTests.Salvaging(w).Any(); i++) w.Run();
            w.Run(after);
            var m = new Registry();
            var copy = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
            Assert.Equal(w.World.Hash(), copy.Hash());
            foreach (var s in run.Steps.Append(new Step("wait", 0, 400)))
            {
                Command[] commands = s.Type switch
                {
                    "wall" => [new Place(s.Cell, Placement.Wall())],
                    "clear" => [new ClearAt(s.Cell, "structure")],
                    "door" => [new SetDoor(s.Cell, w.M.Grid.StructureAt(s.Cell) == Structure.DoorClosed)],
                    _ => [],
                };
                for (int t = 0; t < s.Ticks; t++)
                {
                    w.World.Step(t == 0 ? commands : []);
                    copy.Step(t == 0 ? commands : []);
                }
                Assert.Equal(w.World.Hash(), copy.Hash());
            }
        });
}
