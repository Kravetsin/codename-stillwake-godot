using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Stashing the bag into the crates in reach that hold its items (2026-11-06-quick-stash).</summary>
public class StashTests
{
    private static readonly int Bag = Inventory.Hotbar;
    private static readonly int CrateSlots = Machines.Get("crate").Slots!.Value;
    private static int StackOf(string item) => Content.Items.Get(item).Stack;

    /// <summary>Two crates a cell apart from a gap of two, a wall, and room to stand up to 16 cells off.</summary>
    private static Scenario Store() => new(string.Join("\n",
        "KKKK..KKKK..#........",
        "KKKK..KKKK..#........"), cells: true);

    private static int CrateAt(Scenario w, int x) => w.M.Grid.MachineAt(w.At(x, 0))!.Id;

    private static Stash StashAt(Scenario w, int x, int y = 0, params string[] keep) => new(w.At(x, y), keep);

    private static void Same(IEnumerable<Stack?> want, IEnumerable<Stack?> got) => Assert.Equal(want.ToList(), got.ToList());

    private static Stack?[] Padded(int size, params Stack?[] first) => [.. first, .. new Stack?[size - first.Length]];

    [Fact]
    public void TopsUpTheCratesStackOfTheItemThenFillsItsEmptySlot()
    {
        var w = Store();
        int crate = CrateAt(w, 0);
        int scrap = StackOf("scrap");
        w.Put(0, new Stack("scrap", scrap - 5), crate: crate).Put(1, new Stack("wire", 3), crate: crate).Put(Bag + 3, new Stack("scrap", 20));
        w.Do(StashAt(w, 4)).Run();
        Assert.Empty(w.Refusals);
        Same(Padded(CrateSlots, new("scrap", scrap), new("wire", 3), new("scrap", 15)), w.M.Items.Crate(crate));
        Assert.Null(w.M.Items.Inventory(0)[Bag + 3]);
    }

    [Fact]
    public void LeavesWhatNoCrateHoldsInTheBag()
    {
        var w = Store();
        int crate = CrateAt(w, 0);
        w.Put(0, new Stack("scrap", 1), crate: crate).Put(Bag, new Stack("wire", 4));
        w.Do(StashAt(w, 4)).Run();
        Assert.Equal(new Stack("wire", 4), w.M.Items.Inventory(0)[Bag]);
        Same(Padded(CrateSlots, new Stack("scrap", 1)), w.M.Items.Crate(crate));
    }

    [Fact]
    public void FillsTheNearerCrateFirst()
    {
        var w = Store();
        int left = CrateAt(w, 0), right = CrateAt(w, 6);
        int scrap = StackOf("scrap");
        // Every slot of the right crate full but one stack of scrap short by 5; the left one has room to spare.
        for (int i = 0; i < CrateSlots; i++) w.Put(i, i == 0 ? new Stack("scrap", scrap - 5) : new Stack("wire", StackOf("wire")), crate: right);
        w.Put(0, new Stack("scrap", 1), crate: left).Put(Bag, new Stack("scrap", 12));
        w.Do(StashAt(w, 5)).Run();
        Assert.Equal(new Stack("scrap", scrap), w.M.Items.Crate(right)[0]);
        Assert.Equal(new Stack("scrap", 8), w.M.Items.Crate(left)[0]);
    }

    [Fact]
    public void ACrateAsNearAsAnotherGivesWayToTheLowerId()
    {
        var w = new Scenario(string.Join("\n", "KKKK.KKKK", "KKKK.KKKK"), cells: true);
        int a = CrateAt(w, 0), b = CrateAt(w, 5);
        foreach (int crate in new[] { a, b }) w.Put(0, new Stack("scrap", 1), crate: crate);
        w.Put(Bag, new Stack("scrap", 6));
        w.Do(StashAt(w, 4)).Run();
        Assert.Equal(new Stack("scrap", 7), w.M.Items.Crate(Math.Min(a, b))[0]);
        Assert.Equal(new Stack("scrap", 1), w.M.Items.Crate(Math.Max(a, b))[0]);
    }

    [Theory]
    [InlineData(15, 0, true)]
    [InlineData(16, 0, false)]
    [InlineData(9, 1, true)]
    public void ReachesTwelveCellsFromTheCratesNearestCellThroughAWall(int x, int y, bool reached)
    {
        // The crate's nearest cell is at x 3: 12 cells off is x 15, past the wall.
        var w = new Scenario(string.Join("\n",
            "KKKK.......#..........",
            "KKKK.......#.........."), cells: true);
        int crate = CrateAt(w, 0);
        w.Put(0, new Stack("scrap", 1), crate: crate).Put(Bag, new Stack("scrap", 2));
        w.Do(StashAt(w, x, y)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(reached ? null : new Stack("scrap", 2), w.M.Items.Inventory(0)[Bag]);
    }

    [Fact]
    public void NeverTouchesTheHotbarWhatIsWornOrTheBelt()
    {
        var w = Store();
        int crate = CrateAt(w, 0);
        w.Put(0, new Stack("scrap", 1), crate: crate).Put(0, new Stack("scrap", 3)).Put(Bag - 1, new Stack("scrap", 2))
            .Wear(0, "jetpack").Put(0, new Stack("jetpack", 1), crate: CrateAt(w, 6)).Put(Inventory.Slots + 2, new Stack("battery", 1, 7000))
            .Put(1, new Stack("battery", 1), crate: crate);
        var before = w.M.Items.Inventory(0).ToArray();
        w.Do(StashAt(w, 4)).Run();
        Same(before, w.M.Items.Inventory(0));
    }

    [Fact]
    public void KeepsAKeptItemAndStashesTheRest()
    {
        var w = Store();
        int crate = CrateAt(w, 0);
        w.Put(0, new Stack("scrap", 1), crate: crate).Put(1, new Stack("wire", 1), crate: crate)
            .Put(Bag, new Stack("scrap", 4)).Put(Bag + 1, new Stack("wire", 5));
        w.Do(StashAt(w, 4, 0, "wire")).Run();
        Assert.Null(w.M.Items.Inventory(0)[Bag]);
        Assert.Equal(new Stack("wire", 5), w.M.Items.Inventory(0)[Bag + 1]);
    }

    [Fact]
    public void WhatFitsNowhereStaysInTheBag()
    {
        var w = Store();
        int crate = CrateAt(w, 0);
        int scrap = StackOf("scrap");
        for (int i = 0; i < CrateSlots; i++) w.Put(i, new Stack("scrap", i == 0 ? scrap - 3 : scrap), crate: crate);
        w.Put(Bag, new Stack("scrap", 10));
        w.Do(StashAt(w, 4)).Run();
        Assert.Equal(new Stack("scrap", 7), w.M.Items.Inventory(0)[Bag]);
        Assert.Equal(new Stack("scrap", scrap), w.M.Items.Crate(crate)[0]);
    }

    [Fact]
    public void LeavesAFurnaceHoldingTheItemAlone()
    {
        var w = new Scenario(Layout.Parse(["#FF..#", "#FF..#"]), doubled: true);
        int furnace = w.M.Grid.MachineAt(w.At(1, 0))!.Id;
        w.Put(0, new Stack("scrap", 1), crate: furnace).Put(Bag, new Stack("scrap", 5));
        var before = w.M.Items.Crate(furnace).ToArray();
        w.Do(new Stash(w.At(3, 0), [])).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("scrap", 5), w.M.Items.Inventory(0)[Bag]);
        Same(before, w.M.Items.Crate(furnace));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void NoCrateInReachOrACellOffTheGridMovesNothingAndIsNotRefused(int cell)
    {
        var w = new Scenario(Layout.Empty).Put(Bag, new Stack("scrap", 5));
        var before = w.M.Items.Inventory(0).ToArray();
        w.Do(new Stash(cell, [])).Do(new Stash(0, [])).Run();
        Assert.Empty(w.Refusals);
        Same(before, w.M.Items.Inventory(0));
    }

    [Fact]
    public void TwoPlayersStashingInOneTickApplyInTurnAndTwoWorldsAgree()
    {
        int scrap = StackOf("scrap");
        Scenario Played()
        {
            var w = Store();
            int crate = CrateAt(w, 0);
            for (int i = 0; i < CrateSlots; i++) w.Put(i, new Stack("scrap", i == 0 ? scrap - 8 : scrap), crate: crate);
            w.Put(Bag, new Stack("scrap", 5)).Put(Bag, new Stack("scrap", 5), peer: 1);
            return w.Do(StashAt(w, 4) with { From = 0 }).Do(StashAt(w, 4) with { From = 1 }).Run();
        }
        var a = Played();
        Assert.Empty(a.Refusals);
        // The first takes all its 5; the second finds room for 3 only.
        Assert.Null(a.M.Items.Inventory(0)[Bag]);
        Assert.Equal(new Stack("scrap", 2), a.M.Items.Inventory(1)[Bag]);
        Assert.Equal(a.World.Save(), Played().World.Save());
    }

    [Fact]
    public void AClientShowsItsOwnStashAtOnceAndPlansNothingMoreAfterIt()
    {
        var w = Store();
        int crate = CrateAt(w, 0);
        w.Put(0, new Stack("scrap", 1), crate: crate).Put(1, new Stack("wire", 1), crate: CrateAt(w, 6))
            .Put(Bag, new Stack("scrap", 4)).Put(Bag + 2, new Stack("wire", 6)).Put(Bag + 5, new Stack("gear", 1));
        var stash = StashAt(w, 5);
        var parts = w.M.Items.StashPlan(0, stash, []);
        Assert.Equal([new Stashed(Bag, crate, "scrap", 4), new Stashed(Bag + 2, CrateAt(w, 6), "wire", 6)], parts.OrderBy(p => p.Slot));
        var shown = w.M.Items.AfterMoves(0, [stash]);
        Assert.Empty(w.M.Items.StashPlan(0, stash, [stash]));
        w.Do(stash).Run();
        Same(w.M.Items.Inventory(0), shown.Inventory);
        Same(w.M.Items.Crate(crate), shown.Crates.First(c => c.Crate == crate).Slots);
    }

    /// <summary>Over any bag and any two crates' contents of a few items, wherever the hero stands: no item is made or lost.</summary>
    [Property(MaxTest = 60)]
    public Property KeepsEveryItemsTotalOverTheInventoryAndTheCrates()
    {
        string[] items = ["scrap", "wire", "gear", "battery"];
        var stack = from i in Gen.Choose(0, items.Length - 1)
                    from n in Gen.Choose(1, 40)
                    select new Stack(items[i], Math.Min(n, StackOf(items[i])));
        var slots = Gen.OneOf(Gen.Constant<Stack?>(null), stack.Select(s => (Stack?)s));
        var case_ = from bag in Gen.ArrayOf(slots, Inventory.Slots - Bag)
                    from left in Gen.ArrayOf(slots, CrateSlots)
                    from right in Gen.ArrayOf(slots, CrateSlots)
                    from x in Gen.Choose(-2, 22)
                    from keep in Gen.SubListOf(items)
                    select (bag, left, right, x, keep);
        return Prop.ForAll(case_.ToArbitrary(), c =>
        {
            var w = Store();
            int left = CrateAt(w, 0), right = CrateAt(w, 6);
            for (int i = 0; i < c.bag.Length; i++) if (c.bag[i] is { } s) w.Put(Bag + i, s);
            for (int i = 0; i < CrateSlots; i++)
            {
                if (c.left[i] is { } l) w.Put(i, l, crate: left);
                if (c.right[i] is { } r) w.Put(i, r, crate: right);
            }
            List<(string, int)> Totals() =>
                [.. w.M.Items.Inventory(0).Concat(w.M.Items.Crate(left)).Concat(w.M.Items.Crate(right)).OfType<Stack>()
                    .GroupBy(s => s.Item).Select(g => (g.Key, g.Sum(s => s.Count))).OrderBy(t => t.Key, StringComparer.Ordinal)];
            var before = Totals();
            var hotbar = w.M.Items.Inventory(0).Take(Bag).ToArray();
            var parts = w.M.Items.StashPlan(0, new Stash(c.x < 0 ? -1 : w.At(c.x, 0), [.. c.keep]), []);
            w.Do(new Stash(c.x < 0 ? -1 : w.At(c.x, 0), [.. c.keep])).Run();
            return (before.SequenceEqual(Totals()) && hotbar.SequenceEqual(w.M.Items.Inventory(0).Take(Bag))
                && parts.All(p => !c.keep.Contains(p.Item))).ToProperty();
        });
    }
}
