using System.Linq;
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
    public void AStationsEmptySlotShowsTheGhostOfWhatGoesInIt()
    {
        int fuel = Spec.Machines.Get("furnace").FuelSlot!.Value;
        Assert.Equal(SlotMark.Smelts, Reading.GhostOf("furnace", 0));
        Assert.Equal(SlotMark.Burns, Reading.GhostOf("furnace", fuel));
        // Its outputs take nothing in: no ghost.
        for (int slot = fuel + 1; slot < Spec.Machines.Get("furnace").Slots; slot++) Assert.Equal(SlotMark.None, Reading.GhostOf("furnace", slot));
        Assert.Equal(SlotMark.Smelts, Reading.GhostOf("smelter", 0));
        Assert.Equal(SlotMark.None, Reading.GhostOf("smelter", Spec.Machines.Get("smelter").Inputs!.Value));
        Assert.Equal(SlotMark.Grows, Reading.GhostOf("growCapsule", 0));
        Assert.Equal(SlotMark.Burns, Reading.GhostOf("burnerGenerator", 0));
        Assert.Equal(SlotMark.Unknown, Reading.GhostOf("analyzer", 0));
        Assert.Equal(SlotMark.Charges, Reading.GhostOf("charger", 0));
        for (int slot = 0; slot < Spec.Machines.Get(Spec.Logistics.Bay).Slots; slot++) Assert.Equal(SlotMark.Docks, Reading.GhostOf(Spec.Logistics.Bay, slot));
        // A crate takes anything, and a queued station's inputs any cost: no ghost.
        Assert.Equal(SlotMark.None, Reading.GhostOf("crate", 0));
        Assert.All(Spec.Stations.All.Where(s => !s.Fed), s => Assert.Equal(SlotMark.None, Reading.GhostOf(s.Machine, 0)));
    }

    [Fact]
    public void ACarriedItemLightsTheGhostOfTheSlotItGoesIn()
    {
        var furnace = Open("furnace");
        Assert.True(furnace.Lights(SlotMark.Smelts, "rock"));
        Assert.False(furnace.Lights(SlotMark.Burns, "rock"));
        Assert.True(furnace.Lights(SlotMark.Burns, "planks"));
        // Too hot for it: no glow, as no mark.
        Assert.False(furnace.Lights(SlotMark.Smelts, "silicon"));
        Assert.True(Open("smelter").Lights(SlotMark.Smelts, "silicon"));
        Assert.True(Open("growCapsule").Lights(SlotMark.Grows, "seed"));
        Assert.False(Open("growCapsule").Lights(SlotMark.Grows, "rock"));
        Assert.True(Open("analyzer").Lights(SlotMark.Unknown, "rock"));
        Assert.False(Open("analyzer", "rock").Lights(SlotMark.Unknown, "rock"));
        Assert.True(Open("charger").Lights(SlotMark.Charges, "battery"));
        Assert.False(Open("charger").Lights(SlotMark.Charges, "rock"));
        Assert.True(Open(Spec.Logistics.Bay).Lights(SlotMark.Docks, Spec.Logistics.Drone));
        Assert.False(Open(Spec.Logistics.Bay).Lights(SlotMark.Docks, "battery"));
        Assert.False(furnace.Lights(SlotMark.None, "rock"));
    }

    [Fact]
    public void WhatWearsAStationsMarkInTheBagLightsItsGhostToo()
    {
        foreach (var machine in Spec.Machines.All)
            for (int slot = 0; slot < (machine.Slots ?? 0); slot++)
            {
                var ghost = Reading.GhostOf(machine.Kind, slot);
                if (ghost == SlotMark.None) continue;
                var open = Open(machine.Kind);
                foreach (var item in Spec.Items.All)
                    if (open.HereOf(item.Id) == ghost) Assert.True(open.Lights(ghost, item.Id), $"{item.Id} at {machine.Kind} {slot}");
            }
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
