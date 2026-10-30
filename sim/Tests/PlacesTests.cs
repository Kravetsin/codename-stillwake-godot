using System.Diagnostics;
using System.Text.Json.Nodes;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Places;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Jumping beside a generated vessel (generated-vessels): written into its box right of the ship, the last one cleared.</summary>
public class PlacesTests
{
    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>A contact of <paramref name="cls"/> found and jumped to on <paramref name="w"/>, or on the new game's world.</summary>
    private static (Scenario W, Box Box) Jumped(string cls = "freighter", bool intact = false, Scenario? w = null)
    {
        w ??= new Scenario(Registry.Wreck);
        w.Do(new Find(cls)).Run();
        w.Do(new Jump(w.M.Places.Contacts[^1].Id, intact)).Run();
        Assert.Empty(w.Refusals);
        return (w, w.M.Grid.Reserved!.Value);
    }

    /// <summary>What every cell holds, in every layer: what a jump must leave alone outside its box.</summary>
    private static (int[] Structure, int[] Back, int[] Machine) Cells(Scenario w)
    {
        var grid = w.M.Grid;
        int size = grid.Width * grid.Height;
        return (grid.StructureLayer.ToArray(), grid.BackLayer.ToArray(), Enumerable.Range(0, size).Select(c => grid.MachineAt(c)?.Id ?? 0).ToArray());
    }

    private static void SameOutside(Scenario w, (int[] Structure, int[] Back, int[] Machine) before, params Box[] boxes)
    {
        var now = Cells(w);
        int width = w.M.Grid.Width;
        for (int c = 0; c < before.Structure.Length; c++)
        {
            if (boxes.Any(b => b.Contains(c % width, c / width))) continue;
            Assert.True(before.Structure[c] == now.Structure[c] && before.Back[c] == now.Back[c] && before.Machine[c] == now.Machine[c], $"cell {c} changed");
        }
    }

    private static bool Empty(Scenario w, int cell) =>
        w.M.Grid.StructureAt(cell) == Structure.Empty && w.M.Grid.BackAt(cell) == null && w.M.Grid.MachineAt(cell) == null;

    [Fact]
    public void AJumpWritesTheVesselOnlyInItsBoxRightOfTheShipAndFillsItsCrates()
    {
        var w = new Scenario(Registry.Wreck);
        var built = w.M.Grid.Built;
        var before = Cells(w);
        var (_, box) = Jumped(w: w);
        Assert.Equal(built.Right + Vessels.Gap + 1, box.Left);
        Assert.Equal(built.Top + built.Height / 2 - box.Height / 2, box.Top);
        SameOutside(w, before, box);
        var crates = w.M.Grid.MachinesOf("crate").Where(m => box.Contains(m.Cell % w.M.Grid.Width, m.Cell / w.M.Grid.Width)).ToList();
        Assert.NotEmpty(crates);
        Assert.Contains(crates, c => w.M.Items.Crate(c.Id).Any(s => s != null));
        Assert.Empty(w.M.Places.Contacts);
    }

    [Fact]
    public void ASecondJumpOntoAnOverlappingBoxLeavesNothingOfTheFirstAndTheShipUntouched()
    {
        var w = new Scenario(Registry.Wreck);
        var before = Cells(w);
        var (_, first) = Jumped(intact: true, w: w);
        int width = w.M.Grid.Width;
        int wall = Enumerable.Range(0, width * w.M.Grid.Height).First(c => first.Contains(c % width, c / width) && w.M.Grid.WallAt(c) != null);
        w.Do(new Deconstruct(wall, "structure")).Run();
        w.GiveItems(0, (Axe, 1)).Do(new Enter(wall - 3 * width)).Run();
        int other = Enumerable.Range(0, width * w.M.Grid.Height).Last(c => first.Contains(c % width, c / width) && w.M.Grid.WallAt(c) != null);
        w.Do(HewTesting.Dig(w, other, Axe)).Run();
        Assert.Empty(w.Refusals);
        Assert.NotEmpty(w.M.Building.Plans);
        var (_, second) = Jumped("shuttle", w: w);
        Assert.True(first.Left == second.Left && first.Contains(second.Left, second.Top), "the boxes overlap");
        Assert.DoesNotContain(w.M.Building.Plans, p => first.Contains(p.Cell % width, p.Cell / width));
        Assert.DoesNotContain(w.M.Building.Cracks, k => first.Contains(k.Cell % width, k.Cell / width));
        for (int y = first.Top; y <= first.Bottom; y++)
            for (int x = first.Left; x <= first.Right; x++)
                if (!second.Contains(x, y)) Assert.True(Empty(w, y * width + x), $"({x}, {y}) of the first vessel left");
        SameOutside(w, before, first, second);
    }

    [Fact]
    public void ABreachOpensItsHullWhereAnIntactVesselIsWhole()
    {
        int Backs(Scenario w, Box box) => Enumerable.Range(box.Top, box.Height).Sum(y =>
            Enumerable.Range(box.Left, box.Width).Count(x => w.M.Grid.BackAt(y * w.M.Grid.Width + x) != null));
        // The same contact's seed, one world jumping to it intact and one wrecked.
        var (intact, box) = Jumped(intact: true);
        var (wrecked, same) = Jumped(intact: false);
        Assert.Equal(box, same);
        Assert.True(Backs(wrecked, same) < Backs(intact, box));
    }

    [Fact]
    public void TheStreamAndTheMeteorsPassAVesselByAsIfItWereNotThere()
    {
        var (w, box) = Jumped();
        var still = new Scenario(Registry.Wreck);
        Assert.Equal(still.M.Debris.Hull(), w.M.Debris.Hull());
        // A meteor drawn again and again: never at the vessel ahead in its rows.
        int width = w.M.Grid.Width;
        for (int i = 0; i < 12; i++)
        {
            var root = JsonNode.Parse(w.World.Save())!;
            root["slices"]!["hazards"]!["nextIn"] = 1;
            w.Reload(root.ToJsonString()).Run();
            var meteor = w.M.Hazards.Incoming!;
            Assert.False(box.Contains(meteor.Cell % width, meteor.Cell / width), $"a meteor at {meteor.Cell}, in the vessel");
            w.Run(meteor.Left + 1);
        }
    }

    /// <summary>The new game's world with a wall laid at the zone's right edge on the ship's middle row: a ship built to its edge.</summary>
    private static Scenario ToTheEdge()
    {
        var w = new Scenario(Registry.Wreck);
        var (zone, built) = (w.M.Grid.Zone, w.M.Grid.Built);
        w.Do(new Place((built.Top + built.Height / 2) * w.M.Grid.Width + zone.Right, Placement.Wall())).Run();
        Assert.Equal(zone.Right, w.M.Grid.Built.Right);
        return w;
    }

    [Fact]
    public void AShipBuiltToTheZonesEdgeStillGetsItsVesselPastItAndItsBoxIsWarm()
    {
        var w = ToTheEdge();
        var (_, box) = Jumped(w: w);
        int width = w.M.Grid.Width, row = box.Top + box.Height / 2;
        Assert.Equal(w.M.Grid.Zone.Right + Vessels.Gap + 1, box.Left);
        Assert.True(box.Right > w.M.Grid.Zone.Right + Field.Margin, "it reaches past the field");
        Assert.Equal(0, w.M.Crew.EdgeCost(row * width + box.Right));
        Assert.True(w.M.Crew.EdgeCost(row * width + box.Right + 1) > 0);
        // The next vessel, a shuttle, leaves the cold back where the freighter was.
        Jumped("shuttle", w: w);
        Assert.True(w.M.Crew.EdgeCost(row * width + box.Right) > 0);
    }

    [Fact]
    public void NoOneBuildsInTheBoxInsideTheZoneOrPastIt()
    {
        var w = ToTheEdge();
        var (_, box) = Jumped(w: w);
        int width = w.M.Grid.Width, zoneRight = w.M.Grid.Zone.Right;
        // The box's corner, outside the hull: space in the box.
        int corner = box.Top * width + box.Left;
        Assert.True(Empty(w, corner));
        Assert.Equal("grid.reserved", w.M.Grid.PlacementProblem(corner, Placement.Wall()));
        w.Do(new PlacePlan(corner, Placement.Wall())).Run();
        Assert.Equal("grid.reserved", Assert.Single(w.Refusals).Reason);
        // A pipe or a cable to the vessel's wall from the ship's.
        int ship = (box.Top + box.Height / 2) * width + zoneRight;
        int wall = Enumerable.Range(box.Left, box.Width).Select(x => (box.Top + box.Height / 2) * width + x).First(c => w.M.Grid.WallAt(c) != null);
        Assert.Equal("grid.reserved", w.M.Pipes.LayProblem(0, ship - 4, wall));
        Assert.Equal("grid.reserved", w.M.Power.LayProblem(0, ship, wall));
        w.Refusals.Clear();
        w.Do(new LayPipe(ship - 4, wall)).Do(new Power.Lay(ship, wall)).Run();
        Assert.All(w.Refusals, r => Assert.Equal("grid.reserved", r.Reason));
        Assert.Equal(2, w.Refusals.Count);
    }

    [Fact]
    public void TheAxeBreaksAVesselsWallAndNothingOfTheShips()
    {
        var (w, box) = Jumped(intact: true);
        int width = w.M.Grid.Width, row = box.Top + box.Height / 2;
        w.GiveItems(0, (Axe, 1)).Do(new Enter(row * width + box.Left - 4)).Run();
        int wall = Enumerable.Range(box.Left, box.Width).Select(x => row * width + x).First(c => w.M.Grid.WallAt(c) != null);
        var hew = HewTesting.Dig(w, wall, Axe);
        w.Do(hew).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Grid.WallAt(wall) == null || w.M.Building.Cracks.Any(k => k.Cell == wall));
        // Elsewhere aboard the axe takes nothing: the plan takes the ship apart.
        int shipWall = Enumerable.Range(0, width * w.M.Grid.Height).First(c => !box.Contains(c % width, c / width) && w.M.Grid.WallAt(c) != null);
        Assert.Equal("building.notHere", HewTesting.DigProblem(w, 0, shipWall, Axe));
        w.Do(HewTesting.Dig(w, shipWall, Axe)).Run();
        Assert.Equal("building.notHere", w.Refusals[^1].Reason);
        Assert.NotNull(w.M.Grid.WallAt(shipWall));
        Assert.DoesNotContain(w.M.Building.Cracks, k => k.Cell == shipWall);
    }

    [Fact]
    public void AMapWithNoRoomRightOfTheShipRefusesTheJumpAndChangesNothing()
    {
        var w = new Scenario("""
            #####
            #...#
            #####
            """);
        w.Do(new Find("shuttle")).Run();
        string before = w.World.Save();
        w.Do(new Jump(w.M.Places.Contacts[0].Id)).Run();
        Assert.Equal("places.noRoom", Assert.Single(w.Refusals).Reason);
        Assert.Null(w.M.Grid.Reserved);
        Assert.Single(w.M.Places.Contacts);
        Assert.Equal(JsonNode.Parse(before)!["slices"]!["grid"]!.ToJsonString(), JsonNode.Parse(w.World.Save())!["slices"]!["grid"]!.ToJsonString());
    }

    [Fact]
    public void OffTheShipNothingIsFoundJumpedToNorDismissed()
    {
        var w = new Scenario(Layout.Parse("..#..").OnPlanet());
        w.Do(new Find("shuttle")).Do(new Jump(1)).Do(new Dismiss()).Run();
        Assert.Equal(["places.notAboard", "places.notAboard", "places.notAboard"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void FindRefusesAClassThereIsNotAndAFullRadarAndJumpAContactNotHeld()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Find("nothing")).Run();
        Assert.Equal("places.noClass", Assert.Single(w.Refusals).Reason);
        for (int i = 0; i <= Vessels.MaxContacts; i++) w.Do(new Find("shuttle")).Run();
        Assert.Equal(Vessels.MaxContacts, w.M.Places.Contacts.Count);
        Assert.Equal("places.full", w.Refusals[^1].Reason);
        w.Do(new Jump(999)).Run();
        Assert.Equal("places.noContact", w.Refusals[^1].Reason);
    }

    [Fact]
    public void ADismissedVesselLeavesNothingAndItsBoxIsBuiltInAgain()
    {
        var w = new Scenario(Registry.Wreck);
        var before = Cells(w);
        var (_, box) = Jumped(w: w);
        int width = w.M.Grid.Width;
        int wall = Enumerable.Range(0, width * w.M.Grid.Height).First(c => box.Contains(c % width, c / width) && w.M.Grid.WallAt(c) != null);
        w.Do(new Deconstruct(wall, "structure")).Run();
        w.Do(new Dismiss()).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.Reserved);
        Assert.Empty(w.M.Building.Plans);
        SameOutside(w, before);
        int corner = box.Top * width + box.Left;
        Assert.Null(w.M.Grid.PlacementProblem(corner, Placement.Wall()));
        w.Do(new Dismiss()).Run();
        Assert.Equal("places.noVessel", Assert.Single(w.Refusals).Reason);
    }

    [Fact]
    public void AVesselAndTheRadarsContactsSurviveASave()
    {
        var (w, box) = Jumped();
        w.Do(new Find("shuttle")).Run();
        var contacts = w.M.Places.Contacts.ToList();
        string before = w.World.Save();
        w.Reload();
        Assert.Equal(box, w.M.Grid.Reserved);
        Assert.Equal(contacts, w.M.Places.Contacts);
        Assert.Equal(before, w.World.Save());
    }

    /// <summary>The freighter's jump tick on the new game's world, cleared and written, Rooms and the rest after it (generated-vessels).</summary>
    [Fact]
    public void AFreightersJumpTickStaysUnderItsCeiling()
    {
        // Measured 33–70 ms on a desk (the first with the JIT warming): a hitch of a few frames once per jump.
        const int CeilingMs = 200;
        var (w, _) = Jumped();
        w.Do(new Find("freighter")).Run();
        w.Do(new Jump(w.M.Places.Contacts[^1].Id));
        var clock = Stopwatch.StartNew();
        w.Run();
        clock.Stop();
        Assert.Empty(w.Refusals);
        Assert.True(clock.ElapsedMilliseconds < CeilingMs, $"{clock.ElapsedMilliseconds} ms");
    }
}
