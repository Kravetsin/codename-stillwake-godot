using Stillwake.Sim.Power;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Machines a player switches on and off in their panel (industrial-oxygen): the vaporizer here; the rest by their own tests.</summary>
public class SwitchTests
{
    /// <summary>A room aired by a vaporizer piped to a vent, its tank under the hull and a panel powering it, as the furnace's tests.</summary>
    private static Scenario World()
    {
        var w = new Scenario(AirTesting.Vaporized(["##########", "#....TT..#", "#....TTV.#", "##########"], ["", "     o", "     ---"]), doubled: true);
        for (int i = 0; i < 2000 && w.M.Atmosphere.AiredCells.Count == 0; i++) w.Run();
        Assert.NotEmpty(w.M.Atmosphere.AiredCells);
        return w;
    }

    private static int Vaporizer(Scenario w) => w.M.Grid.MachinesOf("generator")[0].Id;

    [Fact]
    public void AVaporizerSwitchedOffAirsNothingItsRoomSaysWhyAndOnAgainItAirs()
    {
        var w = World();
        int vaporizer = Vaporizer(w), room = w.M.Rooms.RoomAt(w.M.Atmosphere.AiredCells[0])!.Value;
        w.Do(new SetSwitch(vaporizer, false)).Run(300);
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Power.IsOff(vaporizer));
        Assert.Equal(0, w.M.Power.Satisfaction(vaporizer));
        Assert.Empty(w.M.Atmosphere.AiredCells);
        Assert.Equal("atmosphere.off", w.M.Atmosphere.RoomsAir().Single(r => r.Room == room).Problem);
        w.Do(new SetSwitch(vaporizer, true)).Run(600);
        Assert.False(w.M.Power.IsOff(vaporizer));
        Assert.NotEmpty(w.M.Atmosphere.AiredCells);
    }

    [Fact]
    public void OnlyAMachineThatSwitchesIsSwitchedAndOffItLoadsOff()
    {
        var w = World();
        int tank = w.M.Grid.MachinesOf("tank")[0].Id, vaporizer = Vaporizer(w);
        w.Do(new SetSwitch(tank, false)).Do(new SetSwitch(999, false)).Run();
        Assert.Equal(["power.noSwitch", "power.noSwitch"], w.Refusals.Select(r => r.Reason));
        w.Do(new SetSwitch(vaporizer, false)).Run();
        w.Reload();
        Assert.True(w.M.Power.IsOff(vaporizer));
        Assert.False(w.M.Power.IsOff(tank));
    }
}
