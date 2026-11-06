using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>What went into a hero's inventory, noted for its client to show (2026-11-06-head-tally): never saved, read by nothing in the sim.</summary>
public class GainsTests
{
    /// <summary>Open ground over soil, as <see cref="DigTests"/> has it.</summary>
    private const string Map = """
        ........
        ........
        ########
        ########
        """;

    private const string Ground = """
        ........
        ........
        .ssssssb
        .ssssssb
        """;

    private static readonly string Axe = Weapons.Axe.Item;

    private static Scenario World() => new(Layout.Parse(Map).WithMaterials(Ground) with { Kind = Content.Surfaces.TestStrip.Id });
    private static int At(int x, int y) => y * 8 + x;
    private static (int Peer, string Item, int Count, int Cell)[] Noted(Scenario w) =>
        w.M.Items.Gains.Select(g => (g.Peer, g.Item, g.Count, g.Cell)).ToArray();

    [Fact]
    public void ADugCellIsNotedForItsDiggerAtItsCellAnotherPeersForThatPeer()
    {
        var w = World();
        w.Do(HewTesting.Dig(w, At(2, 2), Axe)).Run();
        w.Do(HewTesting.Dig(w, At(4, 2), Axe) with { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([(0, "soil", 1, At(2, 2)), (1, "soil", 1, At(4, 2))], Noted(w));
        Assert.Equal([1L, 2L], w.M.Items.Gains.Select(g => g.Seq));
    }

    [Fact]
    public void NotesOnlyWhatFitsNothingWhenNothingDoesAndNothingItsCallerShowsItself()
    {
        var w = World().GiveItems(0, ("canister", (Inventory.Slots - 1) * StackOf("canister")));
        w.M.Items.GiveOrDrop(0, [("scrap", StackOf("scrap") + 7)], At(2, 0));
        Assert.Equal([(0, "scrap", StackOf("scrap"), At(2, 0))], Noted(w));
        // The bag full now: what falls is a floor stack, a gain only once picked up.
        w.M.Items.GiveOrDrop(0, [("iron", 3)], At(3, 0));
        // A debris grab's loot is shown from where the piece was (CaughtFrom), not here.
        w.M.Items.GiveOrDrop(1, [("iron", 3)], At(3, 0), noted: false);
        Assert.Single(w.M.Items.Gains);
    }

    [Fact]
    public void KeepsTheLatestGainsTheirSequenceClimbingAndALoadEmptiesThem()
    {
        var w = World();
        for (int i = 0; i < ItemsModule.GainsKept + 3; i++) w.M.Items.GiveOrDrop(0, [("scrap", 1)], At(1, 0));
        Assert.Equal(ItemsModule.GainsKept, w.M.Items.Gains.Count);
        Assert.Equal(4L, w.M.Items.Gains[0].Seq);
        Assert.Equal(ItemsModule.GainsKept + 3L, w.M.Items.Gains[^1].Seq);
        w.Reload();
        Assert.Empty(w.M.Items.Gains);
    }

    private static int StackOf(string item) => Content.Items.Get(item).Stack;
}
