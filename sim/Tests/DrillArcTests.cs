using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The tool bench, slice 1 (0098): a drill carries its arc module on its stack, through every move, crate, floor, sort,
/// surface and death; fitted, it hits twice as hard for twice the draw.
/// </summary>
public class DrillArcTests
{
    private const string Drill = "drill", Arc = "arcModule";
    private static readonly Stack Fitted = new(Drill, 1, Module: Arc);
    private static readonly int Belt = Spec.Inventory.Slots + 2;
    private const int S = Spec.Charge.Scale;
    private static readonly int Full = Spec.Charge.Battery;
    private static Stack Battery(int charge) => new("battery", 1, charge);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Stack? At(Scenario w, int slot, int peer = 0) => w.M.Items.Inventory(peer)[slot];

    /// <summary>Open ground over a row of walls, as DigTests': soil at 1 to 3, stone at 4 to 6, bedrock at 7.</summary>
    private static Scenario Ground() =>
        new(Layout.Parse("........\n........\n########\n########").WithMaterials("........\n........\n.ssstttb\n.ssstttb").OnPlanet());

    private static int CrackAt(Scenario w, int cell) => w.M.Building.Cracks.FirstOrDefault(c => c.Cell == cell)?.Damage ?? 0;

    // Damage and draw.

    [Fact]
    public void AFittedDrillHitsTwiceAsHardAndAPlainOneOrNoneCarriedAsTheWeaponSays()
    {
        var w = Ground().Put(0, Fitted).GiveItems(1, (Drill, 1));
        Assert.Equal((2, 1, 1), (w.M.Items.Damage(0, Drill), w.M.Items.Damage(1, Drill), w.M.Items.Damage(2, Drill)));
        // The axe takes no module: its own, whatever else is carried.
        Assert.Equal(Spec.Weapons.Axe.Damage, w.M.Items.Damage(0, Spec.Weapons.Axe.Item));
        Assert.Throws<InvalidOperationException>(() => w.M.Items.Damage(0, "iron"));
    }

    [Fact]
    public void WithTwoDrillsTheFittedOneIsTheOneThatWorksWhereverItLies()
    {
        var w = Ground().GiveItems(0, (Drill, 1)).Put(5, Fitted);
        Assert.Equal(Fitted, w.M.Items.ToolStack(0, Drill));
        Assert.Equal(new Stack(Drill, 1), w.M.Items.ToolStack(1, Drill) ?? new Stack(Drill, 1));
        Assert.Null(w.M.Items.ToolStack(2, Drill));
        Assert.Equal(2, w.M.Items.Damage(0, Drill));
        w.Put(5, null!);
        Assert.Equal(new Stack(Drill, 1), w.M.Items.ToolStack(0, Drill));
        Assert.Equal(1, w.M.Items.Damage(0, Drill));
    }

    [Fact]
    public void AFittedDrillCracksStoneTwiceAPlainOnesWorthAndBreaksItInHalfTheHits()
    {
        var plain = Ground().GiveItems(0, (Drill, 1)).Do(new SetCreative(true)).Do(new Run(Drill, true));
        var fitted = Ground().Put(0, Fitted).Do(new SetCreative(true)).Do(new Run(Drill, true));
        int stone = 2 * 8 + 5, strength = Spec.Materials.Get("stone").Strength;
        plain.Do(HewTesting.Dig(plain, stone, Drill)).Run();
        fitted.Do(HewTesting.Dig(fitted, stone, Drill)).Run();
        Assert.Equal((4, 8), (CrackAt(plain, stone), CrackAt(fitted, stone)));
        int hits = 1;
        while (fitted.M.Grid.WallAt(stone) != null && hits < strength)
        {
            fitted.Do(HewTesting.Dig(fitted, stone, Drill)).Run();
            hits++;
        }
        Assert.Equal((strength + 7) / 8, hits);
        Assert.Empty(fitted.Refusals);
        Assert.Empty(plain.Refusals);
    }

    [Fact]
    public void AFittedDrillDrawsSixATickAPlainOneThreeAndRefusesToRunOnABeltShortOfSix()
    {
        int plain = Spec.Items.Get(Drill).Draw!.Value, fitted = Boost.Cost(plain, Spec.Modules.Arc.Adds(Drill, Spec.Stat.Draw));
        Assert.Equal((3, 6), (plain, fitted));
        var w = new Scenario("S.......").Put(0, Fitted).Put(Belt, Battery(Full)).Do(new Enter(1)).Run();
        w.Do(new Run(Drill, true)).Run(4);
        Assert.Equal(Full - 4 * fitted * S, At(w, Belt)!.Charge);
        Assert.Equal(Drill, w.M.Items.Running(0));

        // A belt holding a tick of the plain drill but not of the fitted one: flat, to run and to hit.
        var short_ = new Scenario("S.......").Put(0, Fitted).Put(Belt, Battery(5 * S)).Do(new Enter(1)).Run();
        Assert.Equal("items.flat", short_.M.Items.WeaponProblem(0, Drill, running: false));
        short_.Do(new Run(Drill, true)).Run();
        Assert.Equal(["items.flat"], Reasons(short_));
        // The same belt runs a plain drill.
        var plainShort = new Scenario("S.......").GiveItems(0, (Drill, 1)).Put(Belt, Battery(5 * S)).Do(new Enter(1)).Run();
        plainShort.Do(new Run(Drill, true)).Run();
        Assert.Empty(plainShort.Refusals);
        Assert.Equal(2 * S, plainShort.M.Items.Inventory(0)[Belt]!.Charge);
    }

    [Fact]
    public void ARunStopsOnTheTickItsBeltCannotPayTheFittedDrawThoughItCouldPayAPlainOne()
    {
        var w = new Scenario("S.......").Put(0, Fitted).Put(Belt, Battery(6 * S + 4 * S)).Do(new Enter(1)).Run();
        w.Do(new Run(Drill, true)).Run();
        Assert.Equal(Drill, w.M.Items.Running(0));
        w.Run();
        Assert.Null(w.M.Items.Running(0));
        Assert.Equal(4 * S, At(w, Belt)!.Charge);
    }

    /// <summary>A wreck in space, hero 0 out by it carrying <paramref name="drill"/>, creative so no belt is needed.</summary>
    private static Scenario Facing(Stack drill)
    {
        var w = new Scenario(Layout.Parse(
        [
            " #####              ",
            " #...#              ",
            " #...#              ",
            " #.S.#              ",
            " #...#              ",
            " #...#              ",
            " #...#              ",
            " #####              ",
        ]), doubled: true).Run(DebrisSpec.Every);
        w.M.Debris.Pieces[0].Item = "hullSection";
        return w.Put(0, drill).Do(new SetCreative(true)).Do(new Enter(w.At(12, 1))).Run();
    }

    [Fact]
    public void AFittedDrillTakesTwiceAPlainOnesHitPointsOffAWreck()
    {
        var plain = Facing(new Stack(Drill, 1));
        var fitted = Facing(Fitted);
        foreach (var w in new[] { plain, fitted }) w.Do(new Run(Drill, true)).Do(HewTesting.Hit(w, 1, Drill)).Do(HewTesting.Hit(w, 1, Drill)).Run();
        Assert.Equal((2, 4), (plain.M.Debris.Pieces[0].Damage, fitted.M.Debris.Pieces[0].Damage));
        Assert.Empty(fitted.Refusals);
    }

    // One fold for the sim and the card (ToolStats).

    /// <summary>A module that is not in content, for the drill and the axe: half as much again of damage, a quarter less draw.</summary>
    private static readonly Spec.ModuleSpec Fake = new("fakeModule", "arc", [new([Drill, Spec.Weapons.Axe.Item], [new Spec.Trade(Spec.Stat.Draw, -250), new Spec.Trade(Spec.Stat.Damage, 500)])]);

    [Fact]
    public void ToolStatsFoldTheModulesOfAStackAndAreWhatTheSimHitsAndDrawsBy()
    {
        Assert.Equal(new ToolStats(1, 2, 3), ToolStats.Of(new Stack(Drill, 1)));
        Assert.Equal(new ToolStats(2, 2, 6), ToolStats.Of(Fitted));
        Assert.Equal(new ToolStats(2, 10, 0), ToolStats.Of(new Stack(Spec.Weapons.Axe.Item, 1)));
        // Two modules summed first, then applied once: damage 1 × 2500 / 1000 down to 2, draw 3 × 1750 / 1000 up to 6.
        Assert.Equal(new ToolStats(2, 2, 6), ToolStats.Of(Drill, [Spec.Modules.Arc, Fake]));
        Assert.Equal(new ToolStats(1, 2, 3), ToolStats.Of(Drill, [Fake]));
        // On blocks, the same gain: 4 × 2500 / 1000 to 10.
        Assert.Equal(10, ToolStats.BlockDamageOf(Drill, [Spec.Modules.Arc, Fake]));
        Assert.Equal(ToolStats.Of(Drill, [Spec.Modules.Arc, Fake]), ToolStats.Of(Drill, [Fake, Spec.Modules.Arc]));
        Assert.Equal([Spec.Modules.Arc], ToolStats.ModulesOf(Fitted));
        Assert.Same(ToolStats.ModulesOf(Fitted), ToolStats.ModulesOf(Fitted with { }));
        Assert.Empty(ToolStats.ModulesOf(new Stack(Drill, 1)));
        // The sim's damage is the fold's for the stack its peer works with.
        var w = Ground().Put(0, Fitted).GiveItems(1, (Drill, 1));
        Assert.Equal((ToolStats.Of(Fitted).Damage, ToolStats.Of(new Stack(Drill, 1)).Damage), (w.M.Items.Damage(0, Drill), w.M.Items.Damage(1, Drill)));
    }

    /// <summary>Soil, stone and alloy side by side in the ground's top row: cells 9, 10 and 11.</summary>
    private static Scenario Materials() => new(Layout.Parse("....\n....\n####\n####").WithMaterials("....\n....\n.sta\n.sta").OnPlanet());

    /// <summary>The digs <paramref name="stack"/> takes to break <paramref name="cell"/>, a hit every pace.</summary>
    private static int HitsToBreak(Stack stack, int cell)
    {
        var w = Materials().Put(0, stack).Do(new SetCreative(true)).Do(new Run(Drill, true));
        int hits = 0;
        while (w.M.Grid.WallAt(cell) != null && hits < 100)
        {
            w.Do(HewTesting.Dig(w, cell, Drill)).Run(Spec.Weapons.Drill.Pace);
            hits++;
        }
        Assert.Empty(w.Refusals);
        return hits;
    }

    [Fact]
    public void TheFittedDrillBreaksSoilStoneAndAlloyInHalfTheHitsAndABatteryLastsHalfAsLong()
    {
        // 4, 12 and 60 strong: a hit every 0.1 s, 4 and 8 a hit on blocks: soil at a hit either way, stone 0.3 s and 0.2 s, alloy 1.5 s and 0.8 s.
        Assert.Equal((1, 3, 15), (HitsToBreak(new Stack(Drill, 1), 9), HitsToBreak(new Stack(Drill, 1), 10), HitsToBreak(new Stack(Drill, 1), 11)));
        Assert.Equal((1, 2, 8), (HitsToBreak(Fitted, 9), HitsToBreak(Fitted, 10), HitsToBreak(Fitted, 11)));
        // A battery: 1800 power-ticks, 600 ticks (30 s) at the plain drill's 3, 300 (15 s) at the fitted 6.
        Assert.Equal((600, 300), (Full / S / ToolStats.Of(new Stack(Drill, 1)).Draw, Full / S / ToolStats.Of(Fitted).Draw));
    }

    // The module goes with its stack.

    [Fact]
    public void AlikeAndTheSortsOrderTellAFittedDrillFromAPlainOne()
    {
        var plain = new Stack(Drill, 1);
        Assert.False(Stacks.Alike(plain, Fitted));
        Assert.True(Stacks.Alike(Fitted, Fitted with { }));
        Assert.False(Stacks.Alike(Fitted, Fitted with { Module = "other" }));
        // Sorted: the plain one first, then by module, ordinal; every stack kept.
        var slots = new Stack?[] { Fitted, null, plain, Fitted with { Module = "a" }, new("battery", 1) };
        var sorted = Stacks.Sorted(slots, 0, slots.Length);
        Assert.Equal(new Stack?[] { plain, Fitted with { Module = "a" }, Fitted, new("battery", 1), null }, sorted.ToList());
    }

    [Fact]
    public void MovesIntoACrateAndBackAndSwapsWithAPlainDrillKeepingItsModule()
    {
        var w = new Scenario(Layout.Parse(["#######", "#S..KK#", "#.....#", "#######"]), doubled: true).Put(0, Fitted).GiveItems(0, (Drill, 1));
        int crate = w.M.Grid.MachinesOf("crate").Single().Id;
        Assert.Equal(new Stack(Drill, 1), At(w, 1));
        // Whole onto the plain drill: they swap, as two batteries of different charge do.
        w.Do(new Move(new Slot(0), new Target(1), 1)).Run();
        Assert.Equal((new Stack(Drill, 1), Fitted), (At(w, 0), At(w, 1)));
        w.Do(new Move(new Slot(1), new Target(2, crate), 1)).Run();
        Assert.Equal(Fitted, w.M.Items.Crate(crate)[2]);
        // Onto a side as a whole, into the first empty slot, never onto the plain one.
        w.Do(new Move(new Slot(0), new Target(Crate: crate), 1)).Run();
        Assert.Equal(new Stack(Drill, 1), w.M.Items.Crate(crate)[0]);
        w.Do(new Move(new Slot(2, crate), new Target(Crate: null), 1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Contains(Fitted, w.M.Items.Inventory(0));
        w.Do(new Sort(crate)).Do(new Sort()).Run();
        Assert.Equal(1, w.M.Items.Inventory(0).Count(s => s == Fitted));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void ThrownPickedUpAndSavedItKeepsItsModuleOnTheFloorAndInTheSlot()
    {
        var w = new Scenario(Layout.Parse(["#######", "#S....#", "#######"]), doubled: true).Put(0, Fitted).Put(1, new Stack(Drill, 1));
        w.Do(new Drop(new Slot(0), 1, w.At(3, 1), 1)).Do(new Drop(new Slot(1), 1, w.At(3, 1), 1)).Run(60);
        Assert.Equal([Arc, null], w.M.Items.Floor.Select(s => s.Module));
        w.Reload();
        Assert.Equal([Arc, null], w.M.Items.Floor.Select(s => s.Module));
        int id = w.M.Items.Floor[0].Id;
        w.Do(new PickUp(id) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Fitted, At(w, 0, peer: 1));
        Assert.Contains("\"module\":\"arcModule\"", w.World.Save());
    }

    [Fact]
    public void ASaveWithNoModuleOnAnyStackWritesNoneAndOneWithAFittedDrillLoadsItBack()
    {
        var w = new Scenario(Layout.Parse(["#######", "#S....#", "#######"]), doubled: true).GiveItems(0, (Drill, 1), ("iron", 3));
        Assert.DoesNotContain("module", w.World.Save());
        w.Put(5, Fitted).Reload();
        Assert.Equal(Fitted, At(w, 5));
    }

    [Fact]
    public void ItsHeroKeepsItWhenItDiesAsGear()
    {
        var w = new Scenario(Layout.Parse(["##########", "#S.......#", "#........#", "##########"]), doubled: true).Put(0, Fitted).GiveItems(0, ("scrap", 5));
        w.Do(new Enter(w.At(5, 1))).Run();
        for (int i = 0; i < 2 * Spec.Crew.Suit * Spec.Crew.BreathEvery && w.M.Crew.HeroOf(0)!.Out == 0; i++) w.Run();
        Assert.Equal([("scrap", 5)], w.M.Items.Floor.Select(s => (s.Item, s.Count)));
        Assert.Equal(Fitted, At(w, 0));
    }

    [Fact]
    public void ItGoesWithItsHeroToAnotherSurfaceAndRunsOnThere()
    {
        var w = new Scenario(Layout.Parse(["............", ".##########.", ".#..D..S..#.", ".##########.", "............"]), doubled: true);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Spec.Surfaces.TestStrip.Id)).Run();
        w.Put(0, Fitted).Put(Belt, Battery(Full)).Do(new Run(Drill, true)).Run();
        w.Do(new Transfer(1)).Run();
        var there = w.On(1);
        Assert.Equal(Fitted, there.Items.Inventory(0)[0]);
        Assert.Equal(Drill, there.Items.Running(0));
        // It draws its fitted draw there: two ticks of six, one aboard and one on the strip.
        Assert.Equal(Full - 2 * 6 * S, there.Items.Inventory(0)[Belt]!.Charge);
    }

    // Conserved under the fuzz.

    /// <summary>A sealed room with nothing open to space, so no stack is ever thrown away: the smelter, the Former, the furnace and a crate.</summary>
    private static readonly string[] Sealed = ["##########", "#EE.MM.KK#", "#EE.MM.FF#", "#......FF#", "##########"];

    /// <summary>Every stack there is with what it holds: carried by peers 0 to 4, held by machines, and on the floor.</summary>
    private static IEnumerable<Stack> Everywhere(Scenario w) =>
        Enumerable.Range(0, 5).SelectMany(p => w.M.Items.Inventory(p))
            .Concat(w.M.Grid.Machines.SelectMany(m => w.M.Items.Crate(m.Id)))
            .OfType<Stack>()
            .Concat(w.M.Items.Floor.Select(s => new Stack(s.Item, s.Count, s.Charge, s.Module)));

    /// <summary>
    /// Every arc module is in exactly one place, whatever the items fuzz draws by three peers carrying modules, fitted drills
    /// and plain ones: as an item, fitted into a machine, or on a drill's stack, a stack of one of an item it fits; never made
    /// or lost, never on two stacks.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property EachArcModuleIsInExactlyOnePlace(PositiveInt seed)
    {
        var w = new Scenario(Layout.Parse(Sealed), doubled: true)
            .GiveItems(0, (Arc, 2), (Drill, 1)).Put(3, Fitted)
            .GiveItems(1, ("iron", 9)).Put(4, Fitted).Put(Belt, Battery(Full), peer: 1)
            .GiveItems(2, (Arc, 1), (Drill, 2)).Put(6, Fitted, peer: 2);
        static (int Items, int Fitted, int OnStacks) Count(Scenario w) =>
        (
            Everywhere(w).Where(s => s.Item == Arc).Sum(s => s.Count),
            w.M.Grid.Machines.Sum(m => w.M.Items.FittedTo(m.Id).Count(f => f == Arc)),
            Everywhere(w).Where(s => s.Module == Arc).Sum(s => s.Count)
        );
        var before = Count(w);
        var rng = new Rng(new RngState { S = (uint)seed.Get });
        for (int i = 0; i < 300; i++)
        {
            if (w.M.Items.Fuzz(rng, i % 3) is { } c) w.Do(c).Run();
            Assert.All(Everywhere(w).Where(s => s.Module != null), s => Assert.True(s.Count == 1 && Spec.Modules.Fits(s.Module, s.Item), $"{s}"));
        }
        var after = Count(w);
        return (after.Items + after.Fitted + after.OnStacks == before.Items + before.Fitted + before.OnStacks && after.OnStacks == before.OnStacks)
            .Label($"{after} arc modules of {before}");
    }
}
