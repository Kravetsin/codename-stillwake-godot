using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>Machine footprints, builds, the drone bay, air, power and light.</summary>
public partial class ContentTests
{
    [Fact]
    public void MachinesHaveAFootprintOfAtLeastOneCellAndPortsInsideIt()
    {
        foreach (var m in Machines.All)
        {
            Assert.True(m.Width >= 1 && m.Height >= 1, m.Kind);
            foreach (var p in m.Ports)
                Assert.True(p.X >= 0 && p.X < m.Width && p.Y >= 0 && p.Y < m.Height, m.Kind);
        }
    }

    [Fact]
    public void MachinesBuiltIntoTheShipAreTheLifepodAloneAMapDrawsItAndItCostsNothing()
    {
        Assert.Equal(["lifepod"], Machines.All.Where(m => m.Fixed).Select(m => m.Kind));
        var pod = Machines.Get("lifepod");
        Assert.Empty(pod.Cost);
        Assert.Equal((0, null, null), (pod.Inputs, pod.Makes, pod.Draws));
        Assert.Contains(Legend.Map, e => Legend.MachineOf(e.Char) == "lifepod");
        Assert.Single(Layout.Parse(Wreck.Rows).Doubled().Machines(), m => m.Kind == "lifepod");
        var (x, y) = Content.Crew.PodHatch;
        Assert.True(x >= 0 && x < pod.Width && y >= 0 && y < pod.Height);
    }

    [Fact]
    public void AMapsMachinesAreFoundOnceAndACopyDrawingOthersFindsItsOwn()
    {
        var drawn = Layout.Parse(Wreck.Rows).Doubled();
        var machines = drawn.Machines();
        Assert.Same(machines, drawn.Machines());
        var blank = drawn with { Rows = [.. drawn.Rows.Select(r => new string(' ', r.Length))] };
        Assert.Empty(blank.Machines());
        Assert.Contains(drawn.Machines(), m => m.Kind == "lifepod");
        Assert.Equal(drawn, drawn with { });
    }

    // Builds

    [Fact]
    public void BuildsCostWhatAnEmptyInventoryHoldsWallsIncluded()
    {
        foreach (var cost in AllBuilds().Select(b => b.Cost))
            Assert.True(SlotsFor(cost) <= Inventory.Slots);
    }

    [Fact]
    public void BuildsCostWholeUnitsOfRealItemsSomethingForEachAndTakeWholeTicks()
    {
        foreach (var (cost, buildTicks) in AllBuilds())
        {
            Assert.NotEmpty(cost);
            foreach (var (item, amount) in cost)
            {
                Assert.True(Content.Items.TryGet(item, out _), item);
                Assert.Null(Loot.Of(item));
                Assert.True(amount > 0, item);
            }
            Assert.True(buildTicks > 0);
            // A tick of work at least for each item, so every part a build is paid in has a tick (0063).
            Assert.True(buildTicks >= cost.Sum(c => c.Count), $"{buildTicks} ticks for {cost.Sum(c => c.Count)} items");
        }
    }

    [Fact]
    public void BuildsComeApartFasterThanTheyGoUpByAWholeFactor() => Assert.True(Builds.DeconstructSpeedup > 1);

    /// <summary>
    /// Drones (0062): every number positive and whole; a bay is a charging station of slots on the power grid, a drone an
    /// item of a stack of one holding charge, made at the assembler; a drone crosses at most a cell a tick, launches short
    /// of full, and full covers any job its bay may find and the way home.
    /// </summary>
    [Fact]
    public void ABayChargesItsSlotsADroneIsABatteryOfItsOwnAndEveryLogisticsNumberIsPositive()
    {
        AllPositive(typeof(Content.Logistics));
        var bay = Machines.Get(Content.Logistics.Bay);
        Assert.True(bay.Slots > 0 && bay.Draws > 0 && bay.Plug != null && bay.Charges);
        Assert.Equal((null, null), (bay.Inputs, bay.FuelSlot));
        var drone = Content.Items.Get(Content.Logistics.Drone);
        Assert.Equal((1, Charge.Drone, null), (drone.Stack, drone.Capacity, drone.Draw));
        Assert.Contains(Stations.Get("assembler").Rows, row => row.Contains(Content.Logistics.Drone));
        Assert.InRange(Content.Logistics.Speed, 1, Cell);
        Assert.InRange(Content.Logistics.LaunchAt, 1, Charge.Drone - 1);
        // Corner to corner of the zone three times, to the source, the target and home, and the two cells spared.
        int ticks = (Cell + Content.Logistics.Speed - 1) / Content.Logistics.Speed;
        int across = 4 * Content.Logistics.Reach + bay.Width + bay.Height - 2;
        Assert.True(Charge.Drone >= (3 * across + 2) * ticks * Content.Logistics.Draw * Charge.Scale);
    }

    // Air

    [Fact]
    public void AirIsInWholeUnitsAndATankTakesAtLeastOneCanister()
    {
        AllPositive(typeof(Air));
        Assert.True(Air.Canister <= Air.TankCapacity);
    }

    // Power

    [Fact]
    public void PowerIsInWholeUnitsAndACableCostsSomethingAndSpansMoreThanACell()
    {
        Assert.True(Content.Power.MaxSpan > 1);
        foreach (var (item, amount) in Content.Power.CableCost)
        {
            // What the BuildCost type held in TypeScript: items there are.
            Assert.True(Content.Items.TryGet(item, out _), item);
            Assert.True(amount > 0, item);
        }
        foreach (var m in Machines.All)
            foreach (int? value in (int?[])[m.Makes, m.Draws])
                if (value != null) Assert.True(value > 0, m.Kind);
    }

    [Fact]
    public void PowerGivesEveryMachineMakingOrDrawingPowerOnePlugInsideItsPictureAndNoOtherAPlug()
    {
        foreach (var m in Machines.All)
        {
            bool powered = m.Powered;
            Assert.True(m.Plug != null == powered, m.Kind);
            if (m.Plug is not { } plug) continue;
            Assert.True(plug.X >= 0 && plug.X < m.Width * 16, m.Kind);
            Assert.True(plug.Y >= 0 && plug.Y < m.Height * 16, m.Kind);
        }
    }

    [Fact]
    public void PowerLetsTheWrecksOnePanelRunItsGeneratorInFull()
    {
        Assert.True(Machines.Get("solarPanel").Makes >= Machines.Get("generator").Draws);
        Assert.NotEmpty(Wreck.Cables);
    }

    // Light

    [Fact]
    public void LightFadesInAirMoreThroughMachinesAndACellThickWallLetsNextToNoneThrough()
    {
        Assert.True(Light.Margin >= 0);
        Assert.True(Light.Rounds >= 1);
        foreach (float keep in (float[])[Light.StarKeep, Light.StarMachine, Light.WallIn, Light.WallDeep, Light.WallOut])
        {
            Assert.True(keep > 0);
            Assert.True(keep < 1);
        }
        Assert.True(Light.StarMachine < Light.StarKeep);
        // Through a wall a cell thick, into the cell beyond: a tenth at most.
        Assert.True(Light.WallIn * Light.WallOut * Light.StarKeep < 0.1f);
    }

    [Fact]
    public void LightComesInColoursAbove0SourcesAtMostTwiceWhiteNeverLeavingACornerBlack()
    {
        foreach (var (r, g, b) in (List<(float, float, float)>)[Light.Star, Light.Lamp, Light.Fire])
            foreach (float n in (float[])[r, g, b])
            {
                Assert.True(n > 0);
                Assert.True(n <= 2);
            }
        foreach (float n in (float[])[Light.Ambient.R, Light.Ambient.G, Light.Ambient.B])
        {
            Assert.True(n > 0);
            Assert.True(n < 1);
        }
        Assert.True(Light.Particle > 0);
    }
}
