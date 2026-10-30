using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>Stacks, slots, the floor, item groups and what holds charge.</summary>
public partial class ContentTests
{
    // Items

    [Fact]
    public void ItemsStackWholeNumbersAndEveryHullMaterialIsOne()
    {
        foreach (var item in Content.Items.All) Assert.True(item.Stack > 0, item.Id);
        // Natural ground (0077) is dug into what it costs: soil, and stone into rock.
        foreach (var material in Materials.All.Where(m => !m.Natural)) Assert.Contains(material.Id, Content.Items.All.Select(i => i.Id));
    }

    [Fact]
    public void ItemsGoIntoInventoriesAndMachinesOfWholeSlotsAMachineTakingThemIntoItsFirstFewOrNone()
    {
        Assert.True(Inventory.Slots > 0);
        foreach (var m in Machines.All)
        {
            if (m.Slots != null) Assert.True(m.Slots > 0, m.Kind);
            if (m.Inputs == null) continue;
            Assert.NotNull(m.Slots);
            Assert.True(m.Inputs >= 0 && m.Inputs < m.Slots, m.Kind);
        }
    }

    // The floor

    [Fact]
    public void TheFloorKeepsADropperFromItsOwnStackForAWholeNumberOfTicksButNotLong()
    {
        Assert.True(Floor.OwnPickupDelay > 0);
        Assert.True(Floor.OwnPickupDelay <= 10 * 20);
    }

    [Fact]
    public void TheFloorThrowsInWholeThousandthsATickOutPastTheDriftAndSlowsWhatItThrows()
    {
        AllPositive(typeof(Floor));
        // Thrown out into space, a stack must leave the ship behind, not drift back onto it.
        Assert.True(Floor.ThrowSpeed > Floor.SpaceDrift);
        Assert.True(Floor.Friction < Floor.ThrowSpeed);
        Assert.True(Floor.Gravity < Floor.MaxFall);
    }

    [Fact]
    public void TheFloorPushesAStackOutOfAHullCornerAndTheMiddleOfAnyMachine()
    {
        // A hull corner is two steps from the room; a machine's middle half its narrower side, rounded up.
        Assert.True(Floor.PushReach >= 2);
        foreach (var m in Machines.All) Assert.True(Floor.PushReach >= (Math.Min(m.Width, m.Height) + 1) / 2, m.Kind);
    }

    [Fact]
    public void EveryItemSortsInTheGroupItsOtherFieldsSayAndTheHotbarIsTheBagsFirstRow()
    {
        // Worn is a wearable; gear not worn is a tool: a new tool sorts with the tools without being told.
        Assert.All(Content.Items.All, i => Assert.Equal(i.Equip != null, i.Group == ItemGroup.Wearable));
        Assert.All(Content.Items.All, i => Assert.Equal(i.Gear && i.Equip == null, i.Group == ItemGroup.Tool));
        // Salvage smelts into materials; what is never carried is caught or broken, a stack of one.
        Assert.All(Content.Items.All.Where(i => i.Group == ItemGroup.Salvage), i => Assert.NotNull(i.Yields));
        Assert.All(Content.Items.All.Where(i => i.Group == ItemGroup.Other), i => Assert.Equal(1, i.Stack));
        Assert.Equal(ItemGroup.Salvage, Content.Items.Get("scrapChunk").Group);
        Assert.Equal(ItemGroup.Material, Content.Items.Get("scrap").Group);
        Assert.InRange(Inventory.Hotbar, 1, Inventory.Slots - 1);
    }

    [Fact]
    public void WhatHoldsChargeIsAStackOfOneNamedInNoCostAndAStationFillsItInWholeTicks()
    {
        AllPositive(typeof(Charge));
        var holding = Content.Items.All.Where(i => i.Capacity != null).ToList();
        Assert.NotEmpty(holding);
        Assert.All(holding, i => Assert.Equal(1, i.Stack));
        Assert.All(holding, i => Assert.True(i.Capacity > 0, i.Id));
        // Batteries are worn on the belt; a drone holds its own and flies on it (0062).
        Assert.All(holding.Where(i => i.Id != Content.Logistics.Drone), i => Assert.Equal(Charge.Belt, i.Equip));
        Assert.Contains(holding, i => i.Id == Content.Logistics.Drone);
        Assert.Contains(Inventory.Equipment, kind => kind == Charge.Belt);
        // Paying never takes what holds charge, so no charge is lost to a recipe, a build or a cable; but the drone, whose
        // battery becomes its own, and the accumulator, a battery wound round a shard (0099): made empty, as a crafted battery
        // is, the battery's charge is lost there.
        string[] ofABattery = [Content.Logistics.Drone, "accumulator"];
        var costs = Crafts.All.Where(c => !ofABattery.Contains(c.Item)).Select(c => c.Cost)
            .Concat(AllBuilds().Select(b => b.Cost))
            .Append(Content.Power.CableCost);
        Assert.All(costs, cost => Assert.DoesNotContain(cost, c => Content.Items.Get(c.Item).Capacity != null));
        // A station puts in what it draws: it fills every store in whole ticks at full power.
        foreach (var station in Machines.All.Where(m => m.Charges))
        {
            Assert.True(station.Draws > 0 && station.Slots > 0, station.Kind);
            Assert.Null(station.Inputs);
            Assert.All(holding, i => Assert.Equal(0, i.Capacity!.Value % (station.Draws!.Value * Charge.Scale)));
        }
        // Consumers draw as machines do, in whole power units; a store is no consumer.
        Assert.All(Content.Items.All.Where(i => i.Draw != null), i => Assert.True(i.Draw > 0, i.Id));
        Assert.DoesNotContain(Content.Items.All, i => i.Draw != null && i.Capacity != null);
    }
}
