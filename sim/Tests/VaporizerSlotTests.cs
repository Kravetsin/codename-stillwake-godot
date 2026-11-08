using Xunit;
using Air = Stillwake.Content.Air;

namespace Stillwake.Sim.Tests;

/// <summary>A vaporizer's slot of canisters (industrial-oxygen): it works with no liquid piped to it.</summary>
public class VaporizerSlotTests
{
    /// <summary>A room with a vaporizer piped to a vent, its tank under the hull empty.</summary>
    private static Scenario World() =>
        new(AirTesting.Vaporized(["##########", "#....TT..#", "#....TTV.#", "##########"], ["", "", "     ---"]), doubled: true);

    [Fact]
    public void ACanisterInItsSlotIsPouredIntoItsOwnLiquidAndAirsTheRoom()
    {
        var w = World().Run(300);
        int vaporizer = w.M.Grid.MachinesOf("generator")[0].Id;
        Assert.Empty(w.M.Atmosphere.AiredCells);
        Assert.Equal(0, w.M.Atmosphere.LiquidFor(vaporizer));
        w.Put(0, new Items.Stack("canister", 2), crate: vaporizer).Run();
        // One poured into its own liquid, the other waiting till that runs out.
        Assert.Equal(1, w.M.Items.Crate(vaporizer)[0]?.Count);
        Assert.InRange(w.M.Atmosphere.LiquidFor(vaporizer), 1, Air.Canister);
        for (int i = 0; i < 2000 && w.M.Atmosphere.AiredCells.Count == 0; i++) w.Run();
        Assert.NotEmpty(w.M.Atmosphere.AiredCells);
        Assert.True(w.M.Atmosphere.LiquidFor(vaporizer) < Air.Canister);
    }

    /// <summary>The room it airs tells the liquid it has, its own and the canisters waiting, and how long it lasts (industrial-oxygen).</summary>
    [Fact]
    public void ItsRoomTellsItsOwnLiquidAndTheCanistersWaitingAndHowLongTheyLast()
    {
        var w = World().Run();
        int vaporizer = w.M.Grid.MachinesOf("generator")[0].Id;
        w.Put(0, new Items.Stack("canister", 3), crate: vaporizer).Run();
        for (int i = 0; i < 2000 && w.M.Atmosphere.AiredCells.Count == 0; i++) w.Run();
        int room = w.M.Rooms.RoomAt(w.M.Atmosphere.AiredCells[0])!.Value;
        var air = w.M.Atmosphere.RoomsAir().Single(r => r.Room == room);
        Assert.Equal(w.M.Atmosphere.LiquidFor(vaporizer) + 2 * Air.Canister, air.Oxygen);
        Assert.NotNull(air.Lasts);
    }

    [Fact]
    public void ItsOwnLiquidLoadsAsSaved()
    {
        var w = World().Run();
        int vaporizer = w.M.Grid.MachinesOf("generator")[0].Id;
        w.Put(0, new Items.Stack("canister", 1), crate: vaporizer).Run(5);
        int held = w.M.Atmosphere.LiquidFor(vaporizer);
        Assert.True(held > 0);
        w.Reload();
        Assert.Equal(held, w.M.Atmosphere.LiquidFor(vaporizer));
    }
}

/// <summary>A vaporizer with nothing on its output airs its own room, as a vent (industrial-oxygen).</summary>
public class UnpipedVaporizerTests
{
    [Fact]
    public void AVaporizerWithNoPipeOnItsOutputAirsItsOwnRoom()
    {
        var w = new Scenario(HandPiped.Chained("#########\n#..TT...#\n#..TT...#\n#########"), doubled: true).Do(new Items.SetCreative(true));
        int tank = w.M.Grid.MachinesOf("tank")[0].Id, vaporizer = w.M.Grid.MachinesOf("generator")[0].Id;
        w.Do(new Atmosphere.LoadCanister(tank)).Do(new Items.SetCreative(false)).Run();
        Assert.Empty(w.M.Grid.MachinesOf("vent"));
        for (int i = 0; i < 2000 && w.M.Atmosphere.AiredCells.Count == 0; i++) w.Run();
        int room = w.M.Rooms.RoomAt(w.M.Grid.MachineById(vaporizer)!.Cell)!.Value;
        Assert.NotEmpty(w.M.Atmosphere.AiredCells);
        Assert.All(w.M.Atmosphere.AiredCells, c => Assert.Equal(room, w.M.Rooms.RoomAt(c)));
        Assert.Equal(Air.GeneratorRate, w.M.Atmosphere.RoomsAir().Single(r => r.Room == room).Made);
    }
}
