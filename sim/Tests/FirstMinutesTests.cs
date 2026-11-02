using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>The first minutes as docs/plans/board.md tells them, on the wreck itself and by commands alone: nothing is granted, bought or given.</summary>
public class FirstMinutesTests
{
    private static readonly Layout Wreck = Registry.Wreck with { Meteors = false };

    private static bool Outside(Scenario w, int cell) => w.M.Rooms.RoomAt(cell) == RoomsModule.Space;

    /// <summary>Hero 0 waits indoors for <paramref name="count"/> pieces, each made an <paramref name="item"/>, stepping out for each: it would choke out there.</summary>
    private static void Catch(Scenario w, string item, int count)
    {
        int space = Enumerable.Range(0, w.M.Grid.Width * w.M.Grid.Height).First(c => Outside(w, c) && w.M.Grid.WallAt(c) == null);
        for (int caught = 0; caught < count; caught++)
        {
            Piece? piece = null;
            for (int i = 0; i < 2000 && piece == null; i++)
            {
                piece = w.M.Debris.Pieces.FirstOrDefault(p => Outside(w, DebrisModule.CellOf(p.X, p.Y, w.M.Grid.Width)));
                if (piece == null) w.Run();
            }
            Assert.NotNull(piece);
            piece.Item = item;
            w.Do(new Enter(space)).Do(new Grab(piece.Id)).Do(new Enter(w.M.Crew.Spawn)).Run();
        }
    }

    /// <summary>Plans <paramref name="kind"/> in the hero's room and works it till it stands.</summary>
    private static int Build(Scenario w, string kind)
    {
        var placement = new Placement("machine", kind);
        int room = w.M.Rooms.RoomAt(w.M.Crew.Spawn)!.Value;
        int cell = Enumerable.Range(0, w.M.Grid.Width * w.M.Grid.Height).First(c => w.M.Rooms.RoomAt(c) == room && w.M.Building.Problem(c, placement) == null);
        w.Do(new PlacePlan(cell, placement)).Run();
        int plan = w.M.Building.Plans.Single().Id;
        Machine? Built() => w.M.Grid.Machines.FirstOrDefault(m => m.Kind == kind);
        for (int i = 0; i < 2 * Machines.Get(kind).BuildTicks && Built() == null; i++) w.Do(new Work(plan)).Run();
        Assert.True(Built() != null, $"{kind} at {cell}: {string.Join(", ", w.Refusals.Select(r => r.Reason).Distinct())}, lacking {string.Join(", ", w.M.Building.Lacking(plan))}");
        return Built()!.Id;
    }

    private static void Put(Scenario w, string item, int count, Target target)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        w.Do(new Move(new Slot(from), target, count)).Run();
    }

    [Fact]
    public void CatchesOpenTheFurnaceAndTheBenchSmeltsOpenTheFloorAndTheBenchOpensWireAndPlate()
    {
        var w = new Scenario(Wreck).Run();
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        Assert.False(w.M.Research.Knows("furnace"));

        // At 10 chunks and 5 hull plates, the furnace; one short is not enough.
        Catch(w, "scrapChunk", 11);
        Catch(w, "hullPlate", 4);
        Assert.False(w.Run().M.Research.Knows("furnace"));
        Catch(w, "hullPlate", 1);
        Assert.True(w.Run().M.Research.Knows("furnace"));
        Assert.False(w.M.Research.Knows("workbench"));
        Catch(w, "hullPlate", 4);

        int furnace = Build(w, "furnace");
        Catch(w, "planks", 10);
        Assert.True(w.Run().M.Research.Knows("workbench"));
        Assert.False(w.M.Research.Knows("floor"));

        // A chunk gives 4 scrap: at 3, floor and ladder ink in. A hull plate gives 2 iron: at 5, the bench has what it opens things with.
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.M.Grid.Machines.First(m => m.Id == furnace).Cell); i++) w.Run();
        Put(w, "planks", 3, new Target(Machines.Get("furnace").FuelSlot, furnace));
        Put(w, "scrapChunk", 3, new Target(Crate: furnace));
        for (int i = 0; i < 2000 && w.M.Knowledge.ObtainedOf("scrap") < 12; i++) w.Run();
        Assert.True(w.Run().M.Research.Knows("floor"));
        Assert.True(w.M.Research.Knows("ladder"));
        Put(w, "hullPlate", 5, new Target(Crate: furnace));
        for (int i = 0; i < 2000 && w.M.Knowledge.ObtainedOf("iron") < 10; i++) w.Run();
        Assert.Equal(10, w.M.Knowledge.ObtainedOf("iron"));
        Assert.False(w.Run().M.Research.Knows("wire"));

        // The bench's last stroke opens what it makes of what is had by now.
        Build(w, "workbench");
        w.Run();
        Assert.True(w.M.Research.Knows("wire"));
        Assert.True(w.M.Research.Knows("plate"));
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.M.Research.Credits);
    }
}
