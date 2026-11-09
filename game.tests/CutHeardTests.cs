using Stillwake.Content;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A catcher's cut as heard (tracker-catchers): a cell broken out since the last look by its material, else one bitten.</summary>
public class CutHeardTests
{
    private const int W = 10, H = 10;
    private static readonly Material Soil = Materials.Get("soil"), Stone = Materials.Get("stone");

    [Fact]
    public void ACellGoneSinceTheLastLookIsHeardBrokenByWhatItWas()
    {
        var walls = new Material?[W * H];
        walls[3 * W + 5] = Stone;
        walls[4 * W + 5] = Soil;
        var heard = new CutHeard();
        Assert.Null(heard.Look(2, 2, 3, 5, W, H, c => walls[c], cut: false));
        walls[3 * W + 5] = null;
        Assert.Equal((3 * W + 5, Stone, true), heard.Look(2, 2, 3, 5, W, H, c => walls[c], cut: true));
    }

    [Fact]
    public void ACutBreakingNothingIsHeardBitingTheFirstStandingAndNoCutIsSilent()
    {
        var walls = new Material?[W * H];
        walls[4 * W + 5] = Soil;
        var heard = new CutHeard();
        heard.Look(2, 2, 3, 5, W, H, c => walls[c], cut: false);
        Assert.Null(heard.Look(2, 2, 3, 5, W, H, c => walls[c], cut: false));
        Assert.Equal((4 * W + 5, Soil, false), heard.Look(2, 2, 3, 5, W, H, c => walls[c], cut: true));
    }

    [Fact]
    public void NothingRoundItIsNothingHeardAndTheEdgeOfTheGridIsKept()
    {
        var walls = new Material?[W * H];
        var heard = new CutHeard();
        Assert.Null(heard.Look(0, 0, 3, 5, W, H, c => walls[c], cut: true));
        Assert.Null(heard.Look(8, 6, 3, 5, W, H, c => walls[c], cut: true));
    }
}
