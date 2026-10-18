using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Inventory = Stillwake.Content.Inventory;
using ItemSpecs = Stillwake.Content.Items;
using Looks = Stillwake.Content.Looks;
using Loot = Stillwake.Content.Loot;
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>The Handbook's record (0066): which kinds of debris the world has found, and what came out of each.</summary>
public class DebrisFoundTests
{
    /// <summary>A room on the left, space right of it: pieces spawn at the right edge in any row (as <c>DebrisTests</c>' map).</summary>
    private static readonly Layout Space = Layout.Parse(
    [
        " #####              ",
        " #...#              ",
        " #...#              ",
        " #.S.#              ",
        " #...#              ",
        " #...#              ",
        " #...#              ",
        " #####              ",
    ]);
    private const int Width = 20;
    private static int At(int x, int y) => y * Width + x;
    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>The record as it is now: kinds in order, each with its items in order.</summary>
    private static List<(string Kind, string[] Items)> Found(Scenario w) =>
        w.M.Debris.Found.Select(f => (f.Kind, f.Items.ToArray())).ToList();

    /// <summary>The record in words, a kind a line: "supplyPod: canister, scrap".</summary>
    private static List<string> Said(Scenario w) => Found(w).Select(f => $"{f.Kind}: {string.Join(", ", f.Items)}").ToList();

    /// <summary>The looks of <paramref name="kind"/> recorded, in order; none while it is not found.</summary>
    private static int[] LooksOf(Scenario w, string kind) => w.M.Debris.Found.FirstOrDefault(f => f.Kind == kind)?.Looks.ToArray() ?? [];

    /// <summary>The first piece out, made a <paramref name="kind"/>, and hero 0 in space by it; in a world of <paramref name="seed"/>.</summary>
    private static Scenario Out(string kind, uint seed = 1)
    {
        var w = new Scenario(Space, seed).Run(DebrisSpec.Every);
        w.M.Debris.Pieces[0].Item = kind;
        return w.Do(new Enter(At(12, 1))).Run();
    }

    /// <summary>The next piece out, once the stream has spawned it, made a <paramref name="kind"/>; its id.</summary>
    private static int Next(Scenario w, string kind)
    {
        while (w.M.Debris.Pieces.Count == 0) w.Run();
        var piece = w.M.Debris.Pieces[0];
        piece.Item = kind;
        return piece.Id;
    }

    [Fact]
    public void RecordsAKindCaughtAndWhatItsRollGaveAndASmallPieceAsItself()
    {
        var w = Out("cargoCrate");
        Assert.Empty(w.M.Debris.Found);
        w.Do(new Grab(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(["cargoCrate: planks"], Said(w));
        w.Do(new Grab(Next(w, "scrapChunk"))).Run();
        Assert.Equal(["cargoCrate: planks", "scrapChunk: scrapChunk"], Said(w));
        // Saved: kept across a save and a load.
        Assert.Equal(["cargoCrate: planks", "scrapChunk: scrapChunk"], Said(w.Reload()));
        // The look the crate wore, the one its id picks for every peer.
        Assert.Equal([Looks.Of("cargoCrate", 1)], LooksOf(w, "cargoCrate"));
    }

    [Fact]
    public void RecordsTheLookOfEachPieceCaughtOnceInTheOrderFirstSeen()
    {
        // Supply pods, ids running on: their four looks come round, the fifth pod's again, recorded once.
        var w = Out("supplyPod");
        var ids = new List<int> { 1 };
        w.Do(new Grab(1)).Run();
        for (int ticks = 0; ids.Count < 5; ticks++)
        {
            Assert.True(ticks < 2000, "too few pods caught");
            int id = Next(w, "supplyPod");
            // One whose middle drifts in a row of the room's walls cannot be caught: it is left to drift on.
            if (w.M.Debris.GrabProblem(0, id) != null)
            {
                w.Run();
                continue;
            }
            ids.Add(id);
            w.Do(new Grab(id)).Run();
        }
        var looks = ids.Select(id => id % Looks.CountOf("supplyPod")).ToList();
        Assert.True(looks.Distinct().Count() < looks.Count, "no look came round twice");
        Assert.Equal(looks.Distinct(), LooksOf(w, "supplyPod"));
        Assert.Equal(LooksOf(w, "supplyPod"), LooksOf(w.Reload(), "supplyPod"));
    }
    [Fact]
    public void RecordsAWreckOnlyOnTheHitThatBreaksItWithWhatFellOutOfIt()
    {
        const string wreck = "cargoContainer";
        int hits = (WreckageSpec.HpOf(wreck)!.Value + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;
        var w = Out(wreck);
        for (int i = 0; i < hits - 1; i++) w.Do(new Hit(1, Axe));
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Debris.Pieces);
        Assert.Empty(w.M.Debris.Found);
        w.Do(new Hit(1, Axe)).Run();
        Assert.Empty(w.M.Debris.Pieces);
        var (kind, items) = Assert.Single(Found(w));
        Assert.Equal(wreck, kind);
        // Exactly what lies on the floor, in the order the loot's entries gave it.
        var fell = w.M.Items.Floor.Select(s => s.Item).ToHashSet();
        Assert.NotEmpty(fell);
        Assert.Equal(Loot.Of(wreck)!.Select(e => e.Item).Where(fell.Contains), items);
        Assert.Equal([Looks.Of(wreck, 1)], LooksOf(w, wreck));
    }

    [Fact]
    public void LeavesAnItemARollMissedUnseenTillALaterPieceGivesIt()
    {
        // The first world whose first supply pod has no seeds: one in two rolls misses.
        var w = Enumerable.Range(1, 40).Select(seed => Out("supplyPod", (uint)seed).Do(new Grab(1)).Run())
            .First(s => s.M.Debris.CaughtFrom(1)!.All(g => g.Item != "seed"));
        Assert.Equal(["supplyPod: canister, scrap"], Said(w));
        // Pod after pod, till one's roll gives seeds: then the seed is seen, after what was.
        for (int pods = 0; pods < 40 && Found(w)[0].Items.Length < 3; pods++)
        {
            int id = Next(w, "supplyPod");
            w.Do(new Grab(id)).Run();
        }
        Assert.Equal(["supplyPod: canister, scrap, seed"], Said(w));
    }

    [Fact]
    public void RecordsWhatFloatsOffAFullInventoryAsSeen()
    {
        var w = Out("cargoCrate");
        w.GiveItems(0, ("canister", Inventory.Slots * ItemSpecs.Get("canister").Stack));
        w.Do(new Grab(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([], w.M.Debris.CaughtFrom(1)!);
        Assert.Contains(w.M.Items.Floor, s => s.Item == "planks");
        Assert.Equal(["cargoCrate: planks"], Said(w));
    }

    [Fact]
    public void RecordsNothingForAGrabOrHitRefusedNorAReel()
    {
        var w = new Scenario(Space).Run(DebrisSpec.Every);
        w.M.Debris.Pieces[0].Item = "supplyPod";
        // Aboard: refused, not outside.
        w.Do(new Grab(1)).Run();
        w.Do(new Enter(At(12, 1))).Run();
        // Gone, never was; a wreck by hand, too big; a reel.
        w.Do(new Grab(9)).Do(new Reel(1, 14_000, 3_000)).Run();
        int wreck = Next(w, "tankCluster");
        w.Do(new Grab(wreck)).Run();
        Assert.Equal(["debris.notOutside", "debris.gone", "debris.tooBig"], w.Refusals.Select(r => r.Reason));
        Assert.Empty(w.M.Debris.Found);
    }

    [Fact]
    public void UpgradesAFormat17SaveToAWorldThatHasFoundNothingAndPlaysOn()
    {
        string old = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format-17.json"));
        Assert.Null(JsonNode.Parse(old)!["slices"]!["debris"]!["found"]);
        var m = new Registry();
        var world = World.Load(Migrations.Upgrade(old), m.All);
        Assert.Empty(m.Debris.Found);
        for (int i = 0; i < 200; i++) world.Step([]);
        Assert.Contains("\"found\":[]", world.Save());
    }

    // The record's invariants, whatever is caught, broken, reeled or refused.

    private sealed record Step(int Action, int Kind, int Times, int Ticks);

    /// <summary>Every kind any zone spawns, in content order.</summary>
    private static readonly string[] Kinds = DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).Select(k => k.Item).Distinct().ToArray();

    /// <summary>What a piece of <paramref name="kind"/> can give: its loot's items, or itself.</summary>
    private static string[] Gives(string kind) => Loot.Of(kind)?.Select(e => e.Item).ToArray() ?? [kind];

    [Property(MaxTest = 80)]
    public Property OnlyGrowsKindsOnceEachItemsAmongWhatTheirKindGivesAndLooksAmongItsOwn()
    {
        var step = from action in Gen.Choose(0, 5)
                   from kind in Gen.Choose(0, Kinds.Length - 1)
                   from times in Gen.Choose(1, 40)
                   from ticks in Gen.Frequency((3, Gen.Choose(1, 20)), (1, Gen.Choose(150, 300)))
                   select new Step(action, kind, times, ticks);
        var cases = from seed in Gen.Choose(1, 10_000) from steps in step.ArrayOf() select ((uint)seed, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var w = new Scenario(Space, c.Item1).Run(DebrisSpec.Every);
            var before = Found(w);
            var beforeLooks = before.Select(f => LooksOf(w, f.Kind)).ToList();
            foreach (var s in c.steps)
            {
                var pieces = w.M.Debris.Pieces;
                // An unhurt piece takes the kind drawn, so every kind is caught or broken now and then.
                if (pieces.Count > 0 && pieces[0].Damage == 0) pieces[0].Item = Kinds[s.Kind];
                int id = pieces.Count > 0 ? pieces[0].Id : s.Kind;
                switch (s.Action)
                {
                    case 0: w.Do(new Grab(id)); break;
                    case 1: for (int i = 0; i < s.Times; i++) w.Do(new Hit(id, Axe)); break;
                    case 2: w.Do(new Reel(id, 14_000, 3_000)); break;
                    case 3: w.Do(new Enter(At(12, 1))); break;
                    case 4: w.Do(new Enter(At(3, 3))); break;
                    default: w.Do(new Grab(id + 1)); break;
                }
                w.Run(s.Ticks);
                var now = Found(w);
                if (now.Count < before.Count) return false.Label("a kind was taken away");
                for (int k = 0; k < now.Count; k++)
                {
                    var (kind, items) = now[k];
                    if (k < before.Count && (before[k].Kind != kind || !items.Take(before[k].Items.Length).SequenceEqual(before[k].Items)))
                        return false.Label($"{kind}'s record changed but by growing");
                    if (!Kinds.Contains(kind)) return false.Label($"{kind} spawns in no zone");
                    if (now.Count(f => f.Kind == kind) > 1) return false.Label($"{kind} recorded twice");
                    if (items.Distinct().Count() != items.Length) return false.Label($"{kind} sees an item twice");
                    if (items.FirstOrDefault(i => !Gives(kind).Contains(i)) is { } stray) return false.Label($"{kind} gave {stray}");
                    var looks = LooksOf(w, kind);
                    if (k < beforeLooks.Count && !looks.Take(beforeLooks[k].Length).SequenceEqual(beforeLooks[k])) return false.Label($"{kind}'s looks changed but by growing");
                    if (looks.Length == 0 || looks.Distinct().Count() != looks.Length) return false.Label($"{kind} with no look, or one twice");
                    if (looks.Any(l => l < 0 || l >= Looks.CountOf(kind))) return false.Label($"{kind} wore a look it has not");
                }
                before = now;
                beforeLooks = now.Select(f => LooksOf(w, f.Kind)).ToList();
            }
            return true.ToProperty();
        });
    }
}
