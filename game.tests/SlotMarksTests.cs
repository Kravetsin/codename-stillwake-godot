using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The mark in an inventory slot's corner while a fed station or a fire is open: what it smelts, burns or grows, before the "?".</summary>
public class SlotMarksTests
{
    private static Reading Open(string? kind, params string[] analysed) => new([], null, false, kind, null, analysed);

    [Fact]
    public void MarkWhatTheOpenFurnaceSmeltsAndBurnsAFlameBeforeTheQuestionMark()
    {
        var furnace = Open("furnace");
        // Rock waits for an analyzer, yet the furnace's flame goes first: the friend saw it smelts.
        Assert.True(furnace.Unknown("rock"));
        Assert.Equal(SlotMark.Smelts, furnace.MarkOf("rock"));
        Assert.Equal(SlotMark.Smelts, furnace.MarkOf("hullPlate"));
        Assert.Equal(SlotMark.Smelts, furnace.MarkOf("scrapChunk"));
        Assert.Equal(SlotMark.Burns, furnace.MarkOf("planks"));
        // Too hot for it: no flame, only what it shows anyway.
        Assert.Equal(SlotMark.Unknown, furnace.MarkOf("silicon"));
        Assert.Equal(SlotMark.None, Open("furnace", "silicon").MarkOf("silicon"));
        Assert.Equal(SlotMark.None, furnace.MarkOf("fireAxe"));
    }

    [Fact]
    public void MarkWhatTheSmelterSmeltsAndTheGrowCapsuleGrowsAndTheBurnersFuel()
    {
        Assert.Equal(SlotMark.Smelts, Open("smelter").MarkOf("silicon"));
        // The smelter runs on power: planks are no fuel there.
        Assert.Equal(SlotMark.Unknown, Open("smelter").MarkOf("planks"));
        Assert.Equal(SlotMark.Grows, Open("growCapsule").MarkOf("seed"));
        Assert.Equal(SlotMark.Unknown, Open("growCapsule").MarkOf("rock"));
        Assert.Equal(SlotMark.Burns, Open("burnerGenerator").MarkOf("planks"));
    }

    [Fact]
    public void MarkOnlyTheQuestionMarkWithNoStationOpen()
    {
        Assert.Equal(SlotMark.Unknown, Open(null).MarkOf("rock"));
        Assert.Equal(SlotMark.None, Open(null, "rock").MarkOf("rock"));
        Assert.Equal(SlotMark.Unknown, Open("crate").MarkOf("rock"));
        Assert.Equal(SlotMark.None, Open("crate").HereOf("planks"));
    }

    [Fact]
    public void SayInTheTipWhatTheOpenStationDoesWithIt()
    {
        var stack = new Stack("rock", 3);
        Assert.Equal(SlotMark.Smelts, Open("furnace").HereTipOf(stack).Card!.Here);
        Assert.Equal(SlotMark.Burns, Open("furnace").HereTipOf(new Stack("planks", 1)).Card!.Here);
        Assert.Equal(SlotMark.None, Open(null).HereTipOf(stack).Card!.Here);
        Assert.NotEqual(Open("furnace").HereTipOf(stack), Open(null).HereTipOf(stack));
    }

    [Fact]
    public void MarkAModuleTheOpenMachineTakesWithItsTechsIconAndSayNoWordsOfWhereItGoes()
    {
        var arc = Spec.Modules.Arc.Item;
        foreach (var host in Spec.Modules.Arc.Hosts) Assert.Equal(SlotMark.Fits, Open(host, arc).MarkOf(arc));
        // Where it does not fit, the furnace, or with no machine open: only what it shows anyway.
        Assert.Equal(SlotMark.None, Open("furnace", arc).MarkOf(arc));
        Assert.Equal(SlotMark.None, Open(null, arc).MarkOf(arc));
        Assert.Equal(SlotMark.Fits, Open("smelter").HereTipOf(new Stack(arc, 1)).Card!.Here);
    }

    [Fact]
    public void EveryModulesTechHasAGlyphOfItsOwnInsideTheSharedFrame()
    {
        foreach (var module in Spec.Modules.All)
        {
            var look = TechLooks.Of(module.Tech);
            Assert.NotEmpty(look.Glyph);
            Assert.All(look.Glyph.SelectMany(s => s.Points), p => Assert.True(p.U is > 0.1f and < 0.9f && p.V is > 0.03f and < 0.97f, $"{module.Tech}: {p}"));
        }
    }
}
