using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The dev panel's item picker: <see cref="Spawn"/> makes items out of nothing, in creative mode only.</summary>
public class SpawnTests
{
    private static int Count(Scenario w, string item, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static int StackOf(string item) => Content.Items.Get(item).Stack;

    [Fact]
    public void ASpawnIsRefusedOutsideCreativeModeAndGivesNothing()
    {
        var w = new Scenario(Layout.Empty).Do(new Spawn("iron", 10)).Run();
        Assert.Equal("items.notCreative", Assert.Single(w.Refusals).Reason);
        Assert.Equal(0, Count(w, "iron"));
        Assert.Empty(w.Stored().Inventories);
    }

    [Fact]
    public void ASpawnGivesTheSenderAllItAsksSpreadOverAsManyStacksAsItTakes()
    {
        int asked = StackOf("scrap") + 7;
        var w = new Scenario(Layout.Empty)
            .Do(new SetCreative(true))
            .Do(new Spawn("scrap", asked) { From = 2 })
            .Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(asked, Count(w, "scrap", 2));
        Assert.Equal(0, Count(w, "scrap", 0));
        Assert.Equal(new Stack?[] { new("scrap", StackOf("scrap")), new("scrap", 7) }, w.M.Items.Inventory(2).Take(2));
    }

    [Fact]
    public void ASpawnGivesWhatFitsAndIsRefusedWhenNothingDoes()
    {
        int canisters = StackOf("canister") * (Inventory.Slots - 1);
        var w = new Scenario(Layout.Empty).GiveItems(0, ("canister", canisters))
            .Do(new SetCreative(true))
            .Do(new Spawn("iron", StackOf("iron") * 3))
            .Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(StackOf("iron"), Count(w, "iron"));
        w.Do(new Spawn("scrap", 1)).Run();
        Assert.Equal("items.full", Assert.Single(w.Refusals).Reason);
        Assert.Equal(0, Count(w, "scrap"));
    }

    [Fact]
    public void ASpawnedBatteryComesFullEachInASlotOfItsOwn()
    {
        var w = new Scenario(Layout.Empty).Do(new SetCreative(true)).Do(new Spawn("battery", 2)).Run();
        Assert.Empty(w.Refusals);
        var full = new Stack("battery", 1, Charge: Content.Items.Get("battery").Capacity!.Value);
        Assert.Equal(new Stack?[] { full, full }, w.M.Items.Inventory(0).Take(2));
    }

    /// <summary>The picker never sends a count below 1: one is our bug, not a refusal.</summary>
    [Fact]
    public void ASpawnOfNoneIsABugNotARefusal()
    {
        var w = new Scenario(Layout.Empty).Do(new SetCreative(true)).Run();
        Assert.Throws<InvalidOperationException>(() => w.Do(new Spawn("iron", 0)).Run());
    }

    [Fact]
    public void EveryItemCanBeSpawnedAStackAtATime()
    {
        var w = new Scenario(Layout.Empty).Do(new SetCreative(true)).Run();
        foreach (var item in Content.Items.All.Take(Inventory.Slots))
            w.Do(new Spawn(item.Id, item.Stack));
        w.Run();
        Assert.Empty(w.Refusals);
        foreach (var item in Content.Items.All.Take(Inventory.Slots)) Assert.Equal(item.Stack, Count(w, item.Id));
    }
}
