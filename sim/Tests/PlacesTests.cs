using System.Diagnostics;
using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Places;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Going to a generated vessel (vessel-worlds): a surface of its own opened by the jump, closed once nobody is on it.</summary>
public class PlacesTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static readonly string Axe = Weapons.Axe.Item;

    /// <summary>
    /// The last contact of <paramref name="cls"/> held, else one found, jumped to by peer <paramref name="peer"/>'s hero on <paramref name="w"/>, or on
    /// the new game's world, the hero let in first: the vessel world's id.
    /// </summary>
    private static (Scenario W, int Vessel) Visited(string cls = "freighter", Scenario? w = null, int peer = 0)
    {
        w ??= new Scenario(Registry.Wreck);
        if (w.M.Crew.HeroOf(peer) == null) w.Do(new Enter(w.M.Crew.Spawn) { From = peer }).Run();
        if (!w.M.Places.Contacts.Any(k => k.Class == cls)) w.Do(new Find(cls)).Run();
        w.Do(new Jump(w.M.Places.Contacts.Last(k => k.Class == cls).Id) { From = peer }).Run();
        Assert.Empty(w.Refusals);
        int vessel = w.World.SurfaceOf(peer);
        Assert.NotEqual(0, vessel);
        return (w, vessel);
    }

    /// <summary>What every cell holds, in every layer.</summary>
    private static (int[] Structure, int[] Back, int[] Machine) Cells(Registry m)
    {
        var grid = m.Grid;
        int size = grid.Width * grid.Height;
        return (grid.StructureLayer.ToArray(), grid.BackLayer.ToArray(), Enumerable.Range(0, size).Select(c => grid.MachineAt(c)?.Id ?? 0).ToArray());
    }

    /// <summary>The cell of (<paramref name="x"/>, <paramref name="y"/>) in a vessel's frame on its world's grid.</summary>
    private static int InFrame(Registry v, int x, int y) => (Vessels.Margin + y) * v.Grid.Width + Vessels.Margin + x;

    [Fact]
    public void AJumpOpensAVesselWorldOfTheContactsSeedAndMovesItsSenderJustInsideTheDoorTheShipUntouched()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        var before = Cells(w.M);
        w.Do(new Find("freighter")).Run();
        var contact = w.M.Places.Contacts[^1];
        var (_, id) = Visited(w: w);
        var surface = w.World.Surfaces[id];
        Assert.Equal(("vessel.freighter", contact.Seed, false), (surface.Kind, surface.Seed, surface.Closed));
        Assert.DoesNotContain(w.M.Places.Contacts, k => k.Id == contact.Id);
        var after = Cells(w.M);
        Assert.Equal(before.Structure, after.Structure);
        Assert.Equal(before.Back, after.Back);
        Assert.Equal(before.Machine, after.Machine);
        var v = w.On(id);
        var vessel = Shipyard.Describe(Vessels.Class("freighter")!, contact.Seed);
        var (dx, dy) = Shipyard.Door(vessel);
        Assert.Equal(InFrame(v, dx, dy), v.Crew.Spawn);
        Assert.Equal(v.Crew.Spawn, v.Crew.HeroOf(0)!.Cell);
        Assert.Null(w.M.Crew.HeroOf(0));
    }

    [Theory]
    [InlineData("shuttle")]
    [InlineData("freighter")]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void TheHeroStandsOnTheDeckJustInsideTheDoorWithRoomForItsBody(string cls)
    {
        var (w, id) = Visited(cls);
        var grid = w.On(id).Grid;
        int cell = w.On(id).Crew.HeroOf(0)!.Cell, width = grid.Width;
        // Two columns and a deck's six rows open over a floor that holds it.
        for (int up = 0; up < Vessels.Open; up++)
            for (int across = 0; across < 2; across++)
                Assert.False(grid.Blocks(cell - up * width + across), $"blocked {up} up, {across} across");
        Assert.True(grid.Solid(cell + width));
        Assert.True(Doorways.IsDoor(grid.StructureAt(cell - 1)));
    }

    [Fact]
    public void TheVesselIsWrittenAsTheShipyardStampsItItsFrameAtTheMarginAndItsCratesFilled()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new Find("shuttle")).Run();
        uint seed = w.M.Places.Contacts[^1].Seed;
        var (_, id) = Visited("shuttle", w);
        var v = w.On(id);
        var vessel = Shipyard.Describe(Vessels.Class("shuttle")!, seed);
        Assert.Equal((vessel.Width + 2 * Vessels.Margin, vessel.Height + 2 * Vessels.Margin), (v.Grid.Width, v.Grid.Height));
        foreach (var part in Shipyard.Stamp(vessel))
        {
            int cell = InFrame(v, part.X, part.Y);
            switch (part.Layer)
            {
                case "machine": Assert.Equal(part.Kind, v.Grid.MachineAt(cell)?.Kind); break;
                case "back": Assert.NotNull(v.Grid.BackAt(cell)); break;
                default: Assert.NotEqual(Structure.Empty, v.Grid.StructureAt(cell)); break;
            }
        }
        var crates = v.Grid.MachinesOf("crate").ToList();
        Assert.NotEmpty(crates);
        Assert.Contains(crates, c => v.Items.Crate(c.Id).Any(s => s != null));
        // Nothing outside the frame: space all round.
        int width = v.Grid.Width;
        for (int c = 0; c < width * v.Grid.Height; c++)
        {
            int x = c % width - Vessels.Margin, y = c / width - Vessels.Margin;
            if (x >= 0 && y >= 0 && x < vessel.Width && y < vessel.Height) continue;
            Assert.True(v.Grid.StructureAt(c) == Structure.Empty && v.Grid.BackAt(c) == null && v.Grid.MachineAt(c) == null, $"cell {c}");
        }
    }

    [Fact]
    public void AVesselsDoorsLeftOpenAreLaidOpenAndItsRoomsAreRoomsOfTheirOwnBehindItsShutDoorsAndHatches()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new Find("freighter")).Run();
        uint seed = w.M.Places.Contacts[^1].Seed;
        var (_, id) = Visited(w: w);
        var v = w.On(id);
        var vessel = Shipyard.Describe(Vessels.Class("freighter")!, seed);
        // Every cell of a door as it was left: open or shut, the way in shut.
        Assert.Contains(vessel.Doors, d => d.Open);
        foreach (var door in vessel.Doors)
            for (int y = door.Y; y < door.Y + Vessels.Open; y++)
                for (int x = door.X; x < door.X + 2; x++)
                {
                    int code = v.Grid.StructureAt(InFrame(v, x, y));
                    Assert.True(Doorways.IsDoor(code));
                    Assert.Equal(door.Open, Doorways.IsOpen(code));
                }
        // Rooms an open door joins are one; the rest each a room of its own.
        var group = Enumerable.Range(0, vessel.Compartments.Count).ToArray();
        int Root(int i) => group[i] == i ? i : Root(group[i]);
        foreach (var door in vessel.Doors.Where(d => d.Open))
        {
            int left = vessel.Compartments.ToList().FindIndex(c => c.Top == door.Y && c.Left + c.Width == door.X);
            int right = vessel.Compartments.ToList().FindIndex(c => c.Top == door.Y && c.Left == door.X + 2);
            group[Root(left)] = Root(right);
        }
        var rooms = vessel.Compartments.Select(c => v.Rooms.RoomAt(InFrame(v, c.Left, c.Top + Vessels.Open - 1))).ToList();
        Assert.All(rooms, r => Assert.NotNull(r));
        for (int i = 0; i < rooms.Count; i++)
            for (int j = i + 1; j < rooms.Count; j++) Assert.Equal(Root(i) == Root(j), rooms[i] == rooms[j]);
    }

    /// <summary>Every part outside a vessel's hull, with a class that carries it.</summary>
    public static TheoryData<string, string> OutsideParts() => new()
    {
        { "shuttle", Vessels.Nozzle }, { "freighter", Vessels.Nozzle }, { "shuttle", Vessels.Rcs }, { "shuttle", Vessels.Mast },
        { "freighter", Vessels.MastTall }, { "shuttle", Vessels.Dish }, { "shuttle", Vessels.Solar }, { "shuttle", Vessels.Tank },
        { "freighter", Vessels.Radiator }, { "freighter", Vessels.Collar }, { "freighter", Vessels.Truss }, { "freighter", Vessels.Pylon }, { "freighter", Vessels.Boom },
        { "freighter", Vessels.Container },
    };

    [Fact]
    public void EveryOutsidePartIsTakenApartForWhatThePlanSays()
    {
        Assert.Equal(Vessels.Outside.Select(r => Vessels.FirstLook(r.Kind)).Distinct().Order(), OutsideParts().Select(d => (string)d[1]).Distinct().Order());
        Assert.Equal([("plate", 4), ("copper", 2)], Machines.Get(Vessels.Nozzle).Cost);
        Assert.Equal([("tube", 2), ("scrap", 3)], Machines.Get(Vessels.Rcs).Cost);
        Assert.Equal(Machines.Get(Vessels.Mast).Cost, Machines.Get(Vessels.MastTall).Cost);
        Assert.Equal([("wire", 2), ("circuit", 1)], Machines.Get(Vessels.Boom).Cost);
        Assert.Equal([("plate", 6), ("planks", 8), ("wire", 3), ("tube", 2)], Machines.Get(Vessels.Container).Cost);
        Assert.All(Vessels.Outside, r => Assert.DoesNotContain(r.Cost, c => c.Item == "nozzle"));
        Assert.All(Vessels.Outside, r => Assert.Contains(r.Kind, Vessels.Machines));
        Assert.All(Vessels.Outside, r => Assert.Null(r.Menu));
    }

    [Theory]
    [MemberData(nameof(OutsideParts))]
    public void AnOutsidePartBrokenByTheSledgehammerGivesWhatItGivesCountedObtained(string cls, string kind)
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.GiveItems(0, (Weapons.Sledgehammer.Item, 1));
        var (_, id) = Visited(cls, w);
        var v = w.On(id);
        Hammered(w, v, v.Grid.Machines.First(m => Vessels.FirstLook(m.Kind) == kind));
    }

    /// <summary>The furniture inside a vessel and its lamp, a row each, every look but the first alike.</summary>
    public static TheoryData<string> Furniture() => [.. Vessels.Furniture.Select(r => Vessels.FirstLook(r.Kind)).Distinct()];

    [Fact]
    public void EveryPieceOfFurnitureIsTakenApartForWhatTheMockupSays()
    {
        Assert.Equal([("wire", 1), ("glass", 1)], Machines.Get(Vessels.Lamp).Cost);
        Assert.Equal([("plate", 2), ("cloth", 2)], Machines.Get(Vessels.Bunk).Cost);
        Assert.Equal([("plate", 2)], Machines.Get(Vessels.Locker).Cost);
        Assert.Equal([("plate", 1)], Machines.Get(Vessels.Table).Cost);
        Assert.Equal([("plate", 1), ("cloth", 1)], Machines.Get(Vessels.Seat).Cost);
        Assert.Equal([("canister", 2), ("tube", 2)], Machines.Get(Vessels.LifeTank).Cost);
        Assert.Equal([("circuit", 2), ("wire", 3)], Machines.Get(Vessels.Console).Cost);
        Assert.Equal([("gear", 3), ("rotor", 1)], Machines.Get(Vessels.Machine).Cost);
        Assert.Equal([("wire", 6), ("copper", 2)], Machines.Get(Vessels.Rack).Cost);
        Assert.Equal([("plate", 4), ("planks", 6), ("wire", 2)], Machines.Get(Vessels.Cargo).Cost);
        // A liner's (big-vessels): the design's yields in existing items.
        Assert.Equal([("planks", 3), ("glass", 2), ("plate", 1)], Machines.Get(Vessels.Bar).Cost);
        Assert.Equal([("planks", 4), ("wire", 3), ("copper", 1)], Machines.Get(Vessels.Piano).Cost);
        Assert.Equal([("planks", 1), ("cloth", 1)], Machines.Get(Vessels.RoundTable).Cost);
        Assert.Equal([("planks", 1)], Machines.Get(Vessels.Chair).Cost);
        Assert.Equal([("frame", 1), ("cloth", 1)], Machines.Get(Vessels.Lounger).Cost);
        Assert.Equal([("frame", 2), ("planks", 1)], Machines.Get(Vessels.Diving).Cost);
        Assert.Equal([("plate", 6), ("tube", 2)], Machines.Get(Vessels.Pool).Cost);
        Assert.Equal([("glass", 3), ("wire", 2), ("copper", 1)], Machines.Get(Vessels.Chandelier).Cost);
        Assert.Equal([("plate", 2), ("coil", 1), ("wire", 1)], Machines.Get(Vessels.Stove).Cost);
        Assert.Equal([("plate", 2)], Machines.Get(Vessels.Counter).Cost);
        Assert.Equal([("plate", 10), ("tube", 4)], Machines.Get(Vessels.Capsule).Cost);
        Assert.Equal([("plate", 14), ("wire", 6), ("glass", 2), ("tube", 3)], Machines.Get(Vessels.ShuttleGull).Cost);
        Assert.Equal([("plate", 20), ("frame", 4), ("tube", 4)], Machines.Get(Vessels.ShuttleMule).Cost);
        Assert.Equal([("plate", 12), ("gear", 4), ("rotor", 1)], Machines.Get(Vessels.ShuttleBeetle).Cost);
        Assert.Equal([("plate", 16), ("glass", 6), ("cloth", 4)], Machines.Get(Vessels.ShuttleWhale).Cost);
        Assert.Equal([("plate", 12), ("frame", 3), ("copper", 1)], Machines.Get(Vessels.ShuttleHeron).Cost);
        // A tanker's (big-vessels): the design's yields.
        Assert.Equal([("gear", 2), ("tube", 3), ("rotor", 1)], Machines.Get(Vessels.Pump).Cost);
        Assert.Equal([("tube", 3), ("gear", 1)], Machines.Get(Vessels.Valves).Cost);
        Assert.Equal([("plate", 6), ("tube", 2)], Machines.Get(Vessels.TankTall).Cost);
        Assert.Equal([("plate", 14), ("tube", 4)], Machines.Get(Vessels.FuelColumn(3)).Cost);
        Assert.Equal([("plate", 18), ("tube", 4)], Machines.Get(Vessels.FuelColumn(4)).Cost);
        Assert.All(Vessels.Furniture, r => Assert.Contains(r.Kind, Vessels.Machines));
        Assert.All(Vessels.Furniture, r => Assert.Null(r.Menu));
        Assert.All(Vessels.Furniture, r => Assert.All(r.Cost, c => Assert.True(Content.Items.TryGet(c.Item, out _), c.Item)));
    }

    [Theory]
    [MemberData(nameof(Furniture))]
    public void APieceOfFurnitureBrokenByTheSledgehammerGivesWhatItGivesCountedObtained(string kind)
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.GiveItems(0, (Weapons.Sledgehammer.Item, 1));
        var (_, id) = Visited("freighter", w);
        var v = w.On(id);
        // Set in the space round the vessel, clear of it: what it gives does not hang on the room it stands in.
        int cell = Vessels.Margin / 2 * v.Grid.Width + Vessels.Margin / 2;
        v.Grid.Place(cell, new Placement("machine", kind));
        Hammered(w, v, v.Grid.MachineAt(cell)!);
    }

    /// <summary>
    /// <paramref name="part"/> on the vessel world <paramref name="v"/> broken by the hero's sledgehammer from inside its top-left cell,
    /// straight up, where every ray meets it first; what it gives in the hero's inventory and counted obtained.
    /// </summary>
    private static void Hammered(Scenario w, Registry v, Machine part)
    {
        string hammer = Weapons.Sledgehammer.Item;
        int width = v.Grid.Width;
        var gives = Machines.Get(part.Kind).Cost;
        var before = gives.Select(c => w.M.Knowledge.ObtainedOf(c.Item)).ToList();
        w.Do(new Enter(part.Cell)).Run();
        for (int blow = 0; blow < 12 && v.Grid.MachineById(part.Id) != null; blow++)
            w.Do(new Hew(hammer, part.Cell % width * 1000 + 500, part.Cell / width * 1000 + 500, HewTesting.Up)).Run(Weapons.Sledgehammer.Pace);
        Assert.Empty(w.Refusals);
        Assert.Null(v.Grid.MachineById(part.Id));
        for (int i = 0; i < gives.Count; i++)
        {
            Assert.Equal(gives[i].Count, v.Items.Inventory(0).Sum(s => s?.Item == gives[i].Item ? s.Count : 0));
            Assert.Equal(before[i] + gives[i].Count, w.M.Knowledge.ObtainedOf(gives[i].Item));
        }
    }

    [Fact]
    public void OnAVesselTheAxeBreaksItsWallsNothingIsMarkedAndOnlyWhatAPlanetAllowsIsBuilt()
    {
        var w = new Scenario(Registry.Wreck with { Known = null });
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.GiveItems(0, (Axe, 1));
        var (_, id) = Visited(w: w);
        var v = w.On(id);
        int width = v.Grid.Width, row = Vessels.Margin + (v.Grid.Height - 2 * Vessels.Margin) / 2;
        // The hero out in space left of the vessel, level with its middle; the first wall of that row.
        w.Do(new Enter(row * width + Vessels.Margin - 4)).Run();
        int wall = Enumerable.Range(Vessels.Margin, width - 2 * Vessels.Margin).Select(x => row * width + x).First(c => v.Grid.WallAt(c) != null);
        w.Do(HewTesting.Dig(w, wall, Axe, surface: id)).Run();
        Assert.Empty(w.Refusals);
        Assert.True(v.Grid.WallAt(wall) == null || v.Building.Cracks.Any(k => k.Cell == wall));
        Assert.Equal("building.notHere", v.Building.Problem(wall, "structure"));
        // Out in the space round it: a wall, as on a planet; a machine, never.
        int space = 10 * width + 10;
        Assert.Null(v.Building.Problem(space, Placement.Wall(), 0));
        Assert.Equal("building.notHere", v.Building.Problem(space, new Placement("machine", "crate"), 0));
    }

    [Fact]
    public void TheSledgehammerBreaksAVesselsSalvageForItsCostCountedObtained()
    {
        string hammer = Weapons.Sledgehammer.Item;
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.GiveItems(0, (hammer, 1));
        var (_, id) = Visited(w: w);
        var v = w.On(id);
        int width = v.Grid.Width;
        var salvage = v.Grid.Machines.First(m => Vessels.SalvageOf(m.Kind) != null && Machines.Get(m.Kind).Hp <= Weapons.Sledgehammer.Damage);
        var cost = Machines.Get(salvage.Kind).Cost;
        var before = cost.Select(c => w.M.Knowledge.ObtainedOf(c.Item)).ToList();
        w.Do(new Enter(salvage.Cell)).Run();
        // A blow from inside its top-left cell, straight up: the first thing every ray meets is the pile.
        w.Do(new Hew(hammer, salvage.Cell % width * 1000 + 500, salvage.Cell / width * 1000 + 500, HewTesting.Up)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(v.Grid.MachineById(salvage.Id));
        for (int i = 0; i < cost.Count; i++)
        {
            Assert.Equal(cost[i].Count, v.Items.Inventory(0).Sum(s => s?.Item == cost[i].Item ? s.Count : 0));
            Assert.Equal(before[i] + cost[i].Count, w.M.Knowledge.ObtainedOf(cost[i].Item));
        }
    }

    [Fact]
    public void OffTheShipNothingIsFoundNorJumpedTo()
    {
        var w = new Scenario(Layout.Parse("..#..").OnPlanet());
        w.Do(new Find("shuttle")).Do(new Jump(1)).Run();
        Assert.Equal(["places.notAboard", "places.notAboard"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void FindRefusesAClassThereIsNotAndAFullRadarAndJumpAContactNotHeldOrASenderWithNoHero()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Find("nothing")).Run();
        Assert.Equal("places.noClass", Assert.Single(w.Refusals).Reason);
        for (int i = 0; i <= Vessels.MaxContacts; i++) w.Do(new Find("shuttle")).Run();
        Assert.Equal(Vessels.MaxContacts, w.M.Places.Contacts.Count);
        Assert.Equal("places.full", w.Refusals[^1].Reason);
        w.Do(new Jump(999)).Run();
        Assert.Equal("places.noContact", w.Refusals[^1].Reason);
        // A peer not in yet: its seat would hold the vessel open with no farewell to close it.
        w.Do(new Jump(w.M.Places.Contacts[0].Id) { From = 3 }).Run();
        Assert.Equal("places.noHero", w.Refusals[^1].Reason);
        Assert.Equal(Vessels.MaxContacts, w.M.Places.Contacts.Count);
        Assert.Single(w.World.Surfaces);
    }

    [Fact]
    public void GoingHomeClosesTheVesselWorldForGoodAndTheNextJumpOpensANewId()
    {
        var (w, id) = Visited("shuttle");
        w.Do(new Transfer(0)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.True(w.World.Surfaces[id].Closed);
        Assert.Empty(w.World.ModulesOn(id));
        w.Do(new Transfer(id)).Run();
        Assert.Equal(World.NoSurface, Assert.Single(w.Refusals).Reason);
        w.Refusals.Clear();
        var (_, next) = Visited("shuttle", w);
        Assert.Equal(id + 1, next);
    }

    [Fact]
    public void AFriendComingAfterArrivesAtTheDoorAndTheLastToGoHomeClosesIt()
    {
        var (w, id) = Visited("shuttle");
        w.Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run();
        w.Do(new Transfer(id) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        var v = w.On(id);
        Assert.Equal(v.Crew.Spawn, v.Crew.HeroOf(1)!.Cell);
        w.Do(new Transfer(0)).Run();
        Assert.False(w.World.Surfaces[id].Closed);
        w.Do(new Transfer(0) { From = 1 }).Run();
        Assert.True(w.World.Surfaces[id].Closed);
    }

    [Fact]
    public void AFarewellOnAVesselClosesIt()
    {
        var (w, id) = Visited("shuttle");
        w.Do(new Leave()).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.True(w.World.Surfaces[id].Closed);
    }

    [Fact]
    public void AHeroDyingOnAVesselWakesAboardAndTheVesselClosesItsBodyWithIt()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.GiveItems(0, ("scrap", 9));
        var (_, id) = Visited("shuttle", w);
        var v = w.On(id);
        // In a vessel's airless room, its suit all but empty: the next breath starts its last, which passes it out.
        v.Crew.HeroOf(0)!.Air = 1;
        w.Run(Content.Crew.BreathEvery);
        w.Run(Content.Crew.LastBreath);
        Assert.True(v.Crew.HeroOf(0)!.Out > 0);
        Assert.False(w.World.Surfaces[id].Closed);
        w.Run(Content.Crew.PassOut);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.NotNull(w.M.Crew.HeroOf(0));
        Assert.True(w.World.Surfaces[id].Closed);
        Assert.DoesNotContain(w.M.Items.Inventory(0), s => s?.Item == "scrap");
    }

    [Fact]
    public void AWorldSavedOnAVesselLoadsBackOnItAndOneSavedAfterLoadsItClosed()
    {
        var (w, id) = Visited();
        w.Do(new Find("shuttle")).Run();
        var contacts = w.M.Places.Contacts.ToList();
        string save = w.World.Save();
        w.Reload();
        Assert.Equal(save, w.World.Save());
        Assert.Equal(id, w.World.SurfaceOf(0));
        Assert.Equal(contacts, w.M.Places.Contacts);
        var v = w.On(id);
        Assert.NotEmpty(v.Grid.MachinesOf("crate"));
        Assert.NotNull(v.Crew.HeroOf(0));

        w.Do(new Transfer(0)).Run();
        save = w.World.Save();
        var surfaces = JsonNode.Parse(save)!["surfaces"]!.AsArray();
        Assert.Equal($$"""{"id":{{id}},"kind":"vessel.freighter","seed":{{w.World.Surfaces[id].Seed}},"closed":true}""", surfaces[id - 1]!.ToJsonString());
        w.Reload();
        Assert.Equal(save, w.World.Save());
        Assert.True(w.World.Surfaces[id].Closed);
    }

    [Fact]
    public void TwoWorldsKeptAlikeAcrossAVisitAndItsCloseHashTheSame()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new Find("shuttle")).Run();
        var copy = new Scenario(Registry.Wreck).Reload(w.World.Save());
        int contact = w.M.Places.Contacts[^1].Id;
        Command[][] ticks = [[new Jump(contact)], [], [new Enter(5) { From = 0 }], [new Transfer(0)], []];
        foreach (var commands in ticks)
        {
            w.World.Step(commands);
            copy.World.Step(commands);
            Assert.Equal(w.World.Hash(), copy.World.Hash());
        }
        Assert.True(copy.World.Surfaces[^1].Closed);
    }

    /// <summary>
    /// Whatever heroes do — come in, find, jump, transfer anywhere, leave — no vessel world stays open with nobody on it after a
    /// tick, and every closed one stays closed (vessel-worlds).
    /// </summary>
    [Property(MaxTest = 20)]
    public void NoVesselWorldIsLeftOpenWithNobodyOnIt(PositiveInt seed)
    {
        var w = new Scenario(Registry.Wreck);
        var rng = new Rng(new RngState { S = (uint)seed.Get });
        var closed = new HashSet<int>();
        for (int tick = 0; tick < 60; tick++)
        {
            int peer = (int)rng.Below(3);
            Command c = rng.Below(6) switch
            {
                0 => new Enter((int)rng.Below(400)) { From = peer },
                1 => new Find(rng.Below(2) == 0 ? "shuttle" : "freighter") { From = peer },
                2 or 3 => new Jump(w.M.Places.Contacts.Count > 0 ? w.M.Places.Contacts[^1].Id : 1) { From = peer },
                4 => new Transfer((int)rng.Below((uint)w.World.Surfaces.Count + 1)) { From = peer },
                _ => new Leave { From = peer },
            };
            w.World.Step([c]);
            foreach (var s in w.World.Surfaces.Skip(1))
            {
                bool someone = Enumerable.Range(0, 3).Any(p => w.World.SurfaceOf(p) == s.Id);
                Assert.True(s.Closed || someone, $"surface {s.Id} open with nobody on it at tick {tick}");
                if (closed.Contains(s.Id)) Assert.True(s.Closed, $"surface {s.Id} opened again");
                if (s.Closed) closed.Add(s.Id);
            }
        }
    }

    /// <summary>A jump tick on the new game's world: its world opened, its vessel written, Rooms and the rest after it; a liner's and a tanker's too.</summary>
    [Theory]
    [InlineData("freighter")]
    [InlineData("liner")]
    [InlineData("tanker")]
    public void AJumpTickStaysUnderItsCeiling(string cls)
    {
        // A hitch of a few frames once per visit, as a load is.
        const int CeilingMs = 200;
        var (w, _) = Visited(cls);
        w.Do(new Transfer(0)).Run();
        w.Do(new Find(cls)).Run();
        w.Do(new Jump(w.M.Places.Contacts[^1].Id));
        var clock = Stopwatch.StartNew();
        w.Run();
        clock.Stop();
        Assert.Empty(w.Refusals);
        output.WriteLine($"{cls}: {clock.ElapsedMilliseconds} ms");
        Assert.True(clock.ElapsedMilliseconds < CeilingMs, $"{clock.ElapsedMilliseconds} ms");
    }
}
