using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Craft = Stillwake.Sim.Crafting.Craft;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>Stations (0057): what holds across every crafting machine, queued and fed alike.</summary>
public class StationsTests
{
    /// <summary>
    /// A sealed room aired by a tank piped to a vent, as the furnace's tests: the lifepod, a furnace, and a Former,
    /// an electric smelter, a circuit assembler and a grow capsule, each cabled to a solar panel of its own out under
    /// the stars.
    /// </summary>
    private static readonly string[] Rows =
    [
        "   PPP   PPP   PPP PPP ",
        "                       ",
        "#######################",
        "#.......OOO.........gg#",
        "#FF.TT..OOOMM.EE.YY.gg#",
        "#FF.TTV.OOOMM.EE.YY.gg#",
        "#######################",
    ];
    private static readonly string[] Under = ["", "", "", "", "    o", "    ---"];

    private static Scenario World()
    {
        var w = new Scenario(Layout.Parse(Rows, Under, [((3, 0), (11, 4)), ((9, 0), (14, 4)), ((15, 0), (17, 4)), ((19, 0), (20, 3))]));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 4)); i++) w.Run();
        return w;
    }

    /// <summary>The stations of <paramref name="w"/>, in machine id order.</summary>
    private static List<(int Id, Station Station)> StationsIn(Scenario w) =>
        [.. w.M.Grid.Machines.Where(m => Stations.TryGet(m.Kind, out _)).Select(m => (m.Id, Stations.Get(m.Kind)))];

    /// <summary>
    /// What <paramref name="item"/> is worth, down to what is neither crafted nor smelts: its craft's cost, or what it
    /// smelts into, recursively; so a craft of silicon and a smelt of it count alike. A seed counts as itself: a crop
    /// makes more than it was (0060), which <see cref="Grown"/> accounts for.
    /// </summary>
    private static void Raw(string item, int count, Dictionary<string, int> into)
    {
        var spec = Content.Items.Get(item);
        if (Crafts.TryGet(item, out var craft))
            foreach (var (part, n) in craft.Cost) Raw(part, n * count, into);
        else if (spec is { Yields: { } yields, Returns: null })
            foreach (var (y, n) in yields) Raw(y, n * count, into);
        else into[item] = into.GetValueOrDefault(item) + count;
    }

    /// <summary>What one crop adds to the worth, beyond its seed: its yields, as <see cref="Raw"/> counts them.</summary>
    private static readonly Dictionary<string, int> Crop = CropWorth();

    private static Dictionary<string, int> CropWorth()
    {
        var crop = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (y, n) in Content.Items.Get("seed").Yields!) Raw(y, n, crop);
        return crop;
    }

    /// <summary>
    /// Whether the worth went from <paramref name="was"/> to <paramref name="now"/> by crops alone, at most
    /// <paramref name="most"/> of them: each adds its yields' worth, and its seed comes back as none, one or two, so the
    /// seeds change by at most one a crop either way; nothing else changes.
    /// </summary>
    private static bool Grown(Dictionary<string, int> was, Dictionary<string, int> now, int most)
    {
        var (from, to) = (new Dictionary<string, int>(was), new Dictionary<string, int>(now));
        int seeds = to.GetValueOrDefault("seed") - from.GetValueOrDefault("seed");
        from.Remove("seed");
        to.Remove("seed");
        for (int crops = 0; crops <= most; crops++)
        {
            if (Math.Abs(seeds) <= crops && Same(Added(from, crops), to)) return true;
        }
        return false;
    }

    private static Dictionary<string, int> Added(Dictionary<string, int> worth, int crops)
    {
        var sum = new Dictionary<string, int>(worth, StringComparer.Ordinal);
        foreach (var (item, n) in Crop) sum[item] = sum.GetValueOrDefault(item) + n * crops;
        return sum;
    }

    private static bool Same(Dictionary<string, int> a, Dictionary<string, int> b) =>
        a.Where(e => e.Value != 0).OrderBy(e => e.Key, StringComparer.Ordinal).SequenceEqual(b.Where(e => e.Value != 0).OrderBy(e => e.Key, StringComparer.Ordinal));

    /// <summary>
    /// The salvage worth of everything: inventories, every machine's slots, the floor and every queued unit,
    /// a smelted item counted as what it smelts into, down to what smelts no more, so a smelt changes nothing;
    /// the planks apart, which a fire burns.
    /// </summary>
    private static (Dictionary<string, int> Worth, int Planks) Worth(Scenario w, int peers)
    {
        var all = new Dictionary<string, int>(StringComparer.Ordinal);
        void Add(Stack? s)
        {
            if (s != null) Raw(s.Item, s.Count, all);
        }
        for (int peer = 0; peer < peers; peer++)
            foreach (var s in w.M.Items.Inventory(peer)) Add(s);
        foreach (var m in w.M.Grid.Machines)
        {
            foreach (var s in w.M.Items.Crate(m.Id)) Add(s);
            // A paid entry's units are paid for; one fed from the inputs holds only its unit begun.
            var queue = w.M.Crafting.Queue(m.Id);
            for (int i = 0; i < queue.Count; i++)
                Raw(queue[i].Recipe, !queue[i].FromInputs ? queue[i].Count : i == 0 && w.M.Crafting.Progress(m.Id) > 0 ? 1 : 0, all);
        }
        foreach (var s in w.M.Items.Floor) Add(new Stack(s.Item, s.Count));
        int planks = all.GetValueOrDefault("planks");
        all.Remove("planks");
        return (all, planks);
    }

    /// <summary>A step of the crew: a command at a station, then ticks run.</summary>
    private sealed record Step(int Kind, int Station, string Recipe, int Count, int Slot, int Peer, int Ticks);

    private static readonly string[] Recipes = [.. Crafts.All.Select(c => c.Item)];

    /// <summary>
    /// The invariants of both feeds and both processes: items are conserved — what a queued craft took, paid when queued
    /// or a unit at a time from the inputs, endless or not, comes back as its units or a cancel's refund, a smelt turns an item into its yields and nothing else, planks only burning, one
    /// a fire a tick at most, and a crop turns a seed into its yields and none, one or two seeds, one a capsule a tick
    /// at most (0060); and no station's queue ever holds more than its <c>QueueMost</c>. A step now and then ripens
    /// the capsule's crop to its last tick, as hours at work would, so crops complete within a run.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ConservesItemsAcrossBothFeedsAndNeverQueuesPastAStationsMost()
    {
        const int peers = 3;
        var step =
            from kind in Gen.Frequency((5, Gen.Constant(0)), (2, Gen.Constant(1)), (3, Gen.Constant(2)), (3, Gen.Constant(3)), (2, Gen.Constant(4)), (5, Gen.Constant(5)), (1, Gen.Constant(6)))
            from station in Gen.Choose(0, 9)
            from recipe in Gen.Frequency((3, Gen.Elements("wire", "gear", "plate", "tube")), (2, Gen.Elements("circuit", "solarCell")), (1, Gen.Elements(Recipes)))
            from count in Gen.Choose(1, 9)
            from slot in Gen.Choose(0, 11)
            from peer in Gen.Choose(0, peers - 1)
            from ticks in Gen.Frequency((6, Gen.Choose(0, 3)), (1, Gen.Choose(10, 300)))
            select new Step(kind, station, recipe, count, slot, peer, ticks);
        var steps = from n in Gen.Choose(0, 60) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World();
            for (int peer = 0; peer < peers; peer++)
                w.GiveItems(peer, ("scrap", 200), ("iron", 100), ("gear", 2), ("wire", 4), ("scrapChunk", 6), ("hullPlate", 3), ("planks", 3), ("rock", 2), ("silicon", 6), ("glass", 4), ("seed", 10), ("biomass", 4));
            var stations = StationsIn(w);
            var fed = stations.Where(s => (Machines.Get(s.Station.Machine).Inputs ?? 0) > 0 && !s.Station.Fed).ToList();
            int capsules = stations.Count(s => s.Station.Process == "grow");
            // Each capsule planted from the start, so ripening brings crops in often.
            foreach (var (id, _) in stations.Where(s => s.Station.Process == "grow")) w.FillCrate(id, ("seed", 10));
            var (worth, planks) = Worth(w, peers);
            foreach (var s in all)
            {
                // Crafts from the inputs go to the stations with inputs, of what they make, piling up behind a head waiting.
                var (id, station) = s.Kind == 5 ? fed[s.Station % fed.Count] : stations[s.Station % stations.Count];
                int slots = w.M.Items.Crate(id).Count;
                if (s.Kind == 4)
                {
                    Ripen(w);
                    continue;
                }
                w.Do(s.Kind switch
                {
                    0 => new Craft(id, s.Recipe, s.Count) { From = s.Peer },
                    5 => new Craft(id, station.Rows.SelectMany(r => r).ElementAt(s.Slot % station.Rows.Sum(r => r.Count)), s.Count, FromInputs: true) { From = s.Peer },
                    6 => new SetEndless(id, s.Count, s.Slot % 3 != 0) { From = s.Peer },
                    1 => new Cancel(id, s.Count) { From = s.Peer },
                    // Planks into a fed station's fuel, anything else into its input; a queued one refuses it, taking nothing.
                    2 => new Move(new Slot(s.Slot), new Target(w.M.Items.Inventory(s.Peer)[s.Slot]?.Item == "planks" ? 1 : 0, id), s.Count) { From = s.Peer },
                    // Now and then a slot emptied into an inventory: else the outputs fill and units wait.
                    _ => new Move(new Slot(s.Slot % slots, id), new Target(), 999) { From = s.Peer },
                });
                for (int t = 0; t <= s.Ticks; t++)
                {
                    w.Run();
                    foreach (var (machine, st) in stations)
                        if (w.M.Crafting.Queue(machine).Count > st.QueueMost) return false;
                    var (now, left) = Worth(w, peers);
                    if (!Grown(worth, now, capsules) || left > planks || left < planks - 1) return false;
                    (worth, planks) = (now, left);
                }
            }
            return true;
        });
    }

    /// <summary>Every grow capsule growing a seed is set to its crop's last tick of work: hours at work, as far as items go.</summary>
    private static void Ripen(Scenario w)
    {
        var state = (CraftingState)w.M.Crafting.Slice;
        foreach (var capsule in w.M.Grid.MachinesOf("growCapsule"))
        {
            if (w.M.Crafting.WorkOf(capsule.Id) is not int work) continue;
            state.Smelts.RemoveAll(s => s.Machine == capsule.Id);
            state.Smelts.Add(new Smelt(capsule.Id, w.M.Items.Crate(capsule.Id)[0]!.Item, work - 1000));
        }
        state.Smelts.Sort((a, b) => a.Machine.CompareTo(b.Machine));
    }

    [Fact]
    public void ProcessesAtAFedStationWhatIsOfItsProcessAndNoHotterThanIt()
    {
        var (furnace, smelter, capsule) = (Stations.Get("furnace"), Stations.Get("smelter"), Stations.Get("growCapsule"));
        Assert.True(CraftingModule.Processes(furnace, "rock") && CraftingModule.Processes(furnace, "hullPlate"));
        // Silicon wants the smelter's heat; planks burn, but nothing is made of them; a seed is grown, never smelted.
        Assert.False(CraftingModule.Processes(furnace, "silicon") || CraftingModule.Processes(furnace, "planks") || CraftingModule.Processes(furnace, "seed"));
        Assert.True(CraftingModule.Processes(smelter, "silicon") && CraftingModule.Processes(smelter, "rock"));
        Assert.True(CraftingModule.Processes(capsule, "seed"));
        Assert.False(CraftingModule.Processes(capsule, "rock"));
    }

    /// <summary>
    /// The fuzz draws crafts and cancels at every station, queued and fed, and at machines that are none: refused as
    /// not here (at the furnace, the smelter and the grow capsule, and of what the Former and the assembler do not
    /// make), as no station, unknown, full and unpaid, and some taken, at every queued station.
    /// </summary>
    [Fact]
    public void TheFuzzDrawsCraftsAtEveryStationRefusedOnesToo()
    {
        var w = World();
        var rng = new Rng(new RngState { S = 7 });
        var at = new Dictionary<int, int>();
        var taken = new HashSet<int>();
        for (int i = 0; i < 3000; i++)
        {
            // Creative mode now and then, as the items fuzz switches it: crafts paid for nothing fill the queues.
            if (i % 50 == 0) w.Do(new SetCreative(i % 100 == 0)).Run();
            if (w.M.Crafting.Fuzz(rng, i % 3) is not { } c) continue;
            int machine = c switch { Craft k => k.Machine, Cancel k => k.Machine, SetEndless k => k.Machine, _ => -1 };
            at[machine] = at.GetValueOrDefault(machine) + 1;
            int refused = w.Refusals.Count;
            w.Do(c).Run();
            if (c is Craft && w.Refusals.Count == refused) taken.Add(machine);
        }
        Assert.All(StationsIn(w), s => Assert.True(at.GetValueOrDefault(s.Id) > 100, $"{s.Station.Machine}: {at.GetValueOrDefault(s.Id)}"));
        var reasons = w.Refusals.Select(r => r.Reason).ToHashSet();
        foreach (var why in new[] { "crafting.notHere", "crafting.noStation", "crafting.unknown", "crafting.full", "crafting.noEntry", "items.notEnough", "crafting.noInputs", "crafting.paid" })
            Assert.Contains(why, reasons);
        string? KindOf(Command c) => c is Craft k ? w.M.Grid.MachineById(k.Machine)?.Kind : null;
        var notHere = w.Refusals.Where(r => r.Reason == "crafting.notHere").Select(r => r.Command).ToList();
        Assert.Contains(notHere, c => KindOf(c) == "former");
        Assert.Contains(notHere, c => KindOf(c) == "furnace");
        Assert.Contains(notHere, c => KindOf(c) == "smelter");
        Assert.Contains(notHere, c => KindOf(c) == "assembler");
        Assert.Contains(notHere, c => KindOf(c) == "growCapsule");
        Assert.Equal(StationsIn(w).Where(s => !s.Station.Fed).Select(s => s.Id).Order(), taken.Order());
    }
}
