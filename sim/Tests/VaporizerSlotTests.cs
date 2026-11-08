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
