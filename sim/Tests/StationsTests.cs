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
    /// A sealed room aired by a tank piped to a vent, as the furnace's tests: the lifepod, a furnace, and a Former
    /// and an electric smelter, each cabled to a solar panel of its own out under the stars.
    /// </summary>
    private static readonly string[] Rows = ["   PPP   PPP     ", "                 ", "#################", "#.......OOO.....#", "#FF.TT..OOOMM.EE#", "#FF.TTV.OOOMM.EE#", "#################"];
    private static readonly string[] Under = ["", "", "", "", "    o", "    ---"];

    private static Scenario World()
    {
        var w = new Scenario(Layout.Parse(Rows, Under, [((3, 0), (11, 4)), ((9, 0), (14, 4))]));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 4)); i++) w.Run();
        return w;
    }

    /// <summary>The stations of <paramref name="w"/>, in machine id order.</summary>
    private static List<(int Id, Station Station)> StationsIn(Scenario w) =>
        [.. w.M.Grid.Machines.Where(m => Stations.TryGet(m.Kind, out _)).Select(m => (m.Id, Stations.Get(m.Kind)))];

    /// <summary>What <paramref name="item"/> is made of, down to salvage: its crafts' costs and its yields, recursively.</summary>
    private static void Raw(string item, int count, Dictionary<string, int> into)
    {
        if (Crafts.TryGet(item, out var craft))
            foreach (var (part, n) in craft.Cost) Raw(part, n * count, into);
        else into[item] = into.GetValueOrDefault(item) + count;
    }

    /// <summary>
    /// The salvage worth of everything: inventories, every machine's slots, the floor and every queued unit,
    /// a smelted item counted as what it smelts into, down to what smelts no more, so a smelt changes nothing;
    /// the planks apart, which a fire burns.
    /// </summary>
    private static (string Worth, int Planks) Worth(Scenario w, int peers)
    {
        var all = new Dictionary<string, int>(StringComparer.Ordinal);
        void Smelted(string item, int count)
        {
            if (Content.Items.Get(item).Yields is { } yields)
                foreach (var (y, n) in yields) Smelted(y, n * count);
            else Raw(item, count, all);
        }
        void Add(Stack? s)
        {
            if (s != null) Smelted(s.Item, s.Count);
        }
        for (int peer = 0; peer < peers; peer++)
            foreach (var s in w.M.Items.Inventory(peer)) Add(s);
        foreach (var m in w.M.Grid.Machines)
        {
            foreach (var s in w.M.Items.Crate(m.Id)) Add(s);
            foreach (var e in w.M.Crafting.Queue(m.Id)) Raw(e.Recipe, e.Count, all);
        }
        foreach (var s in w.M.Items.Floor) Add(new Stack(s.Item, s.Count));
        int planks = all.GetValueOrDefault("planks");
        all.Remove("planks");
        return (string.Join(",", all.Where(a => a.Value != 0).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}:{a.Value}")), planks);
    }

    /// <summary>A step of the crew: a command at a station, then ticks run.</summary>
    private sealed record Step(int Kind, int Station, string Recipe, int Count, int Slot, int Peer, int Ticks);

    private static readonly string[] Recipes = [.. Crafts.All.Select(c => c.Item)];

    /// <summary>
    /// The invariants of both feeds: items are conserved — what a queued craft took comes back as its units or
    /// a cancel's refund, and a smelt turns an item into its yields and nothing else, planks only burning, one a
    /// fire a tick at most; and no station's queue ever holds more than its <c>QueueMost</c>.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ConservesItemsAcrossBothFeedsAndNeverQueuesPastAStationsMost()
    {
        const int peers = 3;
        var step =
            from kind in Gen.Frequency((5, Gen.Constant(0)), (2, Gen.Constant(1)), (3, Gen.Constant(2)), (3, Gen.Constant(3)))
            from station in Gen.Choose(0, 9)
            from recipe in Gen.Frequency((3, Gen.Elements("wire", "gear", "plate", "tube")), (1, Gen.Elements(Recipes)))
            from count in Gen.Choose(1, 9)
            from slot in Gen.Choose(0, 6)
            from peer in Gen.Choose(0, peers - 1)
            from ticks in Gen.Frequency((6, Gen.Choose(0, 3)), (1, Gen.Choose(10, 300)))
            select new Step(kind, station, recipe, count, slot, peer, ticks);
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World();
            for (int peer = 0; peer < peers; peer++)
                w.GiveItems(peer, ("scrap", 200), ("iron", 100), ("gear", 2), ("wire", 4), ("scrapChunk", 6), ("hullPlate", 3), ("planks", 3), ("rock", 2), ("silicon", 2));
            var stations = StationsIn(w);
            var (worth, planks) = Worth(w, peers);
            foreach (var s in all)
            {
                var (id, station) = stations[s.Station % stations.Count];
                int slots = w.M.Items.Crate(id).Count;
                w.Do(s.Kind switch
                {
                    0 => new Craft(id, s.Recipe, s.Count) { From = s.Peer },
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
                    if (now != worth || left > planks || left < planks - 1) return false;
                    planks = left;
                }
            }
            return true;
        });
    }

    /// <summary>
    /// The fuzz draws crafts and cancels at every station, queued and fed, and at machines that are none: refused as
    /// not here (at the furnace and the smelter, and of what the Former does not make), as no station, unknown, full and unpaid,
    /// and some taken, at both queued stations.
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
            int machine = c switch { Craft k => k.Machine, Cancel k => k.Machine, _ => -1 };
            at[machine] = at.GetValueOrDefault(machine) + 1;
            int refused = w.Refusals.Count;
            w.Do(c).Run();
            if (c is Craft && w.Refusals.Count == refused) taken.Add(machine);
        }
        Assert.All(StationsIn(w), s => Assert.True(at.GetValueOrDefault(s.Id) > 100, $"{s.Station.Machine}: {at.GetValueOrDefault(s.Id)}"));
        var reasons = w.Refusals.Select(r => r.Reason).ToHashSet();
        foreach (var why in new[] { "crafting.notHere", "crafting.noStation", "crafting.unknown", "crafting.full", "crafting.noEntry", "items.notEnough" })
            Assert.Contains(why, reasons);
        string? KindOf(Command c) => c is Craft k ? w.M.Grid.MachineById(k.Machine)?.Kind : null;
        var notHere = w.Refusals.Where(r => r.Reason == "crafting.notHere").Select(r => r.Command).ToList();
        Assert.Contains(notHere, c => KindOf(c) == "former");
        Assert.Contains(notHere, c => KindOf(c) == "furnace");
        Assert.Contains(notHere, c => KindOf(c) == "smelter");
        Assert.Equal(StationsIn(w).Where(s => !s.Station.Fed).Select(s => s.Id).Order(), taken.Order());
    }
}
